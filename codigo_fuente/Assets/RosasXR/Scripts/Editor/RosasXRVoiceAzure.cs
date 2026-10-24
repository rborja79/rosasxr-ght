using System.IO;
using System.Security;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace RosasXR.EditorTools
{
    /// <summary>
    /// Genera los audios de la voz guía con una voz neural de Azure (por defecto Salomé, español de Colombia).
    /// La clave se guarda solo en las preferencias de Unity de este Mac (EditorPrefs): no entra al proyecto,
    /// al repositorio ni al APK. Los audios quedan en Resources/Voz y la app funciona sin internet.
    /// </summary>
    public class RosasXRVoiceAzure : EditorWindow
    {
        const string Dir = "Assets/RosasXR/Resources/Voz";
        const string BackupDir = "Assets/RosasXR/VozRespaldo";
        const string KKey = "RosasXR.Azure.Key", KRegion = "RosasXR.Azure.Region", KVoice = "RosasXR.Azure.Voice", KRate = "RosasXR.Azure.Rate";

        string _key, _region, _voice, _rate, _status = "";

        [MenuItem("Rosas XR/11. Voz neural (Azure: Salomé)", priority = 11)]
        static void Open()
        {
            var w = GetWindow<RosasXRVoiceAzure>(true, "Voz guía · Azure", true);
            w.minSize = new Vector2(460, 260);
        }

        void OnEnable()
        {
            _key = EditorPrefs.GetString(KKey, "");
            _region = EditorPrefs.GetString(KRegion, "eastus");
            _voice = EditorPrefs.GetString(KVoice, "es-CO-SalomeNeural");
            _rate = EditorPrefs.GetString(KRate, "-6%");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Clave y región del recurso «Speech» de Azure", EditorStyles.boldLabel);
            _key = EditorGUILayout.PasswordField("Clave (KEY 1)", _key);
            _region = EditorGUILayout.TextField("Región (ej. eastus)", _region);
            _voice = EditorGUILayout.TextField("Voz", _voice);
            _rate = EditorGUILayout.TextField("Velocidad (ej. -6%)", _rate);
            EditorGUILayout.HelpBox("La clave queda solo en este Mac (preferencias de Unity). No se guarda en el proyecto ni en el APK.", MessageType.Info);
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_key) || string.IsNullOrWhiteSpace(_region)))
            {
                if (GUILayout.Button("Probar con una frase («¡Muy bien, buen corte!»)")) { Save(); Generate(true); }
                if (GUILayout.Button("Generar todas las frases", GUILayout.Height(32))) { Save(); Generate(false); }
            }
            if (GUILayout.Button("Borrar la clave guardada")) { EditorPrefs.DeleteKey(KKey); _key = ""; _status = "Clave borrada."; }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedLabel);
        }

        void Save()
        {
            EditorPrefs.SetString(KKey, _key.Trim());
            EditorPrefs.SetString(KRegion, _region.Trim());
            EditorPrefs.SetString(KVoice, _voice.Trim());
            EditorPrefs.SetString(KRate, _rate.Trim());
        }

        void Generate(bool onlyTest)
        {
            Directory.CreateDirectory(Dir);
            int ok = 0, fail = 0, i = 0;
            var lines = VoiceLines.All;
            string lastError = "";
            foreach (var kv in lines)
            {
                if (onlyTest && kv.Key != "bien_2") continue;
                if (EditorUtility.DisplayCancelableProgressBar("Rosas XR", "Voz Azure: " + kv.Key, (float)i++ / lines.Count)) break;
                var (bytes, err) = Synthesize(kv.Value);
                if (bytes == null) { fail++; lastError = err; Debug.LogError($"[RosasXR][Voz Azure] «{kv.Key}»: {err}"); if (err.StartsWith("HTTP 401") || err.StartsWith("HTTP 403")) break; continue; }
                RosasXRVoice.BackupOther(kv.Key, ".wav");
                File.WriteAllBytes(Path.Combine(Dir, kv.Key + ".wav"), bytes);
                ok++;
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            RosasXRVoice.ApplyImportSettings();
            _status = fail == 0
                ? $"Listo: {ok} audio(s) con la voz {_voice}." + (onlyTest ? " Escucha «bien_2» en Assets/RosasXR/Resources/Voz." : "")
                : $"Generados {ok}, con error {fail}. Último error: {lastError}" +
                  (lastError.StartsWith("HTTP 401") ? "\nRevisa la clave y que la región sea la del recurso." : "");
            Debug.Log("[RosasXR][Voz Azure] " + _status.Replace("\n", " "));
            Repaint();
        }

        (byte[] bytes, string error) Synthesize(string text)
        {
            var lang = _voice.Length >= 5 ? _voice.Substring(0, 5) : "es-CO";
            var ssml =
                $"<speak version='1.0' xml:lang='{lang}' xmlns='http://www.w3.org/2001/10/synthesis'>" +
                $"<voice name='{SecurityElement.Escape(_voice)}'><prosody rate='{SecurityElement.Escape(_rate)}'>{SecurityElement.Escape(text)}</prosody></voice></speak>";
            var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/v1";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(ssml));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Ocp-Apim-Subscription-Key", _key.Trim());
            req.SetRequestHeader("Content-Type", "application/ssml+xml");
            req.SetRequestHeader("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
            req.SetRequestHeader("User-Agent", "RosasXR");
            req.timeout = 30;
            var op = req.SendWebRequest();
            var t0 = EditorApplication.timeSinceStartup;
            while (!op.isDone && EditorApplication.timeSinceStartup - t0 < 35) System.Threading.Thread.Sleep(20);
            if (req.result != UnityWebRequest.Result.Success)
                return (null, $"HTTP {req.responseCode} {req.error}");
            var data = req.downloadHandler.data;
            if (data == null || data.Length < 1000) return (null, "respuesta vacía");
            return (data, null);
        }
    }
}
