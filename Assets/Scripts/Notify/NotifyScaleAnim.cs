using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NotifyScaleAnim : MonoBehaviour
{
    [SerializeField] private float scaleValue = 1.2f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private int loopCount = -1;
    private List<MotionHandle> motionHandles = new List<MotionHandle>();
    public bool ActiveOnStart = false;

    public RectTransform RectTransform => transform as RectTransform;
    public UnityEvent OnStartAnim;
    public UnityEvent OnEndAnim;

    private void Reset()
    {
#if UNITY_EDITOR
        var image = GetComponent<Image>();
        if (image)
        {
            var so = new SerializedObject(this);

            // Add OnStartAnim -> image.enabled = true
            //UnityEventEditorUtility.AddBoolPersistentListener(so.FindProperty("OnStartAnim"), image, "set_enabled", true);

            // Add OnEndAnim -> image.enabled = false
            //UnityEventEditorUtility.AddBoolPersistentListener(so.FindProperty("OnEndAnim"), image, "set_enabled", false);

            so.ApplyModifiedProperties();
        }
#endif
    }

    private void OnEnable()
    {
        if (ActiveOnStart)
        {
            StartScaleAnim();
        }
    }

    public void StartScaleAnim()
    {
        StopScaleAnim();

        if (RectTransform != null)
        {
            var handle = ScaleUI(RectTransform);
            motionHandles.Add(handle);
        }
    }

    public MotionHandle ScaleUI(RectTransform rectTransform)
    {
        if (rectTransform == null) return default;

        var originalScale = rectTransform.localScale;
        var targetScale = originalScale * scaleValue;

        var handle = LMotion.Create(originalScale, targetScale, duration)
            .WithLoops(loopCount, LoopType.Yoyo)
            .WithOnCancel(() =>
            {
                if (rectTransform != null)
                {
                    rectTransform.localScale = originalScale; // Reset scale after animation
                }
            })
            .BindToLocalScale(rectTransform);

        return handle;
    }

    public void StopScaleAnim()
    {
        foreach (var handle in motionHandles)
        {
            if (handle.IsActive())
            {
                handle.Cancel();
            }
        }
        motionHandles.Clear();
    }

    private void OnDisable()
    {
        StopScaleAnim();
    }

    private void OnDestroy()
    {
        StopScaleAnim();
    }
}
