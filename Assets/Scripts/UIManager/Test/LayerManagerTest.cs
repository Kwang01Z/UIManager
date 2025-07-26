using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LayerManagerTest : MonoBehaviour
{
    
    void Start()
    {
        LayerManager.Instance.ShowLayer01().Forget();
    }

    
}
