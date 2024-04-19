using Game.SceneFlow;
using System;

namespace Belzont.Utils
{
    public class MultiUIValueBinding<T, U> where T : IEquatable<T>
    {
        public T Value
        {
            get => m_value; set
            {
                if (!m_value.Equals(value))
                {
                    m_value = value;
                    UpdateUIs(value);
                }
            }
        }
        private T m_value;
        private readonly Action<string, object[]> eventCaller;
        private readonly string m_propertyPrefix;
        private readonly Func<T, T> m_dataNormalizeFn;
        private readonly Func<T, U> m_frontendTransformFn;

        public MultiUIValueBinding(T initialValue, string propertyPrefix, Action<string, object[]> euisEventCaller, Action<string, Delegate> callBinder, Func<T, U> frontendTransformFn, Func<T, T> dataNormalizeFn = null)
        {
            m_value = initialValue;
            m_propertyPrefix = propertyPrefix;
            callBinder($"{propertyPrefix}!", OnUiValueChanged);
            callBinder($"{propertyPrefix}?", GetValueUI);
            eventCaller = euisEventCaller;
            m_dataNormalizeFn = dataNormalizeFn;
            m_frontendTransformFn = frontendTransformFn;
        }

        private object GetValueUI() => m_frontendTransformFn is null ? Value : m_frontendTransformFn(Value);

        private void OnUiValueChanged(T newValue)
        {
            m_value = m_dataNormalizeFn is null ? newValue : m_dataNormalizeFn.Invoke(newValue);
            UpdateUIs(m_value);
        }

        private void UpdateUIs(T newValue)
        {
            var valueToSend = GetValueUI();
            eventCaller($"{m_propertyPrefix}->", new object[] { valueToSend });
            GameManager.instance.userInterface.view.View.TriggerEvent($"{m_propertyPrefix}->", valueToSend);
        }
    }


    public class MultiUIValueBinding<T> : MultiUIValueBinding<T, T> where T : IEquatable<T>
    {
        public MultiUIValueBinding(T initialValue, string propertyPrefix, Action<string, object[]> euisEventCaller, Action<string, Delegate> callBinder, Func<T, T> dataNormalizeFn = null)
            : base(initialValue, propertyPrefix, euisEventCaller, callBinder, null, dataNormalizeFn)
        {
        }
    }

}