using TMPro;
using UnityEngine;

public class DotUpdate : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text loadingDot;
    [SerializeField] private float duration = 1f;

    private int _dotCount;
    private float _timeCount;
    private void Update()
    {
        if (canvasGroup && canvasGroup.alpha == 0 || !loadingDot) return;
        _timeCount += Time.deltaTime;
        if (_timeCount < duration) return;
        _timeCount = 0;
        if (_dotCount > 3) _dotCount = 0;
        loadingDot.text = new string('.', _dotCount);
        _dotCount++;
    }
}
