using LitMotion;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderValidator : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public Vector2 Range = Vector2.up;
    [Range(0f, 1f)] public float Progress;
    private void OnValidate()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        UpdateProgress();
    }

    public float Value
    {
        get => (Progress - Range.x) / (Range.y - Range.x);
        set => Progress = Mathf.Clamp(value * (Range.y - Range.x) + Range.x, Range.x, Range.y);
    }

    public void SetValueSmooth(float value, float duration)
    {
        var targetValue = Mathf.Clamp(value * (Range.y - Range.x) + Range.x, Range.x, Range.y);
        LMotion.Create(Progress, targetValue, duration).WithEase(Ease.InQuad).Bind((x) =>
        {
            Progress = x;
            UpdateProgress();
        });
    }

    private void UpdateProgress()
    {
        if (slider)
        {
            slider.value = Mathf.Clamp(Progress, Range.x, Range.y);
        }
    }
}
