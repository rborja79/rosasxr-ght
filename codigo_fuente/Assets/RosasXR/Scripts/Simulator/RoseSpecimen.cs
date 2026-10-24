using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>Tarjeta de una rosa en la estación del simulador (foto real + respuesta conocida).</summary>
    [RequireComponent(typeof(Button))]
    public class RoseSpecimen : RoseItem
    {
        public RawImage photo;
        public Image frame;
        public Color correctColor = new Color(0.35f, 0.78f, 0.45f);
        public Color wrongColor = new Color(0.90f, 0.35f, 0.30f);

        Button _button;

        public override void Setup(RoseSample sample)
        {
            Sample = sample;
            if (photo != null) photo.texture = sample.photo;
            _button = GetComponent<Button>();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(RaiseSelected);
        }

        public override void MarkResult(bool correct)
        {
            Evaluated = true;
            if (frame != null) frame.color = correct ? correctColor : wrongColor;
            if (_button != null) _button.interactable = false;
        }
    }
}
