using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Usa cualquier textura como "cámara": una foto de prueba en el editor,
    /// o la textura de cámara que entregue el SDK de Meta (asignar por código: feed.texture = ...).
    /// </summary>
    public class TextureFeed : CameraFeed
    {
        public Texture texture;

        public override Texture Current => texture;
        public override bool IsReady => texture != null;
    }
}
