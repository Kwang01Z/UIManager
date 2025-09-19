using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class PoolHolder : MonoSingleton<PoolHolder>
{
    Dictionary<string, Queue<MonoBehaviour>> _pools = new ();
    Dictionary<string,int> _capacity = new();
    
    [CanBeNull]
    public MonoBehaviour Get(MonoBehaviour t, Transform parent = null, Vector3 position = default, Quaternion rotation = default, string customKey = "")
    {
        lock (_pools)
        {
            var key = string.IsNullOrEmpty(customKey) ? GetKey(t) : customKey;
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
                result.transform.SetParent(parent, false);
            }
            result.name = key;
            result.transform.position = position;
            result.transform.rotation = rotation;
            result.gameObject.SetActive(true);
            return result;
        }
    }

    public void Release(MonoBehaviour t, string customKey = "")
    {
        lock (_pools)
        {
            var key = string.IsNullOrEmpty(customKey)? t.name : customKey;
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
    }

    public void SetMaxSize(MonoBehaviour t, int size, string customKey = "")
    {
        var key = string.IsNullOrEmpty(customKey)? GetKey(t) : customKey;
        _capacity[key] = size;
    }

    public static string GetKey(MonoBehaviour t)
    {
        return t.name + "-(PoolElement_No." + t.gameObject.GetInstanceID() +")";
    }

    private void OnDestroy()
    {
        lock (_pools)
        {
            _pools.Clear();
        }
    }
}
