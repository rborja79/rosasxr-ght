using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>Ícono de menú (≡) en la esquina superior derecha de la vista: al pulsarlo despliega las opciones.</summary>
    public class FloatingMenu : MonoBehaviour
    {
        public Button iconButton;
        public GameObject panel;
        public Button closeButton;
        [Tooltip("Cerrar el panel automáticamente al elegir cualquier opción.")]
        public bool closeOnSelect = true;

        void Awake()
        {
            iconButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (closeOnSelect)
                foreach (var b in panel.GetComponentsInChildren<Button>(true))
                    if (b != closeButton) b.onClick.AddListener(Close);
            panel.SetActive(false);
        }

        public void Toggle() => panel.SetActive(!panel.activeSelf);
        public void Close() => panel.SetActive(false);
    }
}
