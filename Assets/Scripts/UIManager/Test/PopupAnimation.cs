using UnityEngine;
using UnityEngine.Events;
using LitMotion;
using LitMotion.Extensions;

public class PopupAnimation : MonoBehaviour
{
    [SerializeField] private LayerBase m_uiCoreLayer;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup dimCanvasGroup;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    public UnityEvent OnStartAnim;
    public UnityEvent OnCompleteAnim;

    private MotionHandle _scaleHandle;
    private MotionHandle _fadeHandle;
    private MotionHandle _delayHandle;

    private void Reset()
    {
        if (m_uiCoreLayer == null) m_uiCoreLayer = GetComponent<LayerBase>();
        if (panelRect == null) panelRect = transform.Find("Panel")?.GetComponent<RectTransform>();
        if (dimCanvasGroup == null) dimCanvasGroup = transform.GetChild(0).GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (m_uiCoreLayer != null)
            m_uiCoreLayer.OnShowLayer.Register(PlayAnim);

        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (m_uiCoreLayer != null)
            m_uiCoreLayer.OnShowLayer?.UnRegister(PlayAnim);

        // Huỷ mọi tween còn sống khi object bị destroy
        _scaleHandle.TryCancel();
        _fadeHandle.TryCancel();
        _delayHandle.TryCancel();
    }

    [ContextMenu("Play Animation")]
    public void PlayAnim()
    {
        // Huỷ tween cũ nếu đang chạy
        _scaleHandle.TryCancel();
        _fadeHandle.TryCancel();
        _delayHandle.TryCancel();

        // Giá trị khởi tạo
        if (panelRect) panelRect.localScale = Vector3.zero;
        if (dimCanvasGroup) dimCanvasGroup.alpha = 0f;

        OnStartAnim?.Invoke();

        // Scale Panel
        if (panelRect)
        {
            _scaleHandle = LMotion
                .Create(Vector3.zero, Vector3.one, duration)
                .WithEase(easeType)
                .BindToLocalScale(panelRect);             
        }

        // Fade dim
        if (dimCanvasGroup)
        {
            _fadeHandle = LMotion
                .Create(0f, 1f, duration)
                .WithEase(easeType)
                .BindToAlpha(dimCanvasGroup);
        }

        // “Delay” để bắn OnComplete một lần khi 2 tween trên kết thúc
        _delayHandle = LMotion
            .Create(0f, 1f, duration)
            .WithOnComplete(() => OnCompleteAnim?.Invoke())
            .Bind(_ => { });
    }
}
