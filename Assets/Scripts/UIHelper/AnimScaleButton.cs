using Alchemy.Inspector;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class AnimScaleButton : MonoBehaviour
{
    [SerializeField] private RectTransform targetRect;
    public float endScale = 1.1f;
    public float duration = 0.7f;
    public Ease ease = Ease.OutBack;

    private void Reset()
    {
        if (!targetRect) targetRect = GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        StopAnim();
    }

    protected MotionHandle _motionHandle;
    [Button]
    public void PlayAnim()
    {
        if (_motionHandle.IsActive())
        {
            _motionHandle.TryCancel();
        }

        _motionHandle = LMotion.Create(Vector3.one, Vector3.one * endScale, duration).WithEase(ease).WithLoops(-1, LoopType.Yoyo)
            .BindToLocalScale(targetRect);
    }

    [Button]
    public void StopAnim()
    {
        if (_motionHandle.IsActive())
        {
            _motionHandle.TryCancel();
        }
    }
}
