using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>Pantalla de inicio: perfil, progreso y mapa de etapas (etapa 2 con candado).</summary>
    public class HomeMenu : MonoBehaviour
    {
        public TMP_Text operatorLabel;
        public TMP_Text progressText;
        public TMP_Text stage1Status;
        public TMP_Text stage2Status;
        public Image stage2Button;
        public TMP_Text juryLabel;
        public TMP_Text handLabel;
        public TMP_Text messageText;
        public Color lockedColor = new Color(0.35f, 0.38f, 0.36f);
        public Color unlockedColor = new Color(0f, 0.56f, 0.35f);
        public int maxOperators = 12;

        System.Collections.IEnumerator Start()
        {
            yield return PhotoManifest.Load(); // para conocer cuántas fotos hay por estado
            Refresh();
            yield return new WaitForSeconds(0.6f);
            XRUi.PlaceInFront(transform, 1.3f, -0.1f);
        }

        public void NextOperator()
        {
            OperatorSession.OperatorNumber = OperatorSession.OperatorNumber % maxOperators + 1;
            Refresh();
        }

        public void PreviousOperator()
        {
            OperatorSession.OperatorNumber = OperatorSession.OperatorNumber <= 1 ? maxOperators : OperatorSession.OperatorNumber - 1;
            Refresh();
        }

        public void StartStage1() => SceneNav.Load(SceneNav.Stage1);

        public void StartStage2()
        {
            var p = ProfileStore.Current;
            if (!p.stage2Unlocked && !ProfileStore.JuryMode)
            {
                ProfileStore.EvaluateUnlock(p, out var missing, out _);
                if (messageText != null) messageText.text = "La etapa 2 está bloqueada. " + missing;
                return;
            }
            SceneNav.Load(SceneNav.Harvest);
        }

        public void StartPractice() => SceneNav.Load(SceneNav.Practice);

        public void ToggleJury()
        {
            ProfileStore.JuryMode = !ProfileStore.JuryMode;
            Refresh();
        }

        public void ToggleHandedness()
        {
            var p = ProfileStore.Current;
            p.leftHanded = !p.leftHanded;
            ProfileStore.Save();
            Refresh();
        }

        public void ResetProfile()
        {
            ProfileStore.ResetCurrent();
            Refresh();
            if (messageText != null) messageText.text = "Perfil reiniciado.";
        }

        void Refresh()
        {
            var p = ProfileStore.Current;
            if (operatorLabel != null) operatorLabel.text = "Usuario: " + p.id;
            if (progressText != null)
                progressText.text = $"Nivel {p.Level}  ·  {p.xp} XP  ·  Mejor racha {p.bestStreak}  ·  Lecciones {p.lessonsCompleted}  ·  Rondas de cosecha {p.harvestRounds}" +
                                    (p.badges.Count > 0 ? "\nInsignias: " + string.Join(", ", p.badges) : "");

            bool unlocked = ProfileStore.EvaluateUnlock(p, out var missing, out var prog);
            if (unlocked && !p.stage2Unlocked) { p.stage2Unlocked = true; ProfileStore.Save(); }

            if (stage1Status != null) stage1Status.text = prog;
            bool open = p.stage2Unlocked || ProfileStore.JuryMode;
            if (stage2Status != null)
                stage2Status.text = p.stage2Unlocked ? "Desbloqueada" + (p.cartTutorialDone ? "" : " · empieza con el tutorial del carro")
                    : ProfileStore.JuryMode ? "Abierta por modo jurado" : "[Bloqueada] " + missing;
            if (stage2Button != null) stage2Button.color = open ? unlockedColor : lockedColor;
            if (handLabel != null) handLabel.text = p.leftHanded ? "Zurdo (tijera izquierda)" : "Diestro (tijera derecha)";
            if (juryLabel != null) juryLabel.text = ProfileStore.JuryMode ? "Modo jurado: SÍ" : "Modo jurado: NO";
            if (messageText != null) messageText.text = "";
        }
    }
}
