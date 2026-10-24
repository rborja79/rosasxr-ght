using UnityEngine;

namespace RosasXR
{
    /// <summary>Solo editor (sin visor): pone las tijeras frente a la cámara para probar con mouse y teclado.</summary>
    public class EditorHandDriver : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0.05f, -0.12f, 0.45f);

        void LateUpdate()
        {
            if (!Application.isEditor) { enabled = false; return; }
            var cam = Camera.main;
            if (cam == null) return;
            transform.position = cam.transform.TransformPoint(offset);
            transform.rotation = cam.transform.rotation;
        }
    }
}
