using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleEffector : MonoBehaviour
{
    public Toggle Toggle;
    [Tooltip("State 0: Off, State1: On")]
    public List<AnimationStateBase> AnimationStates;

    public UnityEvent OnToggleOn;
    public UnityEvent OnToggleOff;

    public Toggle.ToggleEvent OnValueChanged;

    private void Reset()
    {
        AnimationStates = GetComponentsInChildren<AnimationStateBase>().ToList();
    }

    private void OnValidate()
    {
        if (Toggle == null) Toggle = GetComponent<Toggle>();
    }

    private void Awake()
    {
        Toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    public void OnToggleValueChanged(bool isOn)
    {
        SetStateSmooth(isOn);
        OnValueChanged?.Invoke(isOn);
    }

    public void SetStateSmooth(bool isOn)
    {
        Toggle.interactable = !isOn;
        foreach (var animationState in AnimationStates)
        {
            animationState.SetupStateSmooth(isOn ? 1 : 0);
        }

        if (isOn)
        {
            OnToggleOn?.Invoke();
        }
        else
        {
            OnToggleOff?.Invoke();
        }
    }

    public void SetState(bool isOn)
    {
        Toggle.interactable = !isOn;
        foreach (var animationState in AnimationStates)
        {
            animationState.SetupState(isOn ? 1 : 0);
        }
        if (isOn)
        {
            OnToggleOn?.Invoke();
        }
        else
        {
            OnToggleOff?.Invoke();
        }
    }

    public void SetValue(bool isOn, bool includeAnim, bool activeEvent)
    {
        if (includeAnim)
        {
            SetStateSmooth(isOn);
        }
        else
        {
            SetState(isOn);
        }
        Toggle.SetIsOnWithoutNotify(isOn);
        if (activeEvent) OnValueChanged?.Invoke(isOn);
    }

    public void SetIsOnWithoutNotify(bool isOn)
    {
        SetValue(isOn, false, false);
    }
}
