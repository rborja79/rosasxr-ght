using System;
using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>Una detección del modelo YOLO, ya traducida a algo que la interfaz entiende.</summary>
    [Serializable]
    public struct RoseDetection
    {
        public int classIndex;          // índice de clase del modelo (orden del data.yaml)
        public float confidence;        // 0..1
        public Vector3 worldPosition;   // posición 3D (Depth API del sample de Meta)
        public bool hasWorldPosition;
        public Vector2 normalizedCenter; // centro de la caja en la imagen (0..1); (-1,-1) si no se conoce
        public float[] probabilities;    // probabilidad por clase (clasificador); null si no se conoce
    }

    /// <summary>
    /// Fuente de detecciones. La interfaz no sabe de dónde vienen: del modelo real
    /// (ExternalDetectionSource) o simuladas en el editor (MockDetectionSource).
    /// </summary>
    public abstract class DetectionSource : MonoBehaviour
    {
        public abstract IReadOnlyList<RoseDetection> Latest { get; }
    }
}
