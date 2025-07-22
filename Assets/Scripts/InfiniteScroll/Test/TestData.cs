using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestData : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private InfiniteScrollData _scrollData;
    [SerializeField] private InfiniteScrollElement _element;
    [SerializeField] private InfiniteScrollElement _element2;

    [SerializeField] private int numberOfElements = 1000;
    private void Start()
    {
        List<InfiniteScrollPlaceHolder> data = new List<InfiniteScrollPlaceHolder>();
        for (int i = 0; i < numberOfElements; i++)
        {
            if (i % 20 == 0)
            {
                data.Add(new InfiniteScrollPlaceHolder(_element2, i));
                data.Add(new InfiniteScrollPlaceHolder(_element2, i));
                data.Add(new InfiniteScrollPlaceHolder(_element2, i));
                data.Add(new InfiniteScrollPlaceHolder(_element2, i));
            }
            data.Add(new InfiniteScrollPlaceHolder(_element, i));
        }

        _scrollData.AddDataRange(data);
    }
}