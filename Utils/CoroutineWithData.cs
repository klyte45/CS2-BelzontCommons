using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Belzont.Utils
{
    public class CoroutineWithData<T>
    {
        public Coroutine Coroutine { get; private set; }
        public T result;
        public IEnumerator<T> m_target;
        public bool IsComplete { get; private set; }
        public Action<T> OnYield;
        public Action<T> OnComplete;

        public CoroutineWithData(MonoBehaviour owner, IEnumerator<T> target, Action<T> onComplete = null, Action<T> onYield = null)
        {
            m_target = target;
            Coroutine = owner.StartCoroutine(Run());
            OnComplete = onComplete;
            OnYield = onYield;
        }
        private IEnumerator Run()
        {
            while (m_target.MoveNext())
            {
                result = m_target.Current;
                OnYield?.Invoke(result);
                yield return result;
            }
            IsComplete = true;
            OnComplete?.Invoke(result);
        }
    }
    public static class CoroutineWithData
    {
        public static CoroutineWithData<U> From<U>(MonoBehaviour owner, IEnumerator<U> target, Action<U> onComplete = null, Action<U> onYield = null) => new(owner, target, onComplete, onYield);
    }
}
