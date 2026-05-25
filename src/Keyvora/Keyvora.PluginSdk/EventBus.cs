namespace Keyvora.PluginSdk;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public sealed class EventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        _handlers.AddOrUpdate(
            typeof(T),
            _ => new List<Delegate> { handler },
            (_, list) => { list.Add(handler); return list; });
    }

    public void Publish<T>(T @event)
    {
        if (_handlers.TryGetValue(typeof(T), out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler is Action<T> action)
                    action(@event);
            }
        }
    }
}
