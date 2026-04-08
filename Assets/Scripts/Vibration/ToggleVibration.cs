using UnityEngine;
using UnityEngine.UI;

public class ToggleVibration : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Reset()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
    }

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(bool isOn)
    {
        if (isOn) VibrationManager.Instance.ExecuteButtonVibration();
    }
}
