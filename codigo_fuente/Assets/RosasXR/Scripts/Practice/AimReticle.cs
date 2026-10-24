using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>
    /// Recuadro fijo en el centro de la vista: el operario encuadra la rosa aquí.
    /// Sigue a la cámara principal (funciona con el Camera Rig de Meta sin reconfigurar).
    /// El tamaño debe coincidir con el recorte del clasificador: se calibra comparando con la vista previa.
    /// </summary>
    public class AimReticle : MonoBehaviour
    {
        public float distance = 0.6f;
        [Tooltip("Tamaño angular del recuadro. Ajustar hasta que coincida con lo que muestra la vista previa del clasificador.")]
        public float angularSizeDegrees = 25f;

        public Graphic[] frameParts;
        public TMP_Text hint;
        public RawImage preview;
        public bool showPreview = true;

        public Color idleColor = new Color(1f, 1f, 1f, 0.85f);
        public Color lockedColor = new Color(1f, 0.85f, 0.1f, 1f);

        RectTransform _rt;

        void Awake()
        {
            _rt = (RectTransform)transform;
            SetLocked(false);
            if (preview != null) preview.gameObject.SetActive(showPreview);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            transform.position = cam.transform.position + cam.transform.forward * distance;
            transform.rotation = cam.transform.rotation;

            // Ancho en metros → píxeles del canvas (escala 0.001: 1000 px = 1 m)
            float meters = 2f * distance * Mathf.Tan(angularSizeDegrees * 0.5f * Mathf.Deg2Rad);
            float px = meters / Mathf.Max(1e-6f, transform.lossyScale.x);
            _rt.sizeDelta = new Vector2(px, px);
        }

        /// <summary>Colorea el recuadro según el resultado en vivo (null = color normal).</summary>
        public void SetHighlight(Color? c)
        {
            if (frameParts == null) return;
            foreach (var g in frameParts)
                if (g != null) g.color = c ?? idleColor;
        }

        public void SetLocked(bool locked)
        {
            if (frameParts != null)
                foreach (var g in frameParts)
                    if (g != null) g.color = locked ? lockedColor : idleColor;
            if (hint != null)
                hint.text = locked ? "Rosa fijada" : "Encuadra el botón completo";
        }
    }
}
