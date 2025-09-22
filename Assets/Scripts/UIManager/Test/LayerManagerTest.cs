using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LayerManagerTest : MonoBehaviour
{
    [SerializeField] private GameObject layer01;
    void Start()
    {
        LayerManager.Instance.ShowLayer01(1).Forget();
        /*await ShowLayer();
        await ShowLayer();*/
    }

    private async UniTask ShowLayer()
    {
        Stopwatch stopwatch = new Stopwatch();
        await UniTask.WaitForSeconds(5);
        stopwatch.Start();
        await LayerManager.Instance.ShowLayer01(1);
        stopwatch.Stop();
        Debug.LogError("ShowLayer:" +stopwatch.ElapsedMilliseconds.ToString());
        await UniTask.WaitForSeconds(5);
        Debug.LogError("ShowLayer01");
        await UniTask.DelayFrame(5);
        
        stopwatch.Restart();
        var insLayer02 = await Addressables.InstantiateAsync("Layers/LayerTest02", LayerManager.Instance.transform);
        insLayer02.SetActive(true);
        stopwatch.Stop();
        Debug.LogError("Addressable: "+stopwatch.ElapsedMilliseconds.ToString());
        await UniTask.WaitForSeconds(5);
        Debug.LogError("ShowLayer01");
        await UniTask.DelayFrame(5);
        
        stopwatch.Restart();
        var insLayer01 = Instantiate(layer01, LayerManager.Instance.transform);
        insLayer01.SetActive(true);
        stopwatch.Stop();
        Debug.LogError("Instantiate: "+stopwatch.ElapsedMilliseconds.ToString());
    }

}
