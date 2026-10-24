using System;
using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>Una foto real de rosa con sus etiquetas del experto.</summary>
    [Serializable]
    public class RoseSample
    {
        public string sampleId;
        public Texture2D photo;
        [Tooltip("Modelo 3D de la rosa (Etapa 2). Si está, se muestra la rosa en la cama en lugar de la foto.")]
        public GameObject model;

        [Tooltip("Estado de corte (id del catálogo de clases). Se usa en la Etapa 2. Vacío si no aplica.")]
        public string classId;

        [Tooltip("Color (id del catálogo de color). Se usa en la Etapa 1. Vacío si no está etiquetado.")]
        public string colorId;

        [Tooltip("Tamaño (id del catálogo de tamaño). Se usa en la Etapa 1. Vacío si no está etiquetado.")]
        public string sizeId;

        [Tooltip("Largo de corte correcto (id del catálogo de largo). Se usa en la Etapa 2. Vacío si no está etiquetado.")]
        public string lengthId;
    }

    /// <summary>
    /// Conjunto de fotos que usa el simulador. Se llena con el menú
    /// "Rosas XR > 2. Importar fotos al dataset del simulador".
    /// </summary>
    [CreateAssetMenu(menuName = "Rosas XR/Dataset del simulador", fileName = "SimulatorDataset")]
    public class SimulatorDataset : ScriptableObject
    {
        public List<RoseSample> samples = new List<RoseSample>();
    }
}
