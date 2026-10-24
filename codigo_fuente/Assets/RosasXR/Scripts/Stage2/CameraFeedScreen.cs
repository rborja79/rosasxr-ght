using UnityEngine;
using UnityEngine.UI;

namespace RosasXR
{
    /// <summary>
    /// Pantalla pequeña en el carro con lo que ve la cámara del brazo (render texture de baja resolución,
    /// ~15 imágenes por segundo para cuidar el rendimiento del visor). Se puede ocultar desde el menú.
    /// </summary>
    public class CameraFeedScreen : MonoBehaviour
    {
        public Camera feedCamera;
        public RawImage screen;
        public GameObject screenRoot;
        public Vector2Int resolution = new Vector2Int(320, 240);
        public float fps = 15f;
        public bool Visible { get; private set; } = true;

        RenderTexture _rt;
        float _next;

        void Awake()
        {
            _rt = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32) { name = "VistaBrazo" };
            _rt.Create();
            feedCamera.targetTexture = _rt;
            feedCamera.enabled = false; // se dibuja a mano, a pocas imágenes por segundo
            if (screen != null) screen.texture = _rt;
        }

        public void Toggle() => SetVisible(!Visible);

        public void SetVisible(bool v)
        {
            Visible = v;
            if (screenRoot != null) screenRoot.SetActive(v);
        }

        void LateUpdate()
        {
            if (!Visible || Time.time < _next) return;
            _next = Time.time + 1f / Mathf.Max(1f, fps);
            feedCamera.Render();
        }

        void OnDestroy()
        {
            if (_rt != null) _rt.Release();
        }
    }
}
