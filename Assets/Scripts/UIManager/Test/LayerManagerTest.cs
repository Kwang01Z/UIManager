using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LayerManagerTest : MonoBehaviour
{
    [SerializeField] private GameObject layer01;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            LayerManager.Instance.ShowLayer01(LayerGroupType.Root);
            UnityEngine.Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            LayerManager.Instance.ShowLayer02(LayerGroupType.Popup);
            UnityEngine.Debug.Break();
        }
    }
}
