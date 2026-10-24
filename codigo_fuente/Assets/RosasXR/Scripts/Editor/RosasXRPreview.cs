using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RosasXR.EditorTools
{
    /// <summary>Captura de prueba de la escena Cosecha desde los ojos del usuario (para revisar sin visor).</summary>
    public static class RosasXRPreview
    {
        [MenuItem("Rosas XR/8. Prueba automática (Cosecha)", priority = 8)]
        public static void SelfTest()
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "../selftest.flag"), "1");
            EditorSceneManager.OpenScene("Assets/RosasXR/Scenes/Cosecha.unity");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Rosas XR/9. Captura de prueba (Cosecha)", priority = 9)]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/RosasXR/Scenes/Cosecha.unity");
            var shots = new (string name, Vector3 pos, Vector3 euler)[]
            {
                ("vista_pasillo", new Vector3(0f, 1.6f, 0.2f), new Vector3(8f, 0f, 0f)),
                ("vista_rosas", new Vector3(0f, 1.6f, 2.5f), new Vector3(-5f, 60f, 0f)),
                ("vista_carro", new Vector3(0f, 1.65f, 0.0f), new Vector3(35f, 0f, 0f)),
                ("vista_cabecera", new Vector3(-0.5f, 1.7f, -1.0f), new Vector3(10f, 20f, 0f)),
            };
            var dir = Path.Combine(Application.dataPath, "../Capturas");
            Directory.CreateDirectory(dir);
            foreach (var s in shots)
            {
                var go = new GameObject("CamCaptura");
                var cam = go.AddComponent<Camera>();
                cam.transform.position = s.pos; cam.transform.eulerAngles = s.euler;
                cam.fieldOfView = 80f; cam.nearClipPlane = 0.03f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.9f, 0.92f, 0.92f);
                var rt = new RenderTexture(1280, 800, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 800, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, s.name + ".png"), tex.EncodeToJPG(85));
                RenderTexture.active = null;
                Object.DestroyImmediate(tex); Object.DestroyImmediate(go); rt.Release();
            }
            Debug.Log("[RosasXR] Capturas en " + dir);
        }
    }
}
