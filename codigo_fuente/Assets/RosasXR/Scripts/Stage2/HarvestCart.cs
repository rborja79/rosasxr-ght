using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RosasXR
{
    /// <summary>
    /// Carro sobre un riel virtual por el corredor. Se mueve SOLO mientras una mano (o las dos) agarra la manija:
    /// al agarrar se guarda el punto de agarre; la mano adelante del punto hace avanzar el carro y atrás (hacia el
    /// cuerpo) lo hace retroceder, con velocidad proporcional, zona muerta y suavizado. Al soltar frena y se detiene.
    /// El usuario se mueve con el carro. Al entrar queda detrás de la manija mirando el corredor.
    /// Cambiar de corredor: menú o joystick a los lados (vuelve a la cabecera y cruza).
    /// En el editor: W/S o flechas.
    /// </summary>
    public class HarvestCart : MonoBehaviour
    {
        public Transform handle;
        public Renderer handleRenderer;
        public Transform rig;            // OVRCameraRig: se mueve junto con el carro
        public float railMinZ = 0.75f, railMaxZ = 22f;
        public float grabRadius = 0.12f;   // distancia a la BARRA de la manija (no a su centro)
        /// <summary>Devuelve true si esa mano (right = derecha) no puede agarrar la manija (p. ej. lleva flores).</summary>
        public System.Func<bool, bool> HandBlocked;
        public float hoverRadius = 0.3f;
        [Header("Empuje")]
        public float deadZone = 0.03f;
        public float gain = 6f;              // (m/s) por metro de desplazamiento de la mano
        public float maxForward = 0.8f, maxBackward = 0.4f;
        public float accel = 3f, brake = 2.5f;
        [Tooltip("Distancia de la cabeza a la manija al recentrar.")]
        public float standBehind = 0.55f;

        public Color handleIdle = new Color(0.08f, 0.08f, 0.09f);
        public Color handleHover = new Color(0.9f, 0.75f, 0.2f);
        public Color handleGrab = new Color(0.1f, 0.75f, 0.35f);

        public bool Grabbed => _grab[0] || _grab[1];
        public float Travelled { get; private set; }
        public float MaxZ { get; private set; }
        public float Speed => Mathf.Abs(_vel);
        public bool AtEnd => transform.position.z >= railMaxZ - 0.15f;

        [Header("Corredores (riel por cada pasillo; se cambia en la cabecera)")]
        public float[] aisles = { -1.5f, 0f, 1.5f };
        public int aisle = 1;
        public float aisleSpeed = 1.0f;
        public bool Switching { get; private set; }
        public string AisleName => $"Corredor {aisle + 1} de {aisles.Length}";

        float[] _maxZ;
        float _stickXPrev, _vel;
        readonly bool[] _grab = new bool[2];
        readonly bool[] _prevPressed = new bool[2];
        readonly float[] _closedAt = { -9f, -9f };
        readonly Vector3[] _refLocal = new Vector3[2];
        Material _handleMat;

        IEnumerator Start()
        {
            MaxZ = transform.position.z;
            _maxZ = new float[aisles.Length];
            for (int i = 0; i < _maxZ.Length; i++) _maxZ[i] = railMinZ;
            if (handleRenderer != null) _handleMat = handleRenderer.material;
            if (Application.isEditor) yield break;
            yield return new WaitForSeconds(0.5f);
            Recenter();
            yield return new WaitForSeconds(1.0f);
            Recenter();
        }

        /// <summary>Gira y mueve el rig para que el usuario quede detrás de la manija, mirando el corredor.</summary>
        public void Recenter()
        {
            var cam = Camera.main;
            if (cam == null || rig == null || handle == null) return;
            var fwd = cam.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 1e-4f)
            {
                float yaw = Vector3.SignedAngle(fwd.normalized, Vector3.forward, Vector3.up);
                rig.RotateAround(cam.transform.position, Vector3.up, yaw);
            }
            var head = cam.transform.position;
            var target = new Vector3(transform.position.x, head.y, handle.position.z - standBehind);
            rig.position += new Vector3(target.x - head.x, 0f, target.z - head.z);
            _grab[0] = _grab[1] = false;
            _vel = 0f;
        }

        /// <summary>Distancia recorrida en el corredor más cercano a x (para contar rosas no cortadas).</summary>
        public float VisitedZ(float x)
        {
            int best = 0;
            for (int i = 1; i < aisles.Length; i++) if (Mathf.Abs(aisles[i] - x) < Mathf.Abs(aisles[best] - x)) best = i;
            return _maxZ != null ? _maxZ[best] : MaxZ;
        }

        /// <summary>Ir directo a un corredor (0, 1, 2): lo usan los letreros clicables de la cabecera.</summary>
        public void ChangeToAisle(int target)
        {
            target = Mathf.Clamp(target, 0, aisles.Length - 1);
            if (target == aisle || Switching) return;
            StartCoroutine(SwitchTo(target));
        }

        public void ChangeAisle(int dir)
        {
            int target = Mathf.Clamp(aisle + dir, 0, aisles.Length - 1);
            if (target == aisle || Switching) return;
            StartCoroutine(SwitchTo(target));
        }

        IEnumerator SwitchTo(int target)
        {
            Switching = true;
            _grab[0] = _grab[1] = false;
            _vel = 0f;
            while (transform.position.z > railMinZ + 0.001f)
            {
                Move(-Mathf.Min(1.6f * Time.deltaTime, transform.position.z - railMinZ));
                yield return null;
            }
            float tx = aisles[target];
            while (Mathf.Abs(transform.position.x - tx) > 0.001f)
            {
                float dx = Mathf.MoveTowards(transform.position.x, tx, aisleSpeed * Time.deltaTime) - transform.position.x;
                transform.position += Vector3.right * dx;
                if (rig != null) rig.position += Vector3.right * dx;
                yield return null;
            }
            aisle = target;
            Switching = false;
        }

        void Update()
        {
            if (Switching) { SetHandleColor(handleIdle); return; }
            var b = XRInputBridge.Instance;
            float want = 0f;
            bool hover = false;
            if (b != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    bool right = i == 1;
                    var p = b.Point(right);
                    if (p == null) { _grab[i] = false; continue; }
                    var gp = b.GrabPoint(right);
                    float d = DistanceToHandle(gp);
                    if (d <= hoverRadius) hover = true;
                    bool g = b.Grab(right);
                    bool justClosed = g && !_prevPressed[i];
                    _prevPressed[i] = g;
                    if (!g) { _grab[i] = false; continue; }
                    if (!_grab[i])
                    {
                        // Solo se engancha si la mano SE CIERRA sobre la manija (no si llega ya cerrada, p. ej. con una flor)
                        bool blocked = HandBlocked != null && HandBlocked(right);
                        // Gracia: la mano pudo cerrarse un instante antes de llegar a la barra
                        if (justClosed) _closedAt[i] = Time.time;
                        bool recent = Time.time - _closedAt[i] < 0.35f;
                        if (recent && d <= grabRadius && !blocked) { _grab[i] = true; _refLocal[i] = Local(gp); }
                        continue;
                    }
                    // Desplazamiento de la mano desde el punto de agarre, proyectado sobre el eje del corredor
                    var delta = rig != null ? rig.TransformVector(Local(gp) - _refLocal[i]) : Vector3.zero;
                    float dz = delta.z;
                    float mag = Mathf.Max(0f, Mathf.Abs(dz) - deadZone) * gain;
                    float v = dz >= 0f ? Mathf.Min(mag, maxForward) : -Mathf.Min(mag, maxBackward);
                    if (Mathf.Abs(v) > Mathf.Abs(want)) want = v;
                }
                // Joystick a los lados: cambiar de corredor
                float sx = b.Stick.x;
                if (Mathf.Abs(sx) > 0.75f && Mathf.Abs(_stickXPrev) <= 0.75f) ChangeAisle(sx > 0 ? 1 : -1);
                _stickXPrev = sx;
            }
            if (Application.isEditor) { float e = EditorAxis(); if (Mathf.Abs(e) > 0f) want = e * maxForward; }

            // Suavizado: acelera hacia la velocidad pedida; sin agarre frena hasta detenerse
            bool driving = Grabbed || (Application.isEditor && Mathf.Abs(want) > 0f);
            float rate = driving ? accel : brake;
            _vel = Mathf.MoveTowards(_vel, driving ? want : 0f, rate * Time.deltaTime);
            if (Mathf.Abs(_vel) > 1e-4f) Move(_vel * Time.deltaTime);

            SetHandleColor(Grabbed ? handleGrab : hover ? handleHover : handleIdle);
        }

        /// <summary>Distancia del punto a la barra de la manija (un cilindro horizontal).</summary>
        float DistanceToHandle(Vector3 p)
        {
            if (handle == null) return 99f;
            var axis = handle.up;                       // eje del cilindro
            float half = handle.lossyScale.y;           // el cilindro de Unity mide 2 unidades de alto
            float t = Mathf.Clamp(Vector3.Dot(p - handle.position, axis), -half, half);
            return Vector3.Distance(p, handle.position + axis * t);
        }

        Vector3 Local(Vector3 world) => rig != null ? rig.InverseTransformPoint(world) : world;

        void SetHandleColor(Color c)
        {
            if (_handleMat != null && _handleMat.color != c) _handleMat.color = c;
        }

        public void Move(float dz)
        {
            float z = transform.position.z;
            float nz = Mathf.Clamp(z + dz, railMinZ, railMaxZ);
            float d = nz - z;
            if (Mathf.Abs(d) < 1e-6f) { _vel = 0f; return; }
            transform.position += Vector3.forward * d;
            if (rig != null) rig.position += Vector3.forward * d;
            Travelled += Mathf.Abs(d);
            MaxZ = Mathf.Max(MaxZ, transform.position.z);
            if (_maxZ != null) _maxZ[aisle] = Mathf.Max(_maxZ[aisle], transform.position.z);
        }

        static float EditorAxis()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return 0f;
            return (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
#else
            return Input.GetAxis("Vertical");
#endif
        }
    }
}
