using System;
using System.Collections.Generic;

namespace Entitas
{
    internal sealed class ObjectPool<T>
    {
        readonly Func<T> _factory;
        readonly Action<T> _reset;
        readonly Stack<T> _objects = new Stack<T>();

        public ObjectPool(Func<T> factory, Action<T> reset = null)
        {
            _factory = factory;
            _reset = reset;
        }

        public T Get() => _objects.Count == 0
            ? _factory()
            : _objects.Pop();

        public void Push(T obj)
        {
            _reset?.Invoke(obj);
            _objects.Push(obj);
        }
    }
}
