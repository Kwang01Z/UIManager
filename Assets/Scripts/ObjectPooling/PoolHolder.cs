using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class PoolHolder : MonoSingleton<PoolHolder>
{
    Dictionary<string, Queue<MonoBehaviour>> _pools = new();
    Dictionary<string, int> _capacity = new();
    HashSet<string> _monoKeys = new(16);

    [CanBeNull]
    public MonoBehaviour Get(MonoBehaviour t, Transform parent = null, Vector3 position = default,
        Quaternion rotation = default, string customKey = "")
    {
        lock (_pools)
        {
            var key = string.IsNullOrEmpty(customKey) ? GetKey(t) : customKey;
            if (_monoKeys.Add(key))
            {
                _pools.Add(key, new Queue<MonoBehaviour>(16));
                _capacity.Add(key, 0);
            }

            var size = _capacity.GetValueOrDefault(key);
            if (!_pools.TryGetValue(key, out var monoQueue)) return null;
            if (size > 0 && monoQueue.Count >= size)
            {
                Debug.Log("Reached capacity of " + key);
                return null;
            }

            MonoBehaviour result = null;

            result = monoQueue.Count <= 0 ? Instantiate(t) : monoQueue.Dequeue();

            result.name = key;
            var resultTransform = result.transform;
            resultTransform.SetParent(parent, false);
            resultTransform.position = position;
            resultTransform.rotation = rotation;
            result.gameObject.SetActive(true);
            return result;
        }
    }

    public void Release(MonoBehaviour t, string customKey = "")
    {
        lock (_pools)
        {
            var key = string.IsNullOrEmpty(customKey) ? t.name : customKey;
            if (_monoKeys.Add(key))
            {
                _pools.Add(key, new Queue<MonoBehaviour>(16));
            }

            if (!_pools.TryGetValue(key, out var queue)) return;
            var size = _capacity.GetValueOrDefault(key);
            if (size <= 0 || queue.Count < size)
            {
                queue.Enqueue(t);
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
        var key = string.IsNullOrEmpty(customKey) ? GetKey(t) : customKey;
        _capacity[key] = size;
    }

    public static string GetKey(MonoBehaviour t)
    {
        return t.name + "-(PoolElement_No." + t.gameObject.GetInstanceID() + ")";
    }

    private void OnDestroy()
    {
        lock (_pools)
        {
            _pools.Clear();
            _monoKeys.Clear();
        }
    }
}