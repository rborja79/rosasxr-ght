using UnityEngine;

namespace RosasXR
{
    /// <summary>Efectos de sonido sencillos (sonidos generados para el proyecto, sin licencias de terceros).</summary>
    public class SoundFx : MonoBehaviour
    {
        public static SoundFx Instance { get; private set; }

        public AudioClip ambient, click, cartLoop, leaves, step, good, bad, deposit;
        [Range(0f, 1f)] public float ambientVolume = 0.35f;

        [Header("Opcional (etapa 2)")]
        public HarvestCart cart;
        public Transform[] handsForLeaves;     // tijeras y guante: roce con el follaje
        public float foliageX = 0.38f;         // |x| a partir del cual la mano está dentro de la cama

        AudioSource _amb, _cart, _oneShot;
        Vector3 _lastHead;
        float _stepAcc, _leafCooldown;
        Vector3[] _lastHand;

        void Awake()
        {
            Instance = this;
            _oneShot = gameObject.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            if (ambient != null)
            {
                _amb = gameObject.AddComponent<AudioSource>();
                _amb.clip = ambient; _amb.loop = true; _amb.volume = ambientVolume; _amb.playOnAwake = false;
                _amb.Play();
            }
            if (cart != null && cartLoop != null)
            {
                _cart = cart.gameObject.AddComponent<AudioSource>();
                _cart.clip = cartLoop; _cart.loop = true; _cart.volume = 0f; _cart.spatialBlend = 0.7f;
                _cart.Play();
            }
            if (handsForLeaves != null) _lastHand = new Vector3[handsForLeaves.Length];
        }

        public static void Play(AudioClip c, float vol = 1f, Vector3? at = null)
        {
            var s = Instance;
            if (s == null || c == null) return;
            if (at.HasValue) AudioSource.PlayClipAtPoint(c, at.Value, vol);
            else s._oneShot.PlayOneShot(c, vol);
        }

        void Update()
        {
            var cam = Camera.main;
            if (_cart != null) _cart.volume = Mathf.Lerp(_cart.volume, Mathf.Clamp01(cart.Speed * 0.9f), 0.15f);
            // Baja el ambiente mientras habla la voz guía
            if (_amb != null) _amb.volume = Mathf.Lerp(_amb.volume, ambientVolume * (VoiceCoach.Speaking ? 0.35f : 1f), 0.1f);

            // Pasos: al caminar físicamente o empujar el carro
            if (cam != null && step != null)
            {
                var h = cam.transform.position; h.y = 0f;
                if (_lastHead != Vector3.zero) _stepAcc += Vector3.Distance(h, _lastHead);
                _lastHead = h;
                if (_stepAcc > 0.65f) { _stepAcc = 0f; Play(step, 0.5f); }
            }

            // Roce de hojas: mano rápida dentro del follaje
            _leafCooldown -= Time.deltaTime;
            if (handsForLeaves != null && leaves != null)
                for (int i = 0; i < handsForLeaves.Length; i++)
                {
                    var t = handsForLeaves[i];
                    if (t == null) continue;
                    var p = t.position;
                    float v = Time.deltaTime > 0 ? Vector3.Distance(p, _lastHand[i]) / Time.deltaTime : 0f;
                    _lastHand[i] = p;
                    if (_leafCooldown <= 0f && Mathf.Abs(p.x) > foliageX && p.y > 0.4f && v > 0.5f && v < 10f)
                    {
                        Play(leaves, Mathf.Clamp01(0.3f + v * 0.2f), p);
                        _leafCooldown = 0.45f;
                    }
                }
        }
    }
}
