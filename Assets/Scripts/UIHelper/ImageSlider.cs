using LitMotion;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ImageSlider : MonoBehaviour
{
    [SerializeField] private Image filterImg;
    private float _maxValue;
    private float _value;
    [Range(0f, 1f)] public float _progress;

    private void OnValidate()
    {
        if (filterImg) filterImg.fillAmount = _progress;
    }

    public void SetMaxValue(float maxValue)
    {
        _maxValue = maxValue;
    }

    public void SetValue(float value, float maxValue = 0)
    {
        _value = value;
        if (maxValue != 0) SetMaxValue(maxValue);
        UpdateProgress();
    }

    private MotionHandle _motionHandle;

    public void SetValueSmooth(float value, float duration)
    {
        // Hủy tween cũ và tạo mới
        _motionHandle.TryCancel();
        _motionHandle = LMotion.Create(_value, value, duration)
            .Bind(x =>
            {
                _value = x;
                UpdateProgress();
            });
    }

    public void SetProgress(float progress)
    {
        _value = (int)(progress * _maxValue);
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (_maxValue == 0) return;
        _progress = Mathf.Clamp01(_value / _maxValue);
        filterImg.fillAmount = _progress;
    }
}
