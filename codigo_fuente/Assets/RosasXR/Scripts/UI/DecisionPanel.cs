using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>Una opción de respuesta en el panel.</summary>
    public struct ChoiceOption
    {
        public string id;
        public string label;
        public Color color;
    }

    /// <summary>
    /// Panel compartido: pregunta + (opcional) foto + un botón por opción.
    /// Sirve para cualquier pregunta (color, tamaño, estado de corte).
    /// </summary>
    public class DecisionPanel : MonoBehaviour
    {
        [Tooltip("Catálogo de estados de corte (para la pregunta de corte).")]
        public RoseClassCatalog catalog;
        public TMP_Text promptText;
        public GameObject previewRoot;
        public RawImage preview;
        public AspectRatioFitter previewFitter;
        public Transform buttonContainer;
        public Button classButtonPrefab;
        public float distance = 0.7f;

        readonly List<Button> _pool = new List<Button>();
        Action<int> _onChosen;

        /// <summary>Pregunta genérica: devuelve el índice de la opción elegida.</summary>
        public void Show(string prompt, Texture image, IList<ChoiceOption> options, Action<int> onChosen)
        {
            _onChosen = onChosen;
            EnsurePool(options.Count);

            for (int i = 0; i < _pool.Count; i++)
            {
                var b = _pool[i];
                bool used = i < options.Count;
                b.gameObject.SetActive(used);
                if (!used) continue;

                var opt = options[i];
                var img = b.GetComponent<Image>();
                if (img != null) img.color = opt.color;
                var label = b.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = opt.label;
                    float lum = 0.299f * opt.color.r + 0.587f * opt.color.g + 0.114f * opt.color.b;
                    label.color = lum < 0.5f ? Color.white : new Color(0.08f, 0.08f, 0.08f);
                }
            }

            if (promptText != null) promptText.text = prompt;

            bool hasImage = image != null;
            if (previewRoot != null) previewRoot.SetActive(hasImage);
            if (preview != null) preview.texture = image;
            if (hasImage && previewFitter != null && image.height > 0)
                previewFitter.aspectRatio = image.width / (float)image.height;

            gameObject.SetActive(true);
            XRUi.PlaceInFront(transform, distance);
        }

        /// <summary>Pregunta de estado de corte usando el catálogo de clases.</summary>
        public void Show(string prompt, Texture image, Action<RoseClass> onChosen)
        {
            var opts = new List<ChoiceOption>();
            foreach (var rc in catalog.classes)
                opts.Add(new ChoiceOption { id = rc.id, label = rc.displayName, color = rc.color });
            Show(prompt, image, opts, i => onChosen?.Invoke(catalog.classes[i]));
        }

        /// <summary>Pregunta sobre un atributo (color, tamaño...).</summary>
        public void Show(Texture image, AttributeCatalog attribute, Action<AttributeOption> onChosen)
        {
            var opts = new List<ChoiceOption>();
            foreach (var o in attribute.options)
                opts.Add(new ChoiceOption { id = o.id, label = o.displayName, color = o.color });
            Show(attribute.question, image, opts, i => onChosen?.Invoke(attribute.options[i]));
        }

        public void Hide() => gameObject.SetActive(false);

        void EnsurePool(int count)
        {
            while (_pool.Count < count)
            {
                var b = Instantiate(classButtonPrefab, buttonContainer);
                int index = _pool.Count;
                b.gameObject.name = "Opcion_" + index;
                b.onClick.AddListener(() => Choose(index));
                _pool.Add(b);
            }
        }

        void Choose(int index)
        {
            var cb = _onChosen;
            _onChosen = null;
            Hide();
            cb?.Invoke(index);
        }
    }
}
