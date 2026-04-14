using Belzont.Interfaces;
using System;
using Unity.Entities;

namespace Belzont.Systems
{
    /// <summary>
    /// Base class for systems that only expose UI bindings and have no per-frame update work.
    /// </summary>
    public abstract partial class BindableSystemBase : SystemBase, IBelzontBindable
    {
        public virtual void SetupCaller(Action<string, object[]> eventCaller) { }
        public virtual void SetupEventBinder(Action<string, Delegate> eventBinder) { }
        public abstract void SetupCallBinder(Action<string, Delegate> callBinder);

        protected sealed override void OnUpdate() { }
    }
}
