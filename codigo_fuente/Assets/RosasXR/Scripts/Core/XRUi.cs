using UnityEngine;

namespace RosasXR
{
    public static class XRUi
    {
        /// <summary>Ubica un canvas de mundo frente a la cabeza del usuario, mirándolo.</summary>
        public static void PlaceInFront(Transform t, float distance = 0.7f, float verticalOffset = -0.1f)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();

            t.position = cam.transform.position + fwd * distance + Vector3.up * verticalOffset;
            // Un canvas de mundo se ve de frente cuando su +Z apunta lejos de la cámara.
            t.rotation = Quaternion.LookRotation(fwd);
        }
    }
}
