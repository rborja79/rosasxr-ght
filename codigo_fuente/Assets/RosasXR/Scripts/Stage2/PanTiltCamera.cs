using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Cámara robótica del mástil. Reposo: pan 0°, tilt 20° y barrido lento ±60°.
    /// Seguimiento: sigue el LED de las tijeras (máx. 120°/s, zona muerta ±3°); si lo pierde más de 1 s vuelve a reposo.
    /// Solo ve lo que está dentro de su cono y sin oclusión.
    /// </summary>
    public class PanTiltCamera : MonoBehaviour
    {
        public Transform panJoint;     // gira en Y local
        public Transform tiltJoint;    // gira en X local (hijo de panJoint)
        public Transform lens;         // punto de la cámara (forward = dirección de mirada)
        public Transform target;       // LED de las tijeras

        public Vector2 panLimits = new Vector2(-100f, 100f);
        public Vector2 tiltLimits = new Vector2(-10f, 60f);
        public float fovDegrees = 70f;
        public float maxRange = 3.5f;
        public float maxSpeed = 120f;
        public float deadZone = 3f;
        public float lostTimeout = 1f;
        public float restTilt = 30f;
        public float sweepAmplitude = 60f;
        public float sweepPeriod = 10f;
        public LayerMask occluders = ~0;
        public LineRenderer debugRay;

        public bool SeesTarget { get; private set; }
        /// <summary>Punto a encuadrar mientras sigue la tijera (p. ej. entre la tijera y el botón). null = el LED.</summary>
        public Vector3? AimOverride { get; set; }
        public bool Tracking { get; private set; }
        public float Pan => _pan;
        public float Tilt => _tilt;
        /// <summary>Por qué no ve la tijera (para la pantalla del carro): "", "lejos", "fuera de alcance", "tapada", "oculta".</summary>
        public string Reason { get; private set; } = "";

        float _pan, _tilt = 20f, _lastSeen = -10f, _sweepT;

        // LateUpdate: se mueve después de que se actualiza la pose de la mano y de las tijeras
        void LateUpdate()
        {
            // Ver = la tijera dentro del cuadro. Buscar = la tijera al alcance del brazo y sin nada que la tape:
            // el servo gira hacia ella aunque todavía no esté en cuadro (busca el mejor ángulo).
            SeesTarget = Check(true);
            bool canSeek = SeesTarget || Check(false);
            if (canSeek) _lastSeen = Time.time;
            Tracking = Time.time - _lastSeen <= lostTimeout;

            float wantPan, wantTilt, speed = maxSpeed;
            if (Tracking && target != null)
            {
                Desired(AimOverride ?? target.position, out wantPan, out wantTilt);
                if (Mathf.Abs(Mathf.DeltaAngle(_pan, wantPan)) < deadZone) wantPan = _pan;
                if (Mathf.Abs(Mathf.DeltaAngle(_tilt, wantTilt)) < deadZone) wantTilt = _tilt;
                _sweepT = 0f;
            }
            else
            {
                _sweepT += Time.deltaTime;
                wantPan = sweepAmplitude * Mathf.Sin(_sweepT * 2f * Mathf.PI / sweepPeriod);
                wantTilt = restTilt;
                speed = 40f;
            }
            wantPan = Mathf.Clamp(wantPan, panLimits.x, panLimits.y);
            wantTilt = Mathf.Clamp(wantTilt, tiltLimits.x, tiltLimits.y);
            // Suavizado + límite de velocidad angular
            float k = 1f - Mathf.Exp(-10f * Time.deltaTime);
            _pan = Mathf.MoveTowards(_pan, Mathf.Lerp(_pan, wantPan, Mathf.Max(k, 0.35f)), speed * Time.deltaTime);
            _tilt = Mathf.MoveTowards(_tilt, Mathf.Lerp(_tilt, wantTilt, Mathf.Max(k, 0.35f)), speed * Time.deltaTime);
            if (panJoint != null) panJoint.localRotation = Quaternion.Euler(0f, _pan, 0f);
            if (tiltJoint != null) tiltJoint.localRotation = Quaternion.Euler(-_tilt, 0f, 0f);

            if (debugRay != null)
            {
                debugRay.enabled = debugRay.gameObject.activeSelf && SeesTarget;
                if (debugRay.enabled) { debugRay.SetPosition(0, lens.position); debugRay.SetPosition(1, target.position); }
            }
        }

        void Desired(Vector3 world, out float pan, out float tilt)
        {
            var parent = panJoint.parent != null ? panJoint.parent : transform;
            var d = parent.InverseTransformPoint(world) - parent.InverseTransformPoint(lens.position);
            pan = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            tilt = Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        }

        bool Check(bool requireInFrame)
        {
            if (target == null || lens == null) { Reason = "oculta"; return false; }
            if (!target.gameObject.activeInHierarchy) { Reason = "oculta"; return false; }
            var to = target.position - lens.position;
            float dist = to.magnitude;
            if (dist > maxRange || dist < 0.05f) { Reason = "lejos"; return false; }
            Desired(target.position, out var p, out var t);
            if (p < panLimits.x - 5f || p > panLimits.y + 5f || t < tiltLimits.x - 10f || t > tiltLimits.y + 10f) { Reason = "fuera de alcance"; return false; }
            if (requireInFrame && Vector3.Angle(lens.forward, to) > fovDegrees * 0.5f) { Reason = "buscando"; return false; }
            // Oclusión: follaje con collider entre la cámara y el LED
            if (Physics.Raycast(lens.position, to / dist, out var hit, dist - 0.03f, occluders, QueryTriggerInteraction.Ignore))
            { Reason = "tapada"; return false; }
            Reason = "";
            return true;
        }
    }
}
