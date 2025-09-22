using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LayerManagerTest : MonoBehaviour
{
    [SerializeField] private GameObject layer01;
    async void Start()
    {
        await UniTask.WaitForSeconds(2);
        await LayerManager.Instance.ShowLayer01(LayerGroupType.Root);
        await UniTask.NextFrame();
        Debug.LogError("ShowLayer01");
        await UniTask.WaitForSeconds(2);
        await LayerManager.Instance.ShowLayer02(LayerGroupType.Popup);
        Debug.LogError("ShowLayer02");
        await LayerManager.Instance.ShowLayer02(LayerGroupType.Popup);
        Debug.LogError("ShowLayer02");
    }
}
