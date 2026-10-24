using System;
using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>Una opción de un atributo (ej. color "Rojo", tamaño "Mediano").</summary>
    [Serializable]
    public class AttributeOption
    {
        [Tooltip("Identificador interno. Es el valor que se escribe en Fotos/etiquetas.csv.")]
        public string id;
        public string displayName;
        [Tooltip("Color del botón.")]
        public Color color = Color.white;
        [TextArea(2, 4)]
        [Tooltip("Cómo reconocerlo. Se muestra cuando el operario se equivoca.")]
        public string explanation;
    }

    /// <summary>
    /// Atributo que se enseña en la Etapa 1 (color, tamaño...). Cada uno es un asset
    /// en Data/, editable sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Rosas XR/Catálogo de atributo", fileName = "AttributeCatalog")]
    public class AttributeCatalog : ScriptableObject
    {
        public string attributeName = "Color";
        public string question = "¿De qué color es esta rosa?";
        public List<AttributeOption> options = new List<AttributeOption>();

        public AttributeOption GetById(string id) =>
            string.IsNullOrEmpty(id) ? null : options.Find(o => o.id == id);
    }
}
