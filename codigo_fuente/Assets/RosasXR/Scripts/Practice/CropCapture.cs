using System;
using System.IO;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Guarda el recorte de la mira como PNG, tomado con la cámara real del visor.
    /// Sirve para darle al equipo del modelo fotos del mismo dominio que verá en la demo
    /// (lente, color y resolución del Quest), y para revisar casos dudosos.
    ///
    /// Carpeta: Application.persistentDataPath/capturas
    /// (en el Quest: /sdcard/Android/data/[paquete]/files/capturas; se descargan con Meta Quest Developer Hub).
    /// </summary>
    public class CropCapture : MonoBehaviour
    {
        public CameraFeed feed;
        [Tooltip("Debe ser igual al cropFraction del clasificador para que la foto sea lo mismo que ve el modelo.")]
        [Range(0.1f, 1f)] public float cropFraction = 0.45f;
        [Tooltip("Lado de la imagen guardada. Más grande que la entrada del modelo para poder re-recortar después.")]
        public int outputSize = 512;

        public string LastSavedPath { get; private set; }
        public int Count { get; private set; }

        // Conectado al botón "Capturar foto".
        public void Capture()
        {
            if (feed == null || !feed.IsReady)
            {
                Debug.LogWarning("[RosasXR] La cámara aún no está lista para capturar.");
                return;
            }

            var src = feed.Current;
            float side = cropFraction * Mathf.Min(src.width, src.height);
            var scale = new Vector2(side / src.width, side / src.height);
            var offset = new Vector2((1f - scale.x) * 0.5f, (1f - scale.y) * 0.5f);

            var rt = RenderTexture.GetTemporary(outputSize, outputSize, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt, scale, offset);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(outputSize, outputSize, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, outputSize, outputSize), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var dir = Path.Combine(Application.persistentDataPath, "capturas");
            Directory.CreateDirectory(dir);
            var name = $"{OperatorSession.OperatorId}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            LastSavedPath = Path.Combine(dir, name);
            File.WriteAllBytes(LastSavedPath, tex.EncodeToPNG());
            Destroy(tex);

            Count++;
            Debug.Log($"[RosasXR] Captura guardada: {LastSavedPath}");
        }
    }
}
