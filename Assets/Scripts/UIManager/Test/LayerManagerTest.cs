using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LayerManagerTest : MonoBehaviour
{
    [SerializeField] private GameObject layer01;
    async void Start()
    {
        await UniTask.WaitForSeconds(5);
        await LayerManager.Instance.ShowLayer01(1);
        await UniTask.WaitForSeconds(5);
        Debug.LogError("ShowLayer01");
        await UniTask.DelayFrame(5);
        var insLayer02 = await Addressables.InstantiateAsync("Layers/LayerTest02", LayerManager.Instance.transform);
        insLayer02.SetActive(true);
        await UniTask.WaitForSeconds(5);
        Debug.LogError("ShowLayer01");
        await UniTask.DelayFrame(5);
        var insLayer01 = Instantiate(layer01, LayerManager.Instance.transform);
        insLayer01.SetActive(true);
        
    }

    
}
