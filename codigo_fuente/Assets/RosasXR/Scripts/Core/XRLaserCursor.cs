using UnityEngine;

namespace RosasXR
{
    /// <summary>Línea y punto del puntero que dibuja el OVRInputModule de Meta (controles o manos).</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class XRLaserCursor : OVRCursor
    {
        public Transform dot;
        public float defaultLength = 3f;
        LineRenderer _line;
        float _lastUpdate = -1f;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
        }

        public override void SetCursorRay(Transform t) => Draw(t.position, t.position + t.forward * defaultLength, false);

        public override void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal) => Draw(start, dest, true);

        void Draw(Vector3 a, Vector3 b, bool hit)
        {
            _line.SetPosition(0, a);
            _line.SetPosition(1, b);
            if (dot != null) { dot.position = b; dot.gameObject.SetActive(hit); }
            _lastUpdate = Time.time;
        }

        void LateUpdate()
        {
            // Ocultar si el módulo de entrada no dibujó nada este frame (sin controles ni manos).
            bool visible = Time.time - _lastUpdate < 0.1f;
            _line.enabled = visible;
            if (!visible && dot != null) dot.gameObject.SetActive(false);
        }
    }
}
