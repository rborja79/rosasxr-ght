using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>
    /// En el visor activa el sistema de puntero oficial de Meta (OVRInputModule + OVRRaycaster),
    /// que funciona con controles y con manos. En el editor deja el mouse.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(EventSystem))]
    public class XRInputSetup : MonoBehaviour
    {
        public OVRInputModule ovrModule;

        Transform _controllerRay;
        OVRHand _rightHand;

        void Update()
        {
            if (Application.isEditor || ovrModule == null) return;
            // Con las manos, el rayo sale de la pose de puntero de la mano derecha; con controles, del control.
            var active = OVRInput.GetActiveController();
            bool hands = active == OVRInput.Controller.Hands || active == OVRInput.Controller.RHand || active == OVRInput.Controller.LHand;
            if (hands && _rightHand != null && _rightHand.IsTracked && _rightHand.IsPointerPoseValid && _rightHand.PointerPose != null)
                ovrModule.rayTransform = _rightHand.PointerPose;
            else if (_controllerRay != null)
                ovrModule.rayTransform = _controllerRay;
        }

        void Awake()
        {
            bool device = !Application.isEditor;
            _controllerRay = ovrModule != null ? ovrModule.rayTransform : null;
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null && rig.rightHandAnchor != null) _rightHand = rig.rightHandAnchor.GetComponentInChildren<OVRHand>(true);

            foreach (var m in GetComponents<BaseInputModule>())
                m.enabled = (m == ovrModule) ? device : !device;

            if (!device) return;

            // En el visor, los objetos 3D se apuntan con OVRPhysicsRaycaster (no con el del mouse)
            foreach (var pr in FindObjectsByType<PhysicsRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (pr.GetType() == typeof(PhysicsRaycaster)) pr.enabled = false;

            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.WorldSpace) continue;
                bool hasSelectables = c.GetComponentInChildren<Selectable>(true) != null
                                      || c.GetComponentInChildren<HorizontalOrVerticalLayoutGroup>(true) != null;
                if (!hasSelectables) continue;

                if (c.GetComponent<OVRRaycaster>() == null) c.gameObject.AddComponent<OVRRaycaster>();
                foreach (var gr in c.GetComponents<GraphicRaycaster>())
                    if (gr.GetType() == typeof(GraphicRaycaster)) gr.enabled = false;
                if (c.worldCamera == null) c.worldCamera = Camera.main;
            }
        }
    }
}
