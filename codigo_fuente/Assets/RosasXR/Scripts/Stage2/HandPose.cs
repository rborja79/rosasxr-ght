using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Lectura simple de la pose de la mano a partir de los huesos de OVRSkeleton (funciona con el esqueleto
    /// OVR y con el de OpenXR, porque busca los huesos por nombre). Todo se normaliza por el largo de la palma,
    /// así sirve para manos grandes y pequeñas.
    /// </summary>
    public class HandPose
    {
        readonly OVRSkeleton _skel;
        Transform _wrist, _middleKnuckle, _indexKnuckle;
        readonly Transform[] _tips = new Transform[4]; // índice, medio, anular, meñique
        bool _ready;

        public HandPose(OVRSkeleton skel) { _skel = skel; }

        public bool Valid => _skel != null && _skel.IsInitialized && _skel.IsDataValid && TryInit();

        bool TryInit()
        {
            if (_ready) return true;
            if (_skel.Bones == null || _skel.Bones.Count == 0) return false;
            foreach (var b in _skel.Bones)
            {
                if (b == null || b.Transform == null) continue;
                // El nombre del objeto del hueso es la etiqueta oficial (Hand_IndexTip, XRHand_IndexTip, ...)
                var n = b.Transform.name;
                if (n.EndsWith("WristRoot") || n.EndsWith("_Wrist")) _wrist = b.Transform;
                else if (n.EndsWith("Middle1") || n.EndsWith("MiddleProximal")) _middleKnuckle = b.Transform;
                else if (n.EndsWith("Index1") || n.EndsWith("IndexProximal")) _indexKnuckle = b.Transform;
                else if (n.EndsWith("IndexTip")) _tips[0] = b.Transform;
                else if (n.EndsWith("MiddleTip")) _tips[1] = b.Transform;
                else if (n.EndsWith("RingTip")) _tips[2] = b.Transform;
                else if (n.EndsWith("PinkyTip") || n.EndsWith("LittleTip")) _tips[3] = b.Transform;
            }
            _ready = _wrist != null && _middleKnuckle != null && _indexKnuckle != null && _tips[0] != null && _tips[1] != null && _tips[2] != null && _tips[3] != null;
            return _ready;
        }

        float Palm => Mathf.Max(0.04f, Vector3.Distance(_wrist.position, _middleKnuckle.position));

        /// <summary>Extensión del dedo: distancia punta–muñeca / largo de palma (~2,0 extendido, ~1,2 recogido).</summary>
        public float Extension(int finger) => Vector3.Distance(_tips[finger].position, _wrist.position) / Palm;

        /// <summary>Separación índice–medio normalizada (para abrir y cerrar la tijera).</summary>
        public float Spread => Vector3.Distance(_tips[0].position, _tips[1].position) / Palm;

        /// <summary>Punta del índice (para pulsar botones con el dedo).</summary>
        public Transform IndexTip => _tips[0];
        /// <summary>Centro de la palma (para agarrar la manija).</summary>
        public Vector3 PalmCenter => Vector3.Lerp(_wrist.position, _middleKnuckle.position, 0.6f);
        /// <summary>Cierre de medio, anular y meñique (agarre de una barra: dedos a medio cerrar).</summary>
        public float GripCurl => (Extension(1) + Extension(2) + Extension(3)) / 3f;

        public bool ScissorPose => Extension(0) > 1.65f && Extension(1) > 1.65f && Extension(2) < 1.5f && Extension(3) < 1.45f;
        public bool Fist => Extension(0) < 1.45f && Extension(1) < 1.45f && Extension(2) < 1.45f;

        /// <summary>Pose para alinear la tijera con los dedos: origen en el nudillo del índice, hacia las puntas.</summary>
        public void ToolPose(out Vector3 pos, out Quaternion rot)
        {
            var tipMid = (_tips[0].position + _tips[1].position) * 0.5f;
            var fwd = (tipMid - _indexKnuckle.position).normalized;
            var side = (_tips[0].position - _tips[1].position);
            var up = Vector3.Cross(fwd, side).normalized;
            if (up.sqrMagnitude < 1e-4f) up = Vector3.up;
            pos = _indexKnuckle.position;
            rot = Quaternion.LookRotation(fwd, up);
        }
    }
}
