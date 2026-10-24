using Meta.XR;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Cámara passthrough del Quest a través del componente oficial de Meta (PassthroughCameraAccess),
    /// el mismo que usan los samples. Es la fuente que se usa en el visor.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class MetaPassthroughFeed : CameraFeed
    {
        public PassthroughCameraAccess cameraAccess;
        [Tooltip("La cámara passthrough solo existe en el visor: en el editor se apaga para no llenar la consola de errores.")]
        public bool disableInEditor = true;

        void Awake()
        {
            if (disableInEditor && Application.isEditor && cameraAccess != null)
                cameraAccess.gameObject.SetActive(false);
        }

        public override bool IsReady => cameraAccess != null && cameraAccess.IsPlaying;
        public override Texture Current => IsReady ? cameraAccess.GetTexture() : null;
    }
}
