using UnityEngine;
using UnityEngine.UI;

public abstract class ButtonBase : MonoBehaviour
{
    [SerializeField] protected Button button;
    protected virtual void Reset()
    {
        if (!button) button = GetComponent<Button>();
    }
    protected virtual void Awake()
    {
        button.onClick.AddListener(OnButtonClicked);
    }
    protected abstract void OnButtonClicked();
}
