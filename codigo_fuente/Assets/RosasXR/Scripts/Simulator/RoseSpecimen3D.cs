using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RosasXR
{
    /// <summary>
    /// Rosa 3D en la cama. Se selecciona apuntando y pulsando (rayo o mouse).
    /// Para evaluar el largo de corte muestra anillos sobre el tallo medidos desde la punta del botón.
    /// </summary>
    public class RoseSpecimen3D : RoseItem, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Color hoverColor = new Color(1f, 0.85f, 0.2f);
        public Color correctColor = new Color(0.11f, 0.64f, 0.42f);
        public Color wrongColor = new Color(0.90f, 0.10f, 0.10f);

        GameObject _model, _marker;
        Renderer _markerRenderer;
        readonly List<GameObject> _cutMarks = new List<GameObject>();
        Bounds _bounds;

        public float StemTopY => _bounds.max.y;

        public override void Setup(RoseSample sample)
        {
            Sample = sample;
            _model = Instantiate(sample.model, transform);
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Límites en espacio mundo para el colisionador y las marcas
            var rends = _model.GetComponentsInChildren<Renderer>();
            _bounds = rends.Length > 0 ? rends[0].bounds : new Bounds(transform.position, Vector3.one * 0.2f);
            foreach (var r in rends) _bounds.Encapsulate(r.bounds);

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = transform.InverseTransformPoint(_bounds.center);
            var size = transform.InverseTransformVector(_bounds.size);
            col.size = new Vector3(Mathf.Max(Mathf.Abs(size.x), 0.18f), Mathf.Abs(size.y), Mathf.Max(Mathf.Abs(size.z), 0.18f));

            // Indicador sobre el botón (resalta al apuntar y muestra el resultado)
            _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_marker.GetComponent<Collider>());
            _marker.name = "Indicador";
            _marker.transform.SetParent(transform, false);
            _marker.transform.position = new Vector3(_bounds.center.x, _bounds.max.y + 0.06f, _bounds.center.z);
            _marker.transform.localScale = Vector3.one * 0.03f;
            _markerRenderer = _marker.GetComponent<Renderer>();
            _markerRenderer.material.color = hoverColor;
            _marker.SetActive(false);
        }

        public void OnPointerClick(PointerEventData e) => RaiseSelected();
        public void OnPointerEnter(PointerEventData e) { if (!Evaluated) _marker.SetActive(true); }
        public void OnPointerExit(PointerEventData e) { if (!Evaluated) _marker.SetActive(false); }

        public override void MarkResult(bool correct)
        {
            Evaluated = true;
            HideCutMarks();
            _marker.SetActive(true);
            _markerRenderer.material.color = correct ? correctColor : wrongColor;
            _marker.transform.localScale = Vector3.one * 0.05f;
        }

        /// <summary>Muestra anillos con etiqueta a cada largo (cm) medido desde la punta del botón.</summary>
        public void ShowCutMarks(IList<AttributeOption> options)
        {
            HideCutMarks();
            foreach (var o in options)
            {
                if (!int.TryParse(o.id, out int cm)) continue;
                float y = StemTopY - cm / 100f;
                if (y < _bounds.min.y + 0.02f) continue;

                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(ring.GetComponent<Collider>());
                ring.name = "Marca_" + cm;
                ring.transform.SetParent(transform, false);
                ring.transform.position = new Vector3(_bounds.center.x, y, _bounds.center.z);
                ring.transform.localScale = new Vector3(0.05f, 0.002f, 0.05f);
                ring.GetComponent<Renderer>().material.color = o.color;
                _cutMarks.Add(ring);

                var labelGo = new GameObject("Etiqueta_" + cm, typeof(RectTransform));
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.position = ring.transform.position + Vector3.up * 0.012f;
                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.text = o.displayName;
                tmp.fontSize = 0.35f;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.color = Color.white;
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = Color.black;
                ((RectTransform)labelGo.transform).sizeDelta = new Vector2(0.2f, 0.05f);
                labelGo.AddComponent<FaceCamera>().offsetRight = 0.12f;
                _cutMarks.Add(labelGo);
            }
        }

        public void HideCutMarks()
        {
            foreach (var g in _cutMarks) if (g != null) Destroy(g);
            _cutMarks.Clear();
        }
    }

    /// <summary>Orienta un texto hacia la cámara, desplazado a un lado del tallo.</summary>
    public class FaceCamera : MonoBehaviour
    {
        public float offsetRight = 0.1f;
        Vector3 _anchor;
        void Start() => _anchor = transform.position;
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var right = cam.transform.right; right.y = 0; right.Normalize();
            transform.position = _anchor + right * offsetRight;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
