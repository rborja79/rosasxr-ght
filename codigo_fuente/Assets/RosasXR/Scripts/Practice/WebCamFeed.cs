using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace RosasXR
{
    /// <summary>
    /// Abre una cámara con WebCamTexture.
    ///  - En el Quest 3: la cámara passthrough (pide el permiso horizonos.permission.HEADSET_CAMERA).
    ///  - En el editor del Mac: la webcam, para probar el clasificador apuntando a rosas o fotos reales.
    /// </summary>
    public class WebCamFeed : CameraFeed
    {
        [Tooltip("Si hay varias cámaras, usar la que contenga este texto en el nombre (vacío = la primera).")]
        public string preferredDeviceNameContains = "";
        public int requestedWidth = 1280;
        public int requestedHeight = 960;
        public int requestedFps = 30;
        [Tooltip("Solo usar la webcam dentro del editor. En el visor se usa MetaPassthroughFeed.")]
        public bool onlyInEditor = true;

        const string HeadsetCameraPermission = "horizonos.permission.HEADSET_CAMERA";

        WebCamTexture _tex;

        public override Texture Current => _tex;
        public override bool IsReady => _tex != null && _tex.isPlaying && _tex.width > 16;

        IEnumerator Start()
        {
            if (onlyInEditor && !Application.isEditor) { enabled = false; yield break; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(HeadsetCameraPermission))
            {
                Permission.RequestUserPermission(HeadsetCameraPermission);
                float t = 0f;
                while (!Permission.HasUserAuthorizedPermission(HeadsetCameraPermission) && t < 20f)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (!Permission.HasUserAuthorizedPermission(HeadsetCameraPermission))
                {
                    Debug.LogError("[RosasXR] Sin permiso de cámara del visor. Revisa que el AndroidManifest declare " + HeadsetCameraPermission);
                    yield break;
                }
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("[RosasXR] No se encontró ninguna cámara.");
                yield break;
            }

            string name = devices[0].name;
            if (!string.IsNullOrEmpty(preferredDeviceNameContains))
                foreach (var d in devices)
                    if (d.name.Contains(preferredDeviceNameContains)) { name = d.name; break; }

            _tex = new WebCamTexture(name, requestedWidth, requestedHeight, requestedFps);
            _tex.Play();
            Debug.Log($"[RosasXR] Cámara abierta: {name}");
        }

        void OnDestroy()
        {
            if (_tex != null) _tex.Stop();
        }
    }
}
