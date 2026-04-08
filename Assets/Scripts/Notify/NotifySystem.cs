using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class NotifySystem
{
    private bool _isActive;
    public bool IsActive => _isActive;
    public NotifyKeyName KeyName { get; private set; }
    public List<Action<bool>> Callbacks { get; private set; }

    public NotifySystem(NotifyKeyName keyName)
    {
        KeyName = keyName;
        Callbacks = new List<Action<bool>>();
    }
    public void AddCallback(Action<bool> callback)
    {
        if (callback == null) return;
        if (!Callbacks.Contains(callback))
        {
            Callbacks.Add(callback);
        }
    }

    public void RemoveCallback(Action<bool> callback)
    {
        if (callback == null) return;
        if (Callbacks.Contains(callback))
        {
            Callbacks.Remove(callback);
        }
    }

    public async UniTask InvokeAsync(bool value)
    {
        await UniTask.NextFrame();
        _isActive = value;
        foreach (var callback in Callbacks)
        {
            callback?.Invoke(value);
        }
    }
}