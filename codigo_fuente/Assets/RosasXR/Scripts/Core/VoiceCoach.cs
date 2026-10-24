using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Voz guía: reproduce las frases pregrabadas de <see cref="VoiceLines"/> (Resources/Voz/clave).
    /// Una sola voz a la vez, con prioridad: 3 = tutorial, 2 = resultado de un corte, 1 = aviso.
    /// Una frase de prioridad menor no interrumpe a una mayor. Se apaga desde el menú (se guarda en el perfil).
    /// Si los audios no se han generado, no hace nada.
    /// </summary>
    public class VoiceCoach : MonoBehaviour
    {
        static VoiceCoach _i;
        AudioSource _src;
        int _prio;
        string _lastKey;
        readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> _lastAt = new Dictionary<string, float>();

        static VoiceCoach I
        {
            get
            {
                if (_i == null) _i = new GameObject("VozGuia").AddComponent<VoiceCoach>();
                return _i;
            }
        }

        void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false; _src.spatialBlend = 0f; _src.priority = 0; _src.volume = 1f;
        }

        public static bool On
        {
            get => ProfileStore.Current.voiceOn;
            set
            {
                ProfileStore.Current.voiceOn = value;
                ProfileStore.Save();
                if (!value) Stop();
            }
        }

        public static bool Speaking => _i != null && _i._src != null && _i._src.isPlaying;

        public static void Stop()
        {
            if (_i != null && _i._src != null) _i._src.Stop();
        }

        /// <summary>Dice la frase. Devuelve false si no se dijo (voz apagada, ocupada con algo más importante o en espera).</summary>
        public static bool Say(string key, int priority = 1, float cooldown = 0f)
        {
            if (string.IsNullOrEmpty(key) || !On) return false;
            var v = I;
            if (cooldown > 0f && v._lastAt.TryGetValue(key, out var t) && Time.time - t < cooldown) return false;
            if (v._src.isPlaying && v._prio > priority) return false;
            if (v._src.isPlaying && v._prio == priority && priority <= 1) return false; // un aviso no corta a otro aviso
            var clip = v.Clip(key);
            if (clip == null) return false;
            v._src.Stop();
            v._src.clip = clip;
            v._src.Play();
            v._prio = priority;
            v._lastKey = key;
            v._lastAt[key] = Time.time;
            Debug.Log("[Voz] " + key);
            return true;
        }

        /// <summary>Dice una de varias frases al azar (sin repetir la última).</summary>
        public static bool SayOneOf(int priority, params string[] keys)
        {
            if (keys == null || keys.Length == 0) return false;
            string pick = keys[Random.Range(0, keys.Length)];
            if (keys.Length > 1 && _i != null && pick == _i._lastKey) pick = keys[(System.Array.IndexOf(keys, pick) + 1) % keys.Length];
            return Say(pick, priority);
        }

        AudioClip Clip(string key)
        {
            if (_cache.TryGetValue(key, out var c)) return c;
            c = Resources.Load<AudioClip>("Voz/" + key);
            _cache[key] = c;
            if (c == null) Debug.LogWarning("[Voz] Falta el audio de «" + key + "» (Rosas XR > 10. Generar voces)");
            return c;
        }
    }
}
