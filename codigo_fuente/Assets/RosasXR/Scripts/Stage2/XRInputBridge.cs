using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Une controles y manos y los organiza por ROLES según la lateralidad del perfil:
    /// MANO DE TIJERA (dominante) y MANO DE SOSTÉN (no dominante). Por cada mano: si está rastreada se usa la
    /// mano, si no el control. Agarrar = grip del control, o pellizco / puño con la mano.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class XRInputBridge : MonoBehaviour
    {
        public static XRInputBridge Instance { get; private set; }

        public Transform leftController, rightController, leftHandAnchor, rightHandAnchor;
        public OVRHand leftHand, rightHand;
        public OVRSkeleton leftSkeleton, rightSkeleton;
        [Range(0f, 1f)] public float pinchOn = 0.7f, pinchOff = 0.4f;

        public bool LeftHanded { get; private set; }
        public event System.Action HandednessChanged;

        HandPose _lPose, _rPose;
        bool _lGrabHand, _rGrabHand;

        // ── Por lado físico ──
        public bool Tracked(bool right) { var h = right ? rightHand : leftHand; return h != null && h.IsTracked; }
        public Transform Point(bool right) => Tracked(right) ? (right ? rightHandAnchor : leftHandAnchor) : (right ? rightController : leftController);
        public Transform Controller(bool right) => right ? rightController : leftController;
        public Transform HandAnchor(bool right) => right ? rightHandAnchor : leftHandAnchor;
        public OVRHand Hand(bool right) => right ? rightHand : leftHand;
        public HandPose Pose(bool right) => right ? _rPose : _lPose;
        /// <summary>Punto de agarre: centro de la palma con manos, el control con controles.</summary>
        public Vector3 GrabPoint(bool right)
        {
            var pose = Pose(right);
            if (Tracked(right) && pose != null && pose.Valid) return pose.PalmCenter;
            var p = Point(right);
            return p != null ? p.position : Vector3.zero;
        }

        public bool Grab(bool right) => OVRInput.Get(right ? OVRInput.RawButton.RHandTrigger : OVRInput.RawButton.LHandTrigger) || (right ? _rGrabHand : _lGrabHand);
        public float Trigger(bool right) => OVRInput.Get(right ? OVRInput.RawAxis1D.RIndexTrigger : OVRInput.RawAxis1D.LIndexTrigger);

        // ── Por rol ──
        public bool ScissorIsRight => !LeftHanded;
        public bool HoldIsRight => LeftHanded;
        public Transform HoldPoint => Point(HoldIsRight);
        public bool HoldGrab => Grab(HoldIsRight);
        public Vector2 Stick { get; private set; }

        void Awake()
        {
            Instance = this;
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                if (leftController == null) leftController = rig.transform.Find("TrackingSpace/LeftControllerAnchor");
                if (rightController == null) rightController = rig.transform.Find("TrackingSpace/RightControllerAnchor");
                if (leftHandAnchor == null) leftHandAnchor = rig.transform.Find("TrackingSpace/LeftHandAnchor");
                if (rightHandAnchor == null) rightHandAnchor = rig.transform.Find("TrackingSpace/RightHandAnchor");
                if (leftHand == null && leftHandAnchor != null) leftHand = leftHandAnchor.GetComponentInChildren<OVRHand>(true);
                if (rightHand == null && rightHandAnchor != null) rightHand = rightHandAnchor.GetComponentInChildren<OVRHand>(true);
                if (leftSkeleton == null && leftHandAnchor != null) leftSkeleton = leftHandAnchor.GetComponentInChildren<OVRSkeleton>(true);
                if (rightSkeleton == null && rightHandAnchor != null) rightSkeleton = rightHandAnchor.GetComponentInChildren<OVRSkeleton>(true);
            }
            _lPose = new HandPose(leftSkeleton);
            _rPose = new HandPose(rightSkeleton);
            LeftHanded = ProfileStore.Current.leftHanded;
        }

        /// <summary>Cambia la lateralidad en caliente (se guarda en el perfil).</summary>
        public void SetLeftHanded(bool left)
        {
            if (LeftHanded == left) return;
            LeftHanded = left;
            ProfileStore.Current.leftHanded = left;
            ProfileStore.Save();
            HandednessChanged?.Invoke();
        }

        void Update()
        {
            _lGrabHand = HandGrab(leftHand, _lPose, _lGrabHand);
            _rGrabHand = HandGrab(rightHand, _rPose, _rGrabHand);
            Stick = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick) + OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        }

        /// <summary>Agarre con la mano: pellizco (con histéresis) o puño.</summary>
        /// <summary>Agarre con la mano: pellizco, o dedos medio/anular/meñique cerrados (como al tomar una barra). Con histéresis.</summary>
        bool HandGrab(OVRHand h, HandPose pose, bool was)
        {
            if (h == null || !h.IsTracked) return false;
            float s = h.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            bool pinch = was ? s > pinchOff : s > pinchOn;
            bool grip = false;
            if (pose != null && pose.Valid)
            {
                float curl = pose.GripCurl;
                grip = was ? curl < 1.78f : curl < 1.6f;
                if (pose.ScissorPose) grip = false; // la seña de tijera no es agarre
            }
            return pinch || grip;
        }
    }
}
