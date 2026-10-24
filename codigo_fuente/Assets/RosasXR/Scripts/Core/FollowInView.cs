using UnityEngine;

namespace RosasXR
{
    /// <summary>Mantiene un panel (HUD) en el campo de visión sin pegarlo a la cara.</summary>
    public class FollowInView : MonoBehaviour
    {
        public float distance = 0.6f;
        public float heightOffset = -0.25f;
        public float tiltDegrees = 20f;
        [Tooltip("Desplazamiento lateral (positivo = a la derecha de la mirada).")]
        public float sideOffset = 0f;
        [Tooltip("Ángulo a partir del cual el panel se reacomoda frente al usuario.")]
        public float recenterAngle = 40f;
        public float smoothing = 4f;

        bool _moving = true;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) return;
            fwd.Normalize();

            var right = Vector3.Cross(Vector3.up, fwd);
            var target = cam.transform.position + fwd * distance + Vector3.up * heightOffset + right * sideOffset;
            var toPanel = transform.position - cam.transform.position;
            toPanel.y = 0f;

            float angle = Vector3.Angle(fwd, toPanel);
            float drift = Vector3.Distance(transform.position, target);
            if (angle > recenterAngle || drift > 0.5f) _moving = true;

            if (!_moving) return;

            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, k);
            var rot = Quaternion.LookRotation(fwd) * Quaternion.Euler(tiltDegrees, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, k);

            if (Vector3.Distance(transform.position, target) < 0.02f) _moving = false;
        }
    }
}
