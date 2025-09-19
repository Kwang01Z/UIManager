using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestElement2 : MonoBehaviour, IDataLoader
{

    public async UniTaskVoid SetupData(object data)
    {
        await UniTask.Yield();
    }
}
