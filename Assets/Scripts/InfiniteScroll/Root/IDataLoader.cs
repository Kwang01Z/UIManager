using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IDataLoader
{
    public UniTaskVoid SetupData(object data);
}
