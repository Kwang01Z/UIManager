using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PoolHolder : MonoSingleton<PoolHolder>
{
    Dictionary<Type, Queue<MonoBehaviour>> _pools = new ();
    Dictionary<Type,int> _capacity = new();
    
    [CanBeNull]
    public MonoBehaviour Get(MonoBehaviour t, Transform parent = null, Vector3 position = default, Quaternion rotation = default)
    {
        var key = t.GetType();
        _pools.TryAdd(key, new Queue<MonoBehaviour>());
        _capacity.TryAdd(key, 0);
        
        var size = _capacity.GetValueOrDefault(key);
        if (size > 0 && _pools[key].Count >= size)
        {
#if UNITY_EDITOR
            Debug.Log("Reached capacity of " + key);
#endif
            return null;
        }
        
        MonoBehaviour result = null;
        
        if (_pools[key].Count <= 0)
        {
            result = Instantiate(t, parent);
        }
        else
        {
            result = _pools[key].Dequeue();
            result.transform.SetParent(parent);
        }
        result.transform.position = position;
        result.transform.rotation = rotation;
        result.gameObject.SetActive(true);
        return result;
    }

    public void Release(MonoBehaviour t)
    {
        var key = t.GetType();
        _pools.TryAdd(key, new Queue<MonoBehaviour>());

        var size = _capacity.GetValueOrDefault(key);
        if (size <= 0 || _pools[key].Count < size)
        {
            _pools[key].Enqueue(t);
            t.gameObject.SetActive(false);
        }
        else
        {
            Destroy(t);
        }
    }

    public void SetMaxSize(MonoBehaviour t, int size)
    {
        _capacity[t.GetType()] = size;
    }

    private void OnDestroy()
    {
        _pools.Clear();
    }
}
