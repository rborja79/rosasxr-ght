using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Sigue la MANO DE SOSTÉN (según la lateralidad): mano rastreada o control. El guante solo se dibuja con
    /// controles (con manos se ve la mano real). Sus hijos (punto del ramo) siguen la mano.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class HandAttach : MonoBehaviour
    {
        public GameObject gloveVisual;
        public Vector3 handOffset = new Vector3(0f, -0.02f, 0.05f);
        public Vector3 controllerOffset = new Vector3(0f, -0.02f, 0.03f);

        void LateUpdate()
        {
            if (Application.isEditor) return;
            var b = XRInputBridge.Instance;
            if (b == null) return;
            bool right = b.HoldIsRight;
            bool tracked = b.Tracked(right);
            var anchor = b.Point(right);
            if (anchor == null) return;
            var hand = b.Hand(right);
            var rot = tracked && hand != null && hand.IsPointerPoseValid && hand.PointerPose != null ? hand.PointerPose.rotation : anchor.rotation;
            transform.SetPositionAndRotation(anchor.position + rot * (tracked ? handOffset : controllerOffset), rot);
            if (gloveVisual != null && gloveVisual.activeSelf == tracked) gloveVisual.SetActive(!tracked);
        }
    }
}
