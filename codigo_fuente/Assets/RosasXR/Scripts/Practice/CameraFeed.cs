using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Fuente de imagen para el clasificador. Implementaciones:
    ///  - WebCamFeed: cámara del Quest (passthrough) o webcam del Mac en el editor.
    ///  - TextureFeed: cualquier textura (una foto de prueba, o la textura de cámara del SDK de Meta).
    /// </summary>
    public abstract class CameraFeed : MonoBehaviour
    {
        public abstract Texture Current { get; }
        public abstract bool IsReady { get; }
    }
}
