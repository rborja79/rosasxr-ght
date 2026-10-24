using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>
    /// Pulsar botones de paneles en el mundo tocándolos con la punta del índice (manos rastreadas).
    /// Complementa al rayo: si el panel está al alcance, basta con tocarlo.
    /// </summary>
    public class FingerPoke : MonoBehaviour
    {
        public float pressDepth = 0.012f;     // a menos de 1,2 cm del panel (o lo atraviesa un poco) = pulsar
        public float releaseDistance = 0.035f;
        public float behindTolerance = 0.03f;

        OVRSkeleton[] _skels;
        HandPose[] _poses;
        readonly List<Button> _buttons = new List<Button>();
        float _nextScan;
        readonly bool[] _armed = { true, true };

        void Start()
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            _skels = new OVRSkeleton[2];
            if (rig != null)
            {
                var l = rig.transform.Find("TrackingSpace/LeftHandAnchor");
                var r = rig.transform.Find("TrackingSpace/RightHandAnchor");
                if (l != null) _skels[0] = l.GetComponentInChildren<OVRSkeleton>(true);
                if (r != null) _skels[1] = r.GetComponentInChildren<OVRSkeleton>(true);
            }
            _poses = new[] { new HandPose(_skels[0]), new HandPose(_skels[1]) };
        }

        void Update()
        {
            if (Application.isEditor || _poses == null) return;
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 1.5f;
                _buttons.Clear();
                _buttons.AddRange(FindObjectsByType<Button>(FindObjectsSortMode.None));
            }
            for (int h = 0; h < 2; h++)
            {
                var pose = _poses[h];
                if (pose == null || !pose.Valid || pose.IndexTip == null) { _armed[h] = true; continue; }
                var tip = pose.IndexTip.position;
                Button hit = null;
                float best = float.MaxValue;
                foreach (var b in _buttons)
                {
                    if (b == null || !b.isActiveAndEnabled || !b.interactable) continue;
                    var rt = (RectTransform)b.transform;
                    // distancia con signo al plano del botón (positivo = delante, del lado del usuario)
                    float d = Vector3.Dot(rt.position - tip, rt.forward);
                    if (d > releaseDistance || d < -behindTolerance) continue;
                    var local = rt.InverseTransformPoint(tip);
                    if (!rt.rect.Contains(new Vector2(local.x, local.y))) continue;
                    if (Mathf.Abs(d) < best) { best = Mathf.Abs(d); hit = b; }
                }
                if (hit == null) { _armed[h] = true; continue; }
                if (_armed[h] && best <= pressDepth)
                {
                    _armed[h] = false;
                    hit.onClick.Invoke();
                    if (SoundFx.Instance != null) SoundFx.Play(SoundFx.Instance.click, 0.4f);
                }
            }
        }
    }
}
