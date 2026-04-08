using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class NotifyFactory : MonoSingleton<NotifyFactory>
{
    private Dictionary<NotifyKeyName, NotifySystem> _notifySystems = new();

    public void RegisterNotify(NotifyKeyName keyName, Action<bool> callback)
    {
        if (!_notifySystems.TryGetValue(keyName, out NotifySystem notifySystem))
        {
            // If the key does not exist, create a new NotifySystem
            notifySystem = new NotifySystem(keyName);
            _notifySystems[keyName] = notifySystem;
        }

        _notifySystems[keyName].AddCallback(callback);
    }

    public void UnregisterNotify(NotifyKeyName keyName, Action<bool> callback)
    {
        if (!_notifySystems.TryGetValue(keyName, out NotifySystem notifySystem))
        {
            return;
        }

        notifySystem.RemoveCallback(callback);
        if (notifySystem.Callbacks.Count == 0)
        {
            // If no callbacks left, remove the NotifySystem
            _notifySystems.Remove(keyName);
        }
    }

    public void InvokeNotify(NotifyKeyName keyName, bool value)
    {
        if (!_notifySystems.TryGetValue(keyName, out NotifySystem notifySystem))
        {
            return;
        }

        notifySystem.InvokeAsync(value).Forget();
    }
    public bool GetNotifyState(NotifyKeyName keyName)
    {
        if (!_notifySystems.TryGetValue(keyName, out NotifySystem notifySystem))
        {
            return false; // Default state if not registered
        }

        return notifySystem.IsActive;
    }
}
