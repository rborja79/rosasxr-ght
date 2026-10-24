using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace RosasXR
{
    /// <summary>
    /// Rayo desde el control derecho para usar los paneles (canvas de mundo) en el visor.
    /// Apuntar resalta el botón; gatillo o botón A lo pulsa. En el editor se apaga (se usa el mouse).
    /// </summary>
    public class XRRayClicker : MonoBehaviour
    {
        public Transform rayOrigin;
        public float maxDistance = 6f;
        public Color idleColor = new Color(1f, 1f, 1f, 0.35f);
        public Color hoverColor = new Color(0.11f, 0.64f, 0.42f, 1f);

        readonly List<Canvas> _canvases = new List<Canvas>();
        LineRenderer _line;
        Transform _dot;
        Selectable _hovered;
        float _nextScan;

        void Start()
        {
            if (Application.isEditor) { enabled = false; return; } // en el editor se usa el mouse

            var mat = new Material(Shader.Find("Sprites/Default"));
            var lineGo = new GameObject("RayoControl");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            _line.material = mat;
            _line.positionCount = 2;
            _line.startWidth = 0.004f;
            _line.endWidth = 0.002f;
            _line.useWorldSpace = true;

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.name = "PuntoRayo";
            dot.transform.SetParent(transform, false);
            dot.transform.localScale = Vector3.one * 0.012f;
            dot.GetComponent<Renderer>().material = mat;
            _dot = dot.transform;
        }

        void Update()
        {
            if (rayOrigin == null) return;

            if (Time.time >= _nextScan)
            {
                _canvases.Clear();
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.isRootCanvas && c.renderMode == RenderMode.WorldSpace) _canvases.Add(c);
                _nextScan = Time.time + 0.5f;
            }

            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            float best = maxDistance;
            Vector3 end = ray.GetPoint(maxDistance);
            Selectable hit = null;
            bool onCanvas = false;

            foreach (var c in _canvases)
            {
                if (c == null || !c.isActiveAndEnabled) continue;
                var rt = (RectTransform)c.transform;
                var plane = new Plane(rt.forward, rt.position);
                if (!plane.Raycast(ray, out float d) || d > best) continue;
                var p = ray.GetPoint(d);
                if (!rt.rect.Contains((Vector2)rt.InverseTransformPoint(p))) continue;

                best = d; end = p; onCanvas = true; hit = null;
                foreach (var s in c.GetComponentsInChildren<Selectable>())
                {
                    if (!s.isActiveAndEnabled || !s.IsInteractable()) continue;
                    var srt = (RectTransform)s.transform;
                    if (srt.rect.Contains((Vector2)srt.InverseTransformPoint(p))) hit = s;
                }
            }

            if (hit != _hovered)
            {
                if (_hovered != null) _hovered.OnPointerExit(null);
                _hovered = hit;
                if (_hovered != null) _hovered.OnPointerEnter(null);
            }

            // Control: gatillo o A. Manos: pellizco índice-pulgar (Button.One con seguimiento de manos).
            bool press = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                      || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)
                      || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RHand)
                      || OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LHand);
            if (press && _hovered is Button b) b.onClick.Invoke();

            var col = _hovered != null ? hoverColor : idleColor;
            _line.startColor = col;
            _line.endColor = col;
            _line.SetPosition(0, ray.origin);
            _line.SetPosition(1, end);
            _dot.gameObject.SetActive(onCanvas);
            if (onCanvas) _dot.position = end;
        }
    }
}
