using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Detecciones falsas para probar la interfaz del modo 2 en el Mac sin visor ni modelo.
    /// En un build para el Quest se apaga sola (onlyInEditor).
    /// </summary>
    public class MockDetectionSource : DetectionSource
    {
        public RoseClassCatalog catalog;
        public bool onlyInEditor = true;
        [Tooltip("Cada cuántos segundos se generan detecciones nuevas.")]
        public float interval = 4f;
        public int maxDetections = 3;
        public float distance = 1.0f;

        readonly List<RoseDetection> _latest = new List<RoseDetection>();
        float _next;

        public override IReadOnlyList<RoseDetection> Latest => _latest;

        void Awake()
        {
            if (onlyInEditor && !Application.isEditor) enabled = false;
        }

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + interval;
            Generate();
        }

        void Generate()
        {
            _latest.Clear();
            var cam = Camera.main;
            if (cam == null) return;

            var indices = new List<int>();
            if (catalog != null)
                foreach (var c in catalog.classes)
                    if (c.modelClassIndex >= 0) indices.Add(c.modelClassIndex);
            if (indices.Count == 0) indices.Add(0);

            int classes = 0;
            foreach (var idx in indices) classes = Mathf.Max(classes, idx + 1);

            int n = Random.Range(1, maxDetections + 1);
            for (int i = 0; i < n; i++)
            {
                var dir = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(-20f, 20f), 0f) * cam.transform.forward;

                // Probabilidades simuladas que suman 1, con una clase dominante.
                int cls = indices[Random.Range(0, indices.Count)];
                var probs = new float[classes];
                float main = Random.Range(0.35f, 0.97f), rest = 1f - main;
                for (int k = 0; k < classes; k++) probs[k] = k == cls ? main : rest / Mathf.Max(1, classes - 1);

                _latest.Add(new RoseDetection
                {
                    classIndex = cls,
                    confidence = main,
                    probabilities = probs,
                    worldPosition = cam.transform.position + dir * distance,
                    hasWorldPosition = true,
                    normalizedCenter = new Vector2(-1f, -1f)
                });
            }
        }
    }
}
