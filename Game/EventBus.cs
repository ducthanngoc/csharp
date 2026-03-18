using System;
using System.Collections.Generic;
using System.Linq;
namespace Game.Core
{
    public class EventBus
    {
        private readonly Dictionary<Type, HashSet<Delegate>> _subscribers = new Dictionary<Type, HashSet<Delegate>>();

        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new HashSet<Delegate>();

            _subscribers[type].Add(handler);
        }
        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);

            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(handler);
        }

        public void Publish<T>(T eventData)
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type)) return;

            foreach (var handler in _subscribers[type].Cast<Action<T>>())
            {
                handler.Invoke(eventData);
            }
        }
    }
}
