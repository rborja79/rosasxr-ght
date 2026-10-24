using System.Collections.Generic;

namespace RosasXR
{
    /// <summary>
    /// Catálogo de frases de la voz guía. Las frases se convierten en audio en el editor (menú
    /// "Rosas XR > 10. Generar voces") y quedan dentro del APK: funciona sin internet.
    /// Sin cifras del pedido en las frases, para que no haya que regenerar si cambia el largo.
    /// </summary>
    public static class VoiceLines
    {
        public static readonly Dictionary<string, string> All = new Dictionary<string, string>
        {
            // ── Avisos antes del corte (la tijera ya está en el tallo) ──
            ["aviso_inmadura"] = "Espera. Esta rosa todavía está inmadura, el botón está muy cerrado. No la cortes.",
            ["aviso_pasada"] = "Espera. Esta rosa ya está pasada, el botón está muy abierto. Déjala en la planta.",
            ["aviso_corto"] = "Cuidado, estás muy cerca del botón. El tallo quedaría corto. Baja la tijera.",
            ["aviso_largo"] = "Estás cortando muy abajo. Sube un poco la tijera y deja yemas en la planta.",
            ["aviso_duda"] = "El sistema tiene dudas con esta rosa. Mira bien los sépalos y decide tú.",
            ["ok_aqui"] = "Ahí sí. Corta.",

            // ── Después del corte ──
            ["bien_1"] = "¡Muy bien!",
            ["bien_2"] = "¡Muy bien, buen corte!",
            ["bien_3"] = "¡Excelente!",
            ["bien_4"] = "¡Así se hace!",
            ["bien_criterio"] = "¡Muy bien! Tuviste razón y el sistema no. Eso es criterio.",
            ["mal_inmadura"] = "Esa rosa estaba inmadura. Fíjate en los sépalos antes de cortar.",
            ["mal_pasada"] = "Esa rosa ya estaba pasada. No sirve para el pedido.",
            ["mal_corto"] = "La rosa estaba bien, pero el tallo quedó corto.",
            ["mal_largo"] = "La rosa estaba bien, pero cortaste muy abajo.",
            ["pedido_completo"] = "¡Pedido completo! Muy buen trabajo.",
            ["fin_ronda"] = "Fin de la ronda. Mira tu resumen.",
            ["sistema_caido"] = "El sistema no está disponible. Trabaja con tu criterio.",
            ["sistema_ok"] = "El sistema volvió a funcionar.",

            // ── Tutorial del carro ──
            ["tut_1"] = "Paso uno: mover el carro. Cierra la mano sobre la manija y llévala hacia adelante para avanzar. Hacia ti, retrocede. Al soltar, el carro se detiene. Avanza un metro.",
            ["tut_2"] = "Paso dos: la cámara te sigue. Haz la seña de tijera con tu mano dominante, con el índice y el medio extendidos. Mueve las tijeras y la cámara del brazo las sigue.",
            ["tut_3"] = "Paso tres: el LED de las tijeras. Pon la tijera en el tallo de las rosas de práctica. Verde: corta aquí. Rojo: no cortes. Ámbar parpadeando: el sistema duda. Celeste: la rosa está en punto, baja al tallo.",
            ["tut_4"] = "Paso cuatro: el semáforo del carro. Ámbar fijo: las últimas lecturas tienen poca confianza, revisa con cuidado. Rojo fijo: el sistema no está disponible, trabaja con tu criterio.",
            ["tut_5"] = "Paso cinco: el sistema también se equivoca. Busca la rosa de práctica cinco. El LED dirá cortar, pero mira los sépalos. Córtala o déjala según tu criterio.",
            ["tut_6"] = "Paso seis: sostén el tallo con la otra mano, córtalo con la tijera y abre la mano sobre la canasta azul.",
            ["tut_cortaste"] = "La cortaste, pero estaba inmadura. El sistema se equivocó. Cuando dudes, mira los sépalos: si están pegados, no está en punto.",
            ["tut_bien"] = "¡Muy bien! La dejaste. Estaba inmadura y el sistema decía cortar. Eso es criterio.",
            ["tut_fin"] = "Tutorial completo. ¡A cosechar!",

            // ── Etapa 1: fotos ──
            ["s1_bien_1"] = "¡Muy bien!",
            ["s1_bien_2"] = "¡Correcto!",
            ["s1_bien_3"] = "¡Excelente!",
            ["s1_racha"] = "¡Vas en racha! Sigue así.",
            ["s1_mal"] = "No es correcto. Mira el rasgo clave en la foto.",
        };
    }
}
