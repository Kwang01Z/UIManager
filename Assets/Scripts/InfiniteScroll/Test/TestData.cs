using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestData : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private IFS_Data _scrollData;
    [SerializeField] private IFS_Element _element;
    [SerializeField] private IFS_Element _element2;
    [SerializeField] private IFS_Element _element3;

    [SerializeField] private int numberOfElements = 1000;
    private void Start()
    {
        List<IFS_PlaceHolder> data = new List<IFS_PlaceHolder>();
        for (int i = 0; i < numberOfElements; i++)
        {
            if (i % 20 == 0)
            {
                data.Add(new IFS_PlaceHolder(_element3, i));
                data.Add(new IFS_PlaceHolder(_element2, i));
                data.Add(new IFS_PlaceHolder(_element2, i));
                data.Add(new IFS_PlaceHolder(_element2, i));
                data.Add(new IFS_PlaceHolder(_element2, i));
            }
            data.Add(new IFS_PlaceHolder(_element, i));
        }

        _scrollData.AddDataRange(data);
    }
}