using TMPro;
using UnityEngine;

namespace RosasXR
{
    public class MainMenu : MonoBehaviour
    {
        public TMP_Text operatorLabel;
        public int maxOperators = 20;

        [Tooltip("Al abrir, ubicar el menú frente a la mirada del usuario.")]
        public bool placeInFrontOnStart = true;

        System.Collections.IEnumerator Start()
        {
            Refresh();
            if (!placeInFrontOnStart) yield break;
            // Esperar a que el seguimiento de cabeza entregue una pose real.
            yield return new WaitForSeconds(0.6f);
            XRUi.PlaceInFront(transform, 1.2f, -0.1f);
        }

        public void NextOperator()
        {
            OperatorSession.OperatorNumber = OperatorSession.OperatorNumber % maxOperators + 1;
            Refresh();
        }

        public void PreviousOperator()
        {
            OperatorSession.OperatorNumber = OperatorSession.OperatorNumber <= 1
                ? maxOperators
                : OperatorSession.OperatorNumber - 1;
            Refresh();
        }

        public void StartStage1()
        {
            OperatorSession.Stage = TrainingStage.Identificacion;
            SceneNav.Load(SceneNav.Simulator);
        }

        public void StartStage2()
        {
            OperatorSession.Stage = TrainingStage.Corte;
            SceneNav.Load(SceneNav.Simulator);
        }

        public void StartPractice() => SceneNav.Load(SceneNav.Practice);

        void Refresh()
        {
            if (operatorLabel != null) operatorLabel.text = "Operario: " + OperatorSession.OperatorId;
        }
    }
}
