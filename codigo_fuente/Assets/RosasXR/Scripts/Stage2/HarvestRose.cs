using UnityEngine;

namespace RosasXR
{
    /// <summary>Una rosa del invernadero de la etapa 2. El origen del objeto es la base del tallo visible.</summary>
    public class HarvestRose : MonoBehaviour
    {
        public string id;
        public string variedad = "Roja";
        [Range(1, 6)] public int grade = 3;
        public float stemLengthCm = 60f;
        [Tooltip("Botón tapado por hojas: el sistema lo ve con menos confianza.")]
        public bool occluded;
        [Tooltip("Altura del centro del botón sobre el origen (m).")]
        public float budHeight = 0.59f;
        [Tooltip("Altura de la base del botón (donde termina el tallo).")]
        public float budBase = 0.555f;

        [Header("Tutorial: salida forzada del sistema (-1 = normal)")]
        public int forcedPredGrade = -1;
        public float forcedConfidence = -1f;

        // Estado en la ronda
        public bool IsCut { get; private set; }
        public bool Evaluated;
        public string lastVerdict = "";
        public int lastPred = -1;
        public float lastConf = -1f;

        public Vector3 BudPos => transform.position + transform.up * budHeight;
        public Vector3 BudBasePos => transform.position + transform.up * budBase;

        /// <summary>Distancia del punto al eje del tallo (infinito si está fuera del tramo cortable).</summary>
        public float DistanceToStem(Vector3 p, out float alongM)
        {
            var up = transform.up;
            alongM = Vector3.Dot(p - transform.position, up);
            if (alongM < 0.02f || alongM > budBase - 0.01f) return float.PositiveInfinity;
            var onAxis = transform.position + up * alongM;
            return Vector3.Distance(p, onAxis);
        }

        /// <summary>Corta el tallo en 'alongM' y pasa la parte cortada a la mano (o la suelta si hand es null). Devuelve el largo del tallo cortado (cm).</summary>
        public float CutAt(float alongM, Transform hand)
        {
            IsCut = true;
            float cm = (budBase - alongM) * 100f;
            if (hand != null)
            {
                // La parte cortada (desde el corte hasta el botón) queda en la mano de sostén
                transform.SetParent(hand, true);
                transform.position = hand.position - transform.up * alongM;
            }
            else
            {
                // Sin sostener: se suelta de la planta en el punto de corte (luego cae)
                transform.SetParent(null, true);
                transform.position += transform.up * alongM * 0.02f;
            }
            var lod = GetComponent<LODGroup>();
            if (lod != null) lod.ForceLOD(0);
            return cm;
        }
    }
}
