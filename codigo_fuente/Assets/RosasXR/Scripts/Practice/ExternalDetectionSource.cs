using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Punto de conexión con el modelo YOLO real. El código de inferencia (sample de Meta)
    /// publica aquí sus resultados en cada ciclo:
    ///
    ///   var src = ExternalDetectionSource.Instance;
    ///   if (src != null) {
    ///       src.BeginFrame();
    ///       foreach (cada caja detectada)
    ///           src.Add(claseId, puntaje, posicionMundo3D);
    ///       src.EndFrame();
    ///   }
    /// </summary>
    public class ExternalDetectionSource : DetectionSource
    {
        public static ExternalDetectionSource Instance { get; private set; }

        [Tooltip("Si no llegan detecciones nuevas en este tiempo, se considera que no hay ninguna.")]
        public float maxAgeSeconds = 1.0f;

        static readonly List<RoseDetection> Empty = new List<RoseDetection>();
        List<RoseDetection> _latest = new List<RoseDetection>();
        List<RoseDetection> _building = new List<RoseDetection>();
        float _stamp = -999f;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public override IReadOnlyList<RoseDetection> Latest =>
            Time.time - _stamp <= maxAgeSeconds ? _latest : Empty;

        public void BeginFrame() => _building.Clear();

        public void Add(int classIndex, float confidence, Vector3 worldPosition, float[] probabilities = null)
        {
            _building.Add(new RoseDetection
            {
                classIndex = classIndex,
                confidence = confidence,
                worldPosition = worldPosition,
                hasWorldPosition = true,
                normalizedCenter = new Vector2(-1f, -1f),
                probabilities = probabilities
            });
        }

        /// <summary>Para cuando solo se tiene la caja 2D (sin posición 3D).</summary>
        public void Add(int classIndex, float confidence, Vector2 normalizedCenter, float[] probabilities = null)
        {
            _building.Add(new RoseDetection
            {
                classIndex = classIndex,
                confidence = confidence,
                hasWorldPosition = false,
                normalizedCenter = normalizedCenter,
                probabilities = probabilities
            });
        }

        public void EndFrame()
        {
            (_latest, _building) = (_building, _latest);
            _stamp = Time.time;
        }

        public void Publish(IEnumerable<RoseDetection> detections)
        {
            BeginFrame();
            _building.AddRange(detections);
            EndFrame();
        }
    }
}
