using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace RosasXR
{
    /// <summary>Una foto real del manifiesto (StreamingAssets/fotos/manifiesto.csv).</summary>
    [Serializable]
    public class PhotoEntry
    {
        public string archivo;        // ruta relativa a StreamingAssets/fotos
        public string variedad;
        public string color;
        public string tamano;
        public int grado;             // 1–6 (aproximado si se etiquetó por carpeta)
        public string estado;         // inmaduro | optimo | pasado
        public string etiquetadoPor;
        public string notas;
        public string rasgo;          // rasgo clave a resaltar en la retroalimentación (opcional)
        public bool referencia;       // usar como foto de referencia de su estado
        public Rect zona;             // zona de resaltado normalizada (0..1), opcional
        public bool HasZone => zona.width > 0f && zona.height > 0f;
    }

    /// <summary>
    /// Carga el manifiesto y las fotos en tiempo de ejecución desde StreamingAssets
    /// (en el Quest las fotos van dentro del APK y se leen con UnityWebRequest).
    /// Formato CSV (separador ;):
    /// archivo;variedad;color;tamano;grado;estado;etiquetado_por;notas;rasgo;referencia;zona
    /// zona = x,y,ancho,alto normalizados (0..1, origen arriba a la izquierda) o vacío.
    /// La decisión cortar / no cortar NO va en el manifiesto: se calcula con el pedido.
    /// </summary>
    public static class PhotoManifest
    {
        public const string Folder = "fotos";
        public const string FileName = "manifiesto.csv";
        public static int MaxSize = 1024;

        public static List<PhotoEntry> Entries { get; private set; } = new List<PhotoEntry>();
        public static bool Loaded { get; private set; }
        public static string Error { get; private set; }

        static readonly Dictionary<string, Texture> _cache = new Dictionary<string, Texture>();
        static readonly LinkedList<string> _lru = new LinkedList<string>();
        public static int CacheLimit = 24;

        static string Url(string rel)
        {
            var p = System.IO.Path.Combine(Application.streamingAssetsPath, Folder, rel).Replace('\\', '/');
            return p.Contains("://") ? p : "file://" + p;
        }

        public static IEnumerator Load()
        {
            if (Loaded) yield break;
            using (var req = UnityWebRequest.Get(Url(FileName)))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Error = "No encontré " + Folder + "/" + FileName + ": " + req.error;
                    Debug.LogError("[RosasXR] " + Error);
                    yield break;
                }
                Parse(req.downloadHandler.text);
            }
            ProfileStore.PhotosPerState.Clear();
            foreach (var e in Entries)
            {
                ProfileStore.PhotosPerState.TryGetValue(e.estado, out var c);
                ProfileStore.PhotosPerState[e.estado] = c + 1;
            }
            Loaded = true;
            Debug.Log($"[RosasXR] Manifiesto: {Entries.Count} fotos");
        }

        static void Parse(string text)
        {
            Entries.Clear();
            var lines = text.Replace("\r", "").Split('\n');
            if (lines.Length == 0) return;
            char sep = lines[0].Contains(";") ? ';' : ',';
            var head = lines[0].TrimStart('﻿').Split(sep);
            int Col(string name) => Array.FindIndex(head, h => h.Trim().ToLowerInvariant() == name);
            int cA = Col("archivo"), cV = Col("variedad"), cC = Col("color"), cT = Col("tamano"), cG = Col("grado"),
                cE = Col("estado"), cP = Col("etiquetado_por"), cN = Col("notas"), cR = Col("rasgo"),
                cRef = Col("referencia"), cZ = Col("zona");
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var p = lines[i].Split(sep);
                string Get(int c) => c >= 0 && c < p.Length ? p[c].Trim().Trim('"') : "";
                var e = new PhotoEntry
                {
                    archivo = Get(cA), variedad = Get(cV), color = Get(cC), tamano = Get(cT),
                    estado = Get(cE).ToLowerInvariant(), etiquetadoPor = Get(cP), notas = Get(cN), rasgo = Get(cR),
                    referencia = Get(cRef) == "1" || Get(cRef).ToLowerInvariant() == "si"
                };
                int.TryParse(Get(cG), out e.grado);
                var z = Get(cZ).Split(',');
                if (z.Length == 4 &&
                    float.TryParse(z[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var zx) &&
                    float.TryParse(z[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var zy) &&
                    float.TryParse(z[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var zw) &&
                    float.TryParse(z[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var zh))
                    e.zona = new Rect(zx, zy, zw, zh);
                if (string.IsNullOrEmpty(e.estado) && e.grado > 0)
                    e.estado = e.grado <= 2 ? "inmaduro" : e.grado <= 4 ? "optimo" : "pasado";
                if (!string.IsNullOrEmpty(e.archivo) && !string.IsNullOrEmpty(e.estado)) Entries.Add(e);
            }
        }

        public static bool TryGet(string archivo, out Texture tex)
        {
            if (_cache.TryGetValue(archivo, out tex) && tex != null)
            {
                _lru.Remove(archivo); _lru.AddFirst(archivo);
                return true;
            }
            return false;
        }

        /// <summary>Carga una foto (reescalada a MaxSize) y la deja en caché.</summary>
        public static IEnumerator LoadPhoto(string archivo, Action<Texture> done)
        {
            if (TryGet(archivo, out var cached)) { done?.Invoke(cached); yield break; }
            using (var req = UnityWebRequestTexture.GetTexture(Url(archivo), true))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[RosasXR] No se pudo cargar {archivo}: {req.error}");
                    done?.Invoke(null);
                    yield break;
                }
                Texture tex = DownloadHandlerTexture.GetContent(req);
                tex = Downscale((Texture2D)tex);
                Put(archivo, tex);
                done?.Invoke(tex);
            }
        }

        static Texture Downscale(Texture2D src)
        {
            if (src == null) return null;
            int w = src.width, h = src.height, m = Mathf.Max(w, h);
            if (m <= MaxSize) return src;
            float k = (float)MaxSize / m;
            var rt = new RenderTexture(Mathf.RoundToInt(w * k), Mathf.RoundToInt(h * k), 0, RenderTextureFormat.ARGB32);
            rt.Create();
            Graphics.Blit(src, rt);
            UnityEngine.Object.Destroy(src);
            return rt;
        }

        static void Put(string key, Texture tex)
        {
            _cache[key] = tex;
            _lru.Remove(key); _lru.AddFirst(key);
            while (_lru.Count > CacheLimit)
            {
                var old = _lru.Last.Value;
                _lru.RemoveLast();
                if (_cache.TryGetValue(old, out var t) && t != null)
                {
                    if (t is RenderTexture r) r.Release();
                    UnityEngine.Object.Destroy(t);
                }
                _cache.Remove(old);
            }
        }
    }
}
