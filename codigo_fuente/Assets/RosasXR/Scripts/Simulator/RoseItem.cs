using System;
using UnityEngine;

namespace RosasXR
{
    /// <summary>Una rosa evaluable en el simulador (tarjeta con foto o rosa 3D).</summary>
    public abstract class RoseItem : MonoBehaviour
    {
        public RoseSample Sample { get; protected set; }
        public bool Evaluated { get; protected set; }
        public event Action<RoseItem> Selected;

        public abstract void Setup(RoseSample sample);
        public abstract void MarkResult(bool correct);

        protected void RaiseSelected()
        {
            if (!Evaluated) Selected?.Invoke(this);
        }
    }
}
