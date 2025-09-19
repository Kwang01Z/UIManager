using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestElement : MonoBehaviour, IDataLoader
{
    public UniTaskVoid SetupData(object data)
    {
        return new UniTaskVoid();
    }
}
