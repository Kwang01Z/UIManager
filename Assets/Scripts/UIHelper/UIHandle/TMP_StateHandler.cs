using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TMP_StateHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private List<TMP_StateData> stateDataList;
    [SerializeField] private int defaultIndex = 0;

    private void Reset()
    {
        try
        {
            if (textMeshPro == null)
            {
                textMeshPro = GetComponent<TextMeshProUGUI>();
            }

            // Additional validation for textMeshPro component
            if (textMeshPro == null)
            {
                Debug.LogError("TMP_StateHandler: Could not find TextMeshProUGUI component on GameObject");
                return;
            }

            stateDataList?.Clear();
            stateDataList = new List<TMP_StateData>();

            // Safely create initial state data with null checks
            TMP_StateData stateData = new TMP_StateData
            {
                Index = 0,
                Color = textMeshPro.color,
                Material = textMeshPro.fontMaterial, // This could be null, which is fine
                FontSize = textMeshPro.fontSize,
                Enabled = textMeshPro.enabled
            };
            stateDataList.Add(stateData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TMP_StateHandler: Error in Reset(): {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }
#if UNITY_EDITOR
    [Button]
    public void SetTextState()
    {
        SetTextState(defaultIndex);
    }
#endif

    public void SetTextState(int index)
    {
        try
        {
            // Comprehensive null and bounds checking
            if (textMeshPro == null)
            {
                Debug.LogError("TMP_StateHandler: textMeshPro component is null");
                return;
            }

            if (stateDataList == null)
            {
                Debug.LogError("TMP_StateHandler: stateDataList is null");
                return;
            }

            if (index < 0 || index >= stateDataList.Count)
            {
                Debug.LogError($"TMP_StateHandler: Index {index} is out of range. stateDataList.Count: {stateDataList.Count}");
                return;
            }

            TMP_StateData stateData = stateDataList.Find(x => x.Index == index);
            if (stateData == null)
            {
                Debug.LogWarning($"TMP_StateHandler: No state data found for index {index}");
                return;
            }

            // Safely set color
            textMeshPro.color = stateData.Color;

            // Safely set material with additional validation
            if (stateData.Material != null)
            {
                // Validate that the material is not destroyed
                if (stateData.Material)
                {
                    textMeshPro.fontMaterial = stateData.Material;
                }
                else
                {
                    Debug.LogWarning($"TMP_StateHandler: Material for index {index} appears to be destroyed, skipping material assignment");
                }
            }

            // Safely set enabled state
            textMeshPro.enabled = stateData.Enabled;

            // Safely set font size
            if (stateData.FontSize > 0)
            {
                textMeshPro.fontSize = stateData.FontSize;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TMP_StateHandler: Error in SetTextState(index: {index}): {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }
}
[Serializable]
public class TMP_StateData
{
    public bool Enabled = true;
    public int Index;
    public Color Color;
    public Material Material;
    public float FontSize;
}
