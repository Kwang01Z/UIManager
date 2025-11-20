using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.Events;

public class AnimShowPopup : MonoBehaviour
{
    [SerializeField] protected LayerBase layerBase;
    [SerializeField] protected CanvasGroup dimCanvasGroup;
    [SerializeField] protected RectTransform panelRect;
    [SerializeField] protected float duration = 0.4f;
    [SerializeField] protected Ease easeType = Ease.OutBack;
    [SerializeField] protected float interval = 0f;
    public UnityEvent OnStartAnim;
    public UnityEvent OnCompleteAnim;

    private void Reset()
    {
        if (layerBase == null) layerBase = GetComponent<LayerBase>();
        if (panelRect == null) panelRect = transform.Find("Panel")?.GetComponent<RectTransform>();
        if (dimCanvasGroup == null) dimCanvasGroup = transform.GetChild(0).GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (layerBase) layerBase.OnShowLayer.AddListener(PlayAnim);
        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (layerBase) layerBase.OnShowLayer.RemoveListener(PlayAnim);
        if (_currentMotionHandle.IsActive())
        {
            _currentMotionHandle.TryCancel();
        }
    }

    protected MotionSequenceBuilder _sequenceBuilder;
    private MotionHandle _currentMotionHandle; // Thêm biến này để lưu handle

    private void PlayAnim()
    {
        // Cancel sequence cũ trước khi tạo mới
        if (_currentMotionHandle.IsActive())
        {
            _currentMotionHandle.TryCancel();
        }

        _sequenceBuilder = LSequence.Create();
        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0;
        OnStartAnim?.Invoke();

        if (panelRect) _sequenceBuilder.Join(LMotion.Create(panelRect.localScale, Vector3.one, duration).WithEase(easeType)
            .BindToLocalScale(panelRect));
        if (dimCanvasGroup)
            _sequenceBuilder.Join(LMotion.Create(dimCanvasGroup.alpha, 1, duration).WithEase(easeType)
                .BindToAlpha(dimCanvasGroup));
        _sequenceBuilder.Join(LMotion.Create(0, 1, duration + interval).WithOnComplete(() => { OnCompleteAnim?.Invoke(); })
            .RunWithoutBinding());

        // Lưu handle khi run
        _currentMotionHandle = _sequenceBuilder.Run();
    }

}
