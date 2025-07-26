using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Screen;

public class CustomCanvasScaler : CanvasScaler
{
    protected Vector2 _currentScreenSize;
    protected float   _screenHeight => height;
    protected float   _screenWidth  => width;
    protected Vector2 _screenSize   => new Vector2(_screenWidth, _screenHeight);

    protected override void Start()
    {
        base.Start();
        SetMatchWidthOrHeight();
    }

    protected void Update()
    {
        if (_currentScreenSize != _screenSize)
        {
            SetMatchWidthOrHeight();
        }
    }

    private void SetMatchWidthOrHeight()
    {
        _currentScreenSize = _screenSize;
        if (Mathf.Approximately(_screenWidth,referenceResolution.x))
            matchWidthOrHeight  = 0.5f;
        else matchWidthOrHeight = (float)width / height > 0.45f ? 1 : 0;
    }
}
