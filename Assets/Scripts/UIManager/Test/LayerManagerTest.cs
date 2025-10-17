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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LayerManager.Instance.ShowLayer01(LayerGroupType.Root);
            //UnityEngine.Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LayerManager.Instance.ShowLayer02(LayerGroupType.FullScreen);
            //UnityEngine.Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            LayerManager.Instance.ShowLayer03(LayerGroupType.Popup);
            //UnityEngine.Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            LayerManager.Instance.ShowLayer04(LayerGroupType.FullScreen);
            //UnityEngine.Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            LayerManager.Instance.ShowLayer05(LayerGroupType.Popup);
            //UnityEngine.Debug.Break();
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            LayerManager.Instance.ShowLayer06(LayerGroupType.Root);
            //UnityEngine.Debug.Break();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LayerManager.Instance.CloseLastLayerGroup();
        }
    }
}
