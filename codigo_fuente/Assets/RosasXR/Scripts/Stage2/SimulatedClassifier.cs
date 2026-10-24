using System;
using System.Collections;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Interfaz del clasificador del carro. Hoy la implementa un modelo simulado; después se reemplaza
    /// por el modelo del equipo en ONNX (cámara de passthrough + rosas reales) sin tocar el resto.
    /// </summary>
    public interface IGradeClassifier
    {
        bool Available { get; }
        /// <summary>Devuelve (grado predicho 1–6, confianza 0–1) tras una demora.</summary>
        void Classify(HarvestRose rose, bool ambiguous, Action<int, float> done);
    }

    /// <summary>Modelo simulado: NO entrega el grado real directamente; se equivoca con la tasa configurada.</summary>
    public class SimulatedClassifier : MonoBehaviour, IGradeClassifier
    {
        [Range(0f, 1f)] public float errorRate = 0.10f;
        public Vector2 delaySeconds = new Vector2(0.2f, 0.4f);
        public bool available = true;
        public bool Available => available;

        public void Classify(HarvestRose rose, bool ambiguous, Action<int, float> done) =>
            StartCoroutine(Run(rose, ambiguous, done));

        IEnumerator Run(HarvestRose rose, bool ambiguous, Action<int, float> done)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(delaySeconds.x, delaySeconds.y));
            if (rose == null) yield break;
            int g;
            float conf;
            if (rose.forcedPredGrade > 0)
            {
                g = rose.forcedPredGrade;
                conf = rose.forcedConfidence > 0 ? rose.forcedConfidence : 0.9f;
            }
            else
            {
                bool wrong = UnityEngine.Random.value < errorRate;
                g = rose.grade;
                if (wrong) g = Mathf.Clamp(g + (UnityEngine.Random.value < 0.5f ? -1 : 1), 1, 6);
                conf = wrong ? UnityEngine.Random.Range(0.6f, 0.88f) : UnityEngine.Random.Range(0.76f, 0.97f);
                if (rose.occluded) conf *= 0.75f;
                if (ambiguous) conf *= 0.7f;
            }
            done?.Invoke(g, conf);
        }
    }
}
