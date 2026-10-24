using System.Collections.Generic;
using UnityEngine;

namespace RosasXR
{
    /// <summary>
    /// Usa la primera fuente de imagen que esté lista. Orden por defecto:
    /// cámara passthrough de Meta (en el visor) → webcam (en el editor del Mac).
    /// </summary>
    public class FirstReadyFeed : CameraFeed
    {
        public List<CameraFeed> feeds = new List<CameraFeed>();

        CameraFeed Active
        {
            get
            {
                foreach (var f in feeds)
                    if (f != null && f != this && f.isActiveAndEnabled && f.IsReady) return f;
                return null;
            }
        }

        public override bool IsReady => Active != null;
        public override Texture Current => Active != null ? Active.Current : null;
    }
}
