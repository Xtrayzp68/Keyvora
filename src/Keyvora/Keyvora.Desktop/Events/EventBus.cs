namespace Keyvora.Desktop.Events;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public sealed class EventBus : IEventBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public void Publish<T>(T eventData) where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_subscribers.TryGetValue(typeof(T), out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Action<T> action)
                    {
                        try { action(eventData); }
                        catch { /* isolate subscriber failures */ }
                    }
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : class
    {
        _lock.EnterWriteLock();
        try
        {
            var list = _subscribers.GetOrAdd(typeof(T), _ => new List<Delegate>());
            list.Add(handler);
            return new Subscription<T>(this, handler);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void Unsubscribe<T>(Action<T> handler) where T : class
    {
        _lock.EnterWriteLock();
        try
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) _subscribers.TryRemove(typeof(T), out _);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        _subscribers.Clear();
    }

    private sealed class Subscription<T> : IDisposable where T : class
    {
        private readonly EventBus _bus;
        private readonly Action<T> _handler;
        private int _disposed;

        public Subscription(EventBus bus, Action<T> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _bus.Unsubscribe(_handler);
        }
    }
}
