namespace Keyvora.Desktop.Actions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public sealed class ActionRegistry
{
    private readonly ConcurrentDictionary<string, IAction> _actions = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_actions.TryAdd(action.TypeId, action))
            throw new InvalidOperationException($"Action type '{action.TypeId}' is already registered.");
    }

    public void Unregister(string typeId)
    {
        _actions.TryRemove(typeId, out _);
    }

    public IAction? Get(string typeId)
    {
        _actions.TryGetValue(typeId, out var action);
        return action;
    }

    public IReadOnlyList<IAction> GetAll() => _actions.Values.ToList();

    public IAction Clone(string typeId)
    {
        var prototype = Get(typeId)
            ?? throw new KeyNotFoundException($"Action '{typeId}' not found.");

        if (Activator.CreateInstance(prototype.GetType()) is not IAction clone)
            throw new InvalidOperationException($"Cannot clone action '{typeId}'.");

        if (prototype.Config != null && clone.Config != null)
        {
            var json = prototype.Config.Serialize();
            clone.Config.Deserialize(json);
        }

        return clone;
    }
}
