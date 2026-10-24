using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>Etiqueta flotante sobre una rosa detectada (vista "en vivo" para la demo).</summary>
    public class DetectionLabel : MonoBehaviour
    {
        public TMP_Text label;
        public Image background;

        public void Set(string text, Color color, Vector3 position)
        {
            transform.position = position;
            if (label != null) label.text = text;
            if (background != null)
            {
                color.a = 0.85f;
                background.color = color;
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
