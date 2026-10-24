using UnityEngine;

namespace RosasXR
{
    /// <summary>Viñeta de confort: oscurece los bordes de la vista mientras el carro se mueve.</summary>
    public class ComfortVignette : MonoBehaviour
    {
        public HarvestCart cart;
        public Renderer quad;
        public float maxAlpha = 0.75f;
        public float speedForMax = 0.6f;
        Material _m;
        float _a;

        void Awake()
        {
            _m = quad.material;
            Apply(0f);
        }

        void LateUpdate()
        {
            float target = cart != null ? Mathf.Clamp01(cart.Speed / speedForMax) * maxAlpha : 0f;
            _a = Mathf.MoveTowards(_a, target, Time.deltaTime * 2f);
            Apply(_a);
        }

        void Apply(float a)
        {
            var c = _m.color; c.a = a; _m.color = c;
            quad.enabled = a > 0.01f;
        }
    }
}
