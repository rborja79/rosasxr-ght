using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RosasXR.EditorTools
{
    /// <summary>
    /// Genera los audios de la voz guía con la voz en español del Mac (comando «say»), sin internet ni licencias
    /// de terceros. Quedan en Assets/RosasXR/Resources/Voz y entran al APK.
    /// </summary>
    public static class RosasXRVoice
    {
        const string Dir = "Assets/RosasXR/Resources/Voz";
        const int Rate = 168; // palabras por minuto: un poco más lento que lo normal, para el invernadero

        [MenuItem("Rosas XR/10. Generar voces (narración)", priority = 10)]
        public static void Generate()
        {
            var voice = PickVoice(out var available);
            Debug.Log("[RosasXR][Voz] Voces en español instaladas: " + (available.Length > 0 ? available : "(ninguna)") + " -> usando: " + (voice ?? "(ninguna)"));
            if (voice == null)
            {
                EditorUtility.DisplayDialog("Rosas XR", "No encontré una voz en español en este Mac.\nInstala una en Ajustes del Sistema > Accesibilidad > Contenido leído > Voz del sistema.", "OK");
                return;
            }
            Directory.CreateDirectory(Dir);
            var tmp = Path.Combine(Path.GetTempPath(), "rosasxr_voz.txt");
            int ok = 0, fail = 0;
            int i = 0, n = VoiceLines.All.Count;
            foreach (var kv in VoiceLines.All)
            {
                EditorUtility.DisplayProgressBar("Rosas XR", "Generando voz: " + kv.Key, (float)i++ / n);
                File.WriteAllText(tmp, kv.Value, new UTF8Encoding(false));
                BackupOther(kv.Key, ".aiff");
                var outPath = Path.GetFullPath(Path.Combine(Dir, kv.Key + ".aiff"));
                var (code, _, err) = Run("/usr/bin/say", $"-v \"{voice}\" -r {Rate} -o \"{outPath}\" -f \"{tmp}\"");
                if (code == 0 && File.Exists(outPath)) ok++;
                else { fail++; Debug.LogError($"[RosasXR][Voz] Falló «{kv.Key}»: {err}"); }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            ApplyImportSettings();
            Debug.Log($"[RosasXR][Voz] Listo: {ok} audios, {fail} con error, voz «{voice}».");
        }

        const string BackupDir = "Assets/RosasXR/VozRespaldo";

        /// <summary>Mueve a VozRespaldo el audio de esa frase en el otro formato, para que no haya dos clips con el mismo nombre.</summary>
        public static void BackupOther(string key, string keepExt)
        {
            foreach (var ext in new[] { ".aiff", ".wav" })
            {
                if (ext == keepExt) continue;
                var src = $"{Dir}/{key}{ext}";
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(src) == null) continue;
                var folder = $"{BackupDir}/{ext.TrimStart('.')}";
                if (!AssetDatabase.IsValidFolder(BackupDir)) AssetDatabase.CreateFolder("Assets/RosasXR", "VozRespaldo");
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(BackupDir, ext.TrimStart('.'));
                var dst = $"{folder}/{key}{ext}";
                if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null) AssetDatabase.DeleteAsset(dst); // respaldo viejo de un audio generado
                var err = AssetDatabase.MoveAsset(src, dst);
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[RosasXR][Voz] No se pudo respaldar " + src + ": " + err);
            }
        }

        /// <summary>Ajustes de importación de los audios de voz: mono, Vorbis, descomprimidos al cargar.</summary>
        public static void ApplyImportSettings()
        {
            foreach (var key in VoiceLines.All.Keys)
                foreach (var ext in new[] { ".aiff", ".wav" })
                {
                    var imp = AssetImporter.GetAtPath($"{Dir}/{key}{ext}") as AudioImporter;
                    if (imp == null) continue;
                    imp.forceToMono = true;
                    var s = imp.defaultSampleSettings;
                    s.loadType = AudioClipLoadType.DecompressOnLoad;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.7f;
                    imp.defaultSampleSettings = s;
                    imp.SaveAndReimport();
                }
        }

        /// <summary>Elige la mejor voz en español instalada: mejorada/premium y latinoamericana primero.</summary>
        static string PickVoice(out string available)
        {
            var (_, output, _) = Run("/usr/bin/say", "-v ?");
            var rx = new Regex(@"^(.+?)\s+(es_[A-Z0-9]+)\s+#", RegexOptions.Multiline);
            var found = rx.Matches(output).Cast<Match>().Select(m => (name: m.Groups[1].Value.Trim(), loc: m.Groups[2].Value)).ToList();
            available = string.Join(", ", found.Select(f => $"{f.name} [{f.loc}]"));
            string[] novelty = { "Eddy", "Flo", "Grandma", "Grandpa", "Reed", "Rocko", "Sandy", "Shelley", "Abuela", "Abuelo" };
            int Score(string name, string loc)
            {
                int s = loc switch { "es_CO" => 6, "es_MX" => 5, "es_US" => 5, "es_419" => 5, "es_AR" => 4, "es_ES" => 3, _ => 2 };
                var low = name.ToLowerInvariant();
                if (low.Contains("premium")) s += 20;
                else if (low.Contains("enhanced") || low.Contains("mejorad")) s += 12;
                if (novelty.Any(nv => name.StartsWith(nv))) s -= 8;
                if (name.StartsWith("Paulina") || name.StartsWith("Mónica") || name.StartsWith("Monica")) s += 2;
                return s;
            }
            var best = found.OrderByDescending(f => Score(f.name, f.loc)).FirstOrDefault();
            return best.name;
        }

        static (int code, string stdout, string stderr) Run(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
                };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd(), e = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);
                return (p.ExitCode, o, e);
            }
            catch (System.Exception ex) { return (-1, "", ex.Message); }
        }
    }
}
