using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>Retroalimentación tras cada decisión (y resumen de sesión).</summary>
    public class FeedbackPanel : MonoBehaviour
    {
        public TMP_Text headline;
        public TMP_Text body;
        public TMP_Text continueLabel;
        public Image headerBar;
        public Button continueButton;

        public Color correctColor = new Color(0.20f, 0.58f, 0.32f);
        public Color wrongColor = new Color(0.76f, 0.24f, 0.20f);
        public Color neutralColor = new Color(0.27f, 0.36f, 0.33f);
        public float distance = 0.7f;

        Action _onContinue;
        bool _wired;

        void Awake() => Wire();

        void Wire()
        {
            if (_wired || continueButton == null) return;
            continueButton.onClick.AddListener(Continue);
            _wired = true;
        }

        /// <param name="correct">true = acierto, false = error, null = neutro (resumen).</param>
        public void Show(bool? correct, string title, string text, Action onContinue, string buttonText = "Continuar")
        {
            Wire();
            _onContinue = onContinue;

            if (headline != null) headline.text = title;
            if (body != null) body.text = text;
            if (continueLabel != null) continueLabel.text = buttonText;
            if (headerBar != null)
                headerBar.color = correct == null ? neutralColor : (correct.Value ? correctColor : wrongColor);

            gameObject.SetActive(true);
            XRUi.PlaceInFront(transform, distance);
        }

        public void Hide() => gameObject.SetActive(false);

        void Continue()
        {
            var cb = _onContinue;
            _onContinue = null;
            Hide();
            cb?.Invoke();
        }
    }
}
