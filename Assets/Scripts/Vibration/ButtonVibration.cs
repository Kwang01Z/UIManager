using UnityEngine;
using UnityEngine.UI;

public class ButtonVibration : MonoBehaviour
{
    public Button Button;
    public bool OverridePresetVibration = false;
    public int VibrationStrength = 20;

    private void OnValidate()
    {
        if (!Button) Button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        Button?.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        Button?.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (VibrationManager.Instance != null)
        {
            if (OverridePresetVibration)
            {
                VibrationManager.Instance.ExecuteVibrationSingle(VibrationStrength);
            }
            else
            {
                VibrationManager.Instance.ExecuteButtonVibration();
            }
        }
    }
}
