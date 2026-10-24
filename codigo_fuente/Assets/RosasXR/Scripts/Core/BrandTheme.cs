using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Colores y logo de la marca. Se edita el asset Data/BrandTheme y luego se ejecuta
    /// "Rosas XR > 3. Construir escenas y prefabs" para aplicar los cambios.
    /// Valores por defecto: paleta de ghtcorp.com.
    /// </summary>
    [CreateAssetMenu(menuName = "Rosas XR/Tema de marca", fileName = "BrandTheme")]
    public class BrandTheme : ScriptableObject
    {
        public string companyName = "GHT";

        [Tooltip("Logo en PNG con fondo transparente. En el Inspector de la imagen: Texture Type = Sprite (2D and UI). " +
                 "Con los paneles verde oscuro, usar la versión blanca del logo.")]
        public Sprite logo;

        [Header("Colores (paleta GHT)")]
        [Tooltip("Botón principal: Modo 1, 'Evaluar rosa'. GHT #008F59")]
        public Color primary = Hex(0x00, 0x8F, 0x59);
        [Tooltip("Botón secundario: Modo 2. GHT #569966")]
        public Color secondary = Hex(0x56, 0x99, 0x66);
        [Tooltip("Fondo de los paneles. GHT #00301B")]
        public Color panelBackground = Hex(0x00, 0x30, 0x1B, 0.96f);
        [Tooltip("Botones de navegación y encabezado neutro. GHT #045231")]
        public Color neutral = Hex(0x04, 0x52, 0x31);
        [Tooltip("Botón 'Continuar'.")]
        public Color light = Hex(0xF2, 0xF2, 0xF0);

        [Header("Retroalimentación")]
        [Tooltip("Acierto. GHT #1CA36A")]
        public Color correct = Hex(0x1C, 0xA3, 0x6A);
        [Tooltip("Error. Rojo del logo GHT #E51919")]
        public Color wrong = Hex(0xE5, 0x19, 0x19);

        static Color Hex(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);
    }
}
