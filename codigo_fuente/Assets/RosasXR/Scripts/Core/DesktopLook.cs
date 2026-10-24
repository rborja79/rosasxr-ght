using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RosasXR
{
    /// <summary>
    /// Solo para probar en el editor sin visor: mantener clic derecho y mover el mouse
    /// para mirar alrededor. Se desactiva solo si hay un dispositivo XR activo.
    /// </summary>
    public class DesktopLook : MonoBehaviour
    {
        public float sensitivity = 0.15f;
        [Tooltip("En el editor (sin visor), sube la cámara a esta altura. 0 = no cambiar.")]
        public float editorEyeHeight = 0f;
        float _yaw, _pitch;

        void Start()
        {
            // Solo en el editor. En el visor el XR puede no estar listo en Start, así que no se usa XRSettings.
            if (!Application.isEditor) { enabled = false; return; }
            if (editorEyeHeight > 0f) transform.position = new Vector3(transform.position.x, editorEyeHeight, transform.position.z);
            var e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return;
            Vector2 d = mouse.delta.ReadValue();
#else
            if (!Input.GetMouseButton(1)) return;
            Vector2 d = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
            _yaw += d.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - d.y * sensitivity, -80f, 80f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
