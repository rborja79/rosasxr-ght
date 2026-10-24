using UnityEngine.SceneManagement;

namespace RosasXR
{
    /// <summary>Etapas del simulador.</summary>
    public enum TrainingStage
    {
        Identificacion = 1, // Etapa 1: reconocer color y tamaño
        Corte = 2           // Etapa 2: decidir el corte
    }

    /// <summary>Operario y etapa activos. Se eligen en el menú y sobreviven al cambio de escena.</summary>
    public static class OperatorSession
    {
        public static int OperatorNumber = 1;
        public static string OperatorId => $"OP-{OperatorNumber:00}";
        public static TrainingStage Stage = TrainingStage.Identificacion;
    }

    /// <summary>Nombres de escena (deben coincidir con los archivos .unity).</summary>
    public static class SceneNav
    {
        public const string Menu = "Menu";
        public const string Simulator = "Simulador";
        public const string Practice = "PracticaYOLO";
        public const string Stage1 = "Etapa1";
        public const string Harvest = "Cosecha";

        public static void Load(string scene) => SceneManager.LoadScene(scene);
        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);
    }
}
