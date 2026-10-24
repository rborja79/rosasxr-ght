using System;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Tijeras en la MANO DE TIJERA.
    /// Con manos: aparecen al hacer la pose "tijera" (índice y medio extendidos, demás dedos recogidos) y
    /// reemplazan la mano; se abren según la separación índice–medio y CORTAN al juntarlos (con histéresis).
    /// Si se pierde la pose más de 0,5 s, vuelve a verse la mano.
    /// Con controles: siempre visibles; el gatillo las cierra (corta al pasar de abierta a cerrada).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ScissorsTool : MonoBehaviour
    {
        public GameObject visual;            // malla de tijeras + guante + LED
        public Transform bladeA, bladeB;     // hojas (giran en Y local)
        public Transform tip;                // zona de corte entre las hojas
        public Transform led;
        public float maxOpenDegrees = 30f;

        [Header("Manos (separación índice–medio / palma)")]
        public float openAbove = 0.42f, closeBelow = 0.24f;
        public float poseEnterSeconds = 0.12f, poseExitSeconds = 0.5f;
        public Vector3 handOffset = new Vector3(0f, -0.015f, 0.0f);

        [Header("Controles (gatillo)")]
        public float triggerClose = 0.75f, triggerOpen = 0.35f;
        public Vector3 controllerOffset = new Vector3(0f, -0.01f, 0.03f);
        public Vector3 controllerEuler = new Vector3(20f, 0f, 0f);

        public event Action<Vector3> Cut;
        public bool Active { get; private set; } = true;
        public float Openness { get; private set; } = 1f;
        public bool UsingHands { get; private set; }

        bool _closed;
        float _poseT, _lostT;
        bool? _lastSide;

        void LateUpdate()
        {
            if (Application.isEditor) { SetActive(true); Animate(1f); return; }
            var b = XRInputBridge.Instance;
            if (b == null) return;
            bool right = b.ScissorIsRight;
            if (_lastSide.HasValue && _lastSide.Value != right) ShowHand(true); // cambio de lateralidad
            _lastSide = right;

            UsingHands = b.Tracked(right);
            if (UsingHands)
            {
                var pose = b.Pose(right);
                bool ok = pose.Valid && pose.ScissorPose;
                if (ok) { _poseT += Time.deltaTime; _lostT = 0f; } else { _lostT += Time.deltaTime; _poseT = 0f; }
                if (!Active && _poseT >= poseEnterSeconds) { SetActive(true); _closed = false; }
                else if (Active && _lostT >= poseExitSeconds) SetActive(false);
                ShowHand(!Active, right);

                if (Active && pose.Valid)
                {
                    pose.ToolPose(out var p, out var r);
                    transform.SetPositionAndRotation(p + r * handOffset, r);
                    float spread = pose.Spread;
                    Animate(Mathf.InverseLerp(closeBelow, openAbove + 0.15f, spread));
                    if (!_closed && spread < closeBelow) { _closed = true; Cut?.Invoke(tip.position); }
                    else if (_closed && spread > openAbove) _closed = false;
                }
            }
            else
            {
                ShowHand(true, right);
                SetActive(true);
                var c = b.Controller(right);
                if (c != null)
                {
                    transform.SetPositionAndRotation(c.TransformPoint(controllerOffset), c.rotation * Quaternion.Euler(controllerEuler));
                }
                float t = b.Trigger(right);
                Animate(1f - t);
                if (!_closed && t > triggerClose) { _closed = true; Cut?.Invoke(tip.position); }
                else if (_closed && t < triggerOpen) _closed = false;
            }
        }

        void SetActive(bool on)
        {
            Active = on;
            if (visual != null && visual.activeSelf != on) visual.SetActive(on);
        }

        void Animate(float open01)
        {
            Openness = open01;
            float a = open01 * maxOpenDegrees * 0.5f;
            if (bladeA != null) bladeA.localRotation = Quaternion.Euler(0f, a, 0f);
            if (bladeB != null) bladeB.localRotation = Quaternion.Euler(0f, -a, 0f);
        }

        readonly System.Collections.Generic.Dictionary<bool, (SkinnedMeshRenderer[] s, OVRMeshRenderer[] m)> _handRends =
            new System.Collections.Generic.Dictionary<bool, (SkinnedMeshRenderer[], OVRMeshRenderer[])>();

        (SkinnedMeshRenderer[] s, OVRMeshRenderer[] m) Rends(bool right)
        {
            if (_handRends.TryGetValue(right, out var r) && r.s != null && r.s.Length > 0) return r;
            var b = XRInputBridge.Instance;
            var anchor = b != null ? b.HandAnchor(right) : null;
            r = anchor == null ? (new SkinnedMeshRenderer[0], new OVRMeshRenderer[0])
                               : (anchor.GetComponentsInChildren<SkinnedMeshRenderer>(true), anchor.GetComponentsInChildren<OVRMeshRenderer>(true));
            _handRends[right] = r;
            return r;
        }

        /// <summary>Oculta la malla de la mano de tijera mientras se ven las tijeras.</summary>
        void ShowHand(bool show, bool right)
        {
            var r = Rends(right);
            foreach (var x in r.s) if (x != null && x.enabled != show) x.enabled = show;
            foreach (var x in r.m) if (x != null && x.enabled != show) x.enabled = show;
        }

        /// <summary>Al cambiar de lateralidad, vuelve a mostrar la mano que tenía las tijeras.</summary>
        void ShowHand(bool show)
        {
            if (_lastSide.HasValue) ShowHand(show, _lastSide.Value);
        }
    }
}
