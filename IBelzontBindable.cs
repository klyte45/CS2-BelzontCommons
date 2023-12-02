using System;

namespace Belzont.Interfaces
{
    public interface IBelzontBindable
    {
        public void SetupCaller(Action<string, object[]> eventCaller);
        public void SetupEventBinder(Action<string, Delegate> eventCaller);
        public void SetupCallBinder(Action<string, Delegate> eventCaller);
    }
}
