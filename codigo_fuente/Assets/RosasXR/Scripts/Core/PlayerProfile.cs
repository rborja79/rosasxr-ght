using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RosasXR
{
    /// <summary>Una respuesta de la etapa 1 (se guarda en el perfil para el candado y la repetición espaciada).</summary>
    [Serializable]
    public class AnswerRecord
    {
        public string photo;     // archivo de la foto
        public string estado;    // estado real (inmaduro | optimo | pasado)
        public int exercise;     // 1 = ¿se corta?, 2 = ¿en qué estado?
        public bool correct;
        public int ms;
        public string when;
    }

    /// <summary>Perfil por usuario guardado en el visor (JSON en persistentDataPath/perfiles).</summary>
    [Serializable]
    public class PlayerProfile
    {
        public string id = "OP-01";
        public int xp;
        public int streak;          // aciertos seguidos (etapa 1)
        public int bestStreak;
        public int lessonsCompleted;
        public bool exercise2Unlocked;
        public bool stage2Unlocked;
        public bool cartTutorialDone;
        public int harvestRounds;
        public int stage2Level = 1; // 1 novato, 2 intermedio, 3 experto
        public bool leftHanded;     // zurdo: la tijera va en la mano izquierda
        public bool voiceOn = true; // voz guía (narración y avisos)
        public List<string> badges = new List<string>();
        public List<AnswerRecord> history = new List<AnswerRecord>();

        public int Level => 1 + xp / 100;

        public bool AddBadge(string b)
        {
            if (badges.Contains(b)) return false;
            badges.Add(b);
            return true;
        }
    }

    /// <summary>Reglas del candado de la etapa 2 (configurables).</summary>
    [Serializable]
    public class UnlockRule
    {
        public int window = 30;               // últimas N respuestas
        public float minAccuracy = 0.80f;     // en esas N
        public float minPerState = 0.70f;     // en CADA estado
        public int minAnswersPerState = 5;    // se reduce si hay pocas fotos de un estado
    }

    public static class ProfileStore
    {
        static PlayerProfile _current;
        public static UnlockRule Rule = new UnlockRule();
        /// <summary>Modo jurado: salta el candado de la etapa 2 para la demostración.</summary>
        public static bool JuryMode = true; // encendido por defecto para la demostración
        /// <summary>Fotos disponibles por estado (lo llena el cargador del manifiesto; ajusta el mínimo por estado).</summary>
        public static Dictionary<string, int> PhotosPerState = new Dictionary<string, int>();

        static string Dir => Path.Combine(Application.persistentDataPath, "perfiles");
        static string PathFor(string id) => Path.Combine(Dir, id + ".json");

        public static PlayerProfile Current
        {
            get
            {
                if (_current == null || _current.id != OperatorSession.OperatorId) _current = Load(OperatorSession.OperatorId);
                return _current;
            }
        }

        public static PlayerProfile Load(string id)
        {
            try
            {
                var p = PathFor(id);
                if (File.Exists(p))
                {
                    var prof = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(p));
                    if (prof != null) { prof.id = id; return prof; }
                }
            }
            catch (Exception e) { Debug.LogWarning("[RosasXR] Perfil ilegible, se crea uno nuevo: " + e.Message); }
            return new PlayerProfile { id = id };
        }

        public static void Save()
        {
            if (_current == null) return;
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(PathFor(_current.id), JsonUtility.ToJson(_current, true));
            }
            catch (Exception e) { Debug.LogError("[RosasXR] No se pudo guardar el perfil: " + e.Message); }
        }

        public static void ResetCurrent()
        {
            _current = new PlayerProfile { id = OperatorSession.OperatorId };
            Save();
        }

        static readonly string[] States = { "inmaduro", "optimo", "pasado" };

        /// <summary>Evalúa el candado. Devuelve true si se cumple; 'missing' explica qué falta.</summary>
        public static bool EvaluateUnlock(PlayerProfile p, out string missing, out string progress)
        {
            var r = Rule;
            var h = p.history;
            int n = Mathf.Min(r.window, h.Count);
            int ok = 0;
            for (int i = h.Count - n; i < h.Count; i++) if (h[i].correct) ok++;
            float acc = n > 0 ? (float)ok / n : 0f;

            var parts = new List<string>();
            var prog = new List<string>();
            if (h.Count < r.window) parts.Add($"responder {r.window - h.Count} fotos más");
            if (acc < r.minAccuracy) parts.Add($"subir el acierto de {acc:P0} a {r.minAccuracy:P0} (últimas {r.window})");
            prog.Add($"Últimas {n}: {acc:P0}");

            foreach (var s in States)
            {
                int avail = PhotosPerState.TryGetValue(s, out var a) ? a : r.minAnswersPerState;
                if (avail == 0) continue;
                int need = Mathf.Max(1, Mathf.Min(r.minAnswersPerState, avail));
                int tot = 0, good = 0;
                foreach (var x in h) if (x.estado == s) { tot++; if (x.correct) good++; }
                float sa = tot > 0 ? (float)good / tot : 0f;
                prog.Add($"{StateName(s)} {sa:P0}");
                if (tot < need) parts.Add($"ver al menos {need - tot} fotos más de «{StateName(s)}»");
                else if (sa < r.minPerState) parts.Add($"«{StateName(s)}» en {sa:P0} (mínimo {r.minPerState:P0})");
            }
            missing = parts.Count == 0 ? "" : "Falta: " + string.Join("; ", parts) + ".";
            progress = string.Join(" · ", prog);
            return parts.Count == 0;
        }

        public static string StateName(string s) => s switch
        {
            "inmaduro" => "Inmaduro",
            "optimo" => "En punto",
            "pasado" => "Pasado",
            _ => s
        };
    }
}
