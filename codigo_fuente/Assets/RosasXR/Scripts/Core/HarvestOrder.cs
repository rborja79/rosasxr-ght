using System;

namespace RosasXR
{
    /// <summary>Pedido activo: define qué grados se cortan. Lo comparten la etapa 1 y la etapa 2.</summary>
    [Serializable]
    public class HarvestOrder
    {
        public string mercado = "EE. UU.";
        public string variedad = "Rosa roja (variedad por confirmar)";
        public int gradoMin = 3;
        public int gradoMax = 4;
        public int metaTallos = 8;
        [UnityEngine.Tooltip("[Validar con el agrónomo] Largo de tallo aceptado (cm), medido desde el corte hasta la base del botón.")]
        public float largoMinCm = 35f;
        public float largoMaxCm = 50f;

        public bool LengthOk(float cm) => cm >= largoMinCm && cm <= largoMaxCm;

        public bool Accepts(int grado) => grado >= gradoMin && grado <= gradoMax;
        public string Label => $"{mercado} — {variedad} — grados aceptados {gradoMin}–{gradoMax} — tallo {largoMinCm:0}–{largoMaxCm:0} cm";
        public string Short => $"{mercado} · grados {gradoMin}–{gradoMax}";

        public static HarvestOrder Current = new HarvestOrder();
    }
}
