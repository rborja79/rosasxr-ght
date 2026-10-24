using System;
using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Un estado de la rosa (ej. "punto de corte"). Es el contrato común entre
    /// el simulador, el modelo YOLO y la retroalimentación al operario.
    /// </summary>
    [Serializable]
    public class RoseClass
    {
        [Tooltip("Identificador interno. Debe coincidir con el nombre de la carpeta de fotos (Fotos/<id>).")]
        public string id = "punto_corte";

        [Tooltip("Texto que ve el operario en el botón.")]
        public string displayName = "Punto de corte";

        [Tooltip("Índice de clase que produce el modelo YOLO para este estado (orden de 'names' en el data.yaml). -1 si el modelo no lo predice.")]
        public int modelClassIndex = -1;

        [Tooltip("¿Una rosa en este estado se corta?")]
        public bool shouldCut;

        [Tooltip("Color del botón y de las etiquetas de detección.")]
        public Color color = Color.white;

        [TextArea(2, 5)]
        [Tooltip("Explicación que se muestra en la retroalimentación del simulador.")]
        public string explanation;

        [Tooltip("Recomendación del sistema cuando el modelo predice esta clase con probabilidad >= umbral (ej. ESPERAR).")]
        public string recommendation;
    }

    [CreateAssetMenu(menuName = "Rosas XR/Catálogo de clases", fileName = "RoseClassCatalog")]
    public class RoseClassCatalog : ScriptableObject
    {
        public List<RoseClass> classes = new List<RoseClass>();

        public RoseClass GetById(string id) => classes.Find(c => c.id == id);

        public RoseClass GetByModelIndex(int index) =>
            index < 0 ? null : classes.Find(c => c.modelClassIndex == index);
    }
}
