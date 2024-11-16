using Belzont.Interfaces;
using System;
using Unity.Entities;

namespace Belzont.Utils
{
    public abstract class DataBaseController : ComponentSystemBase, IBelzontBindable
    {
        private Action<string, object[]> EventCaller { get; set; }
        private Action<string, Delegate> CallBinder { get; set; }

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            CallBinder = callBinder;
            if (EventCaller != null) InitValueBindings();
        }
        public virtual void SetupEventBinder(Action<string, Delegate> eventBinder)
        {
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            EventCaller = eventCaller;
            if (CallBinder != null) InitValueBindings();
        }

        private void InitValueBindings()
        {
            DoInitValueBindings(EventCaller, CallBinder);
            CallBinder = null;
            EventCaller = null;
        }

        protected abstract void DoInitValueBindings(Action<string, object[]> eventCaller, Action<string, Delegate> callBinder);
        public override void Update() { }
        protected override void OnCreate()
        {
            base.OnCreate();
        }
    }
}