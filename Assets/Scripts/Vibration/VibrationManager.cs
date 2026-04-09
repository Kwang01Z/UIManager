using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VibrationUtility;

public class VibrationManager : MonoSingleton<VibrationManager>
{
    #region PROPERTIES

    [Header("This is a singleton class that manages device vibration settings and execution.")]
    public float VibrationRate = 0.15f;

    [Header("BUTTON SETTING(s)")]
    public int ButtonVibrationStrength = 25;

    private Coroutine continuesVibrationCoroutine;
    private CancellationTokenSource _cts;

    #endregion

    #region MAIN

    private void Start()
    {
        VibrationUtil.Init();
    }

    public void ExecuteButtonVibration()
    {
        if (!GameSettingData.IsPlayVibration) return;
        ExecuteVibrationSingle(ButtonVibrationStrength);
    }

    public void ExecuteVibrationSingle(VibrationUtil.VibrationType vibrateType)
    {
        if (!GameSettingData.IsPlayVibration) return;
        VibrationUtil.Vibrate(vibrateType);
    }

    public void ExecuteVibrationSingle(int vibrateStrength)
    {
        if (!GameSettingData.IsPlayVibration) return;
        VibrationUtil.VibrateFor(vibrateStrength);
    }

    public void CustomVibrating(float duration, float amplitude)
    {
        if (!GameSettingData.IsPlayVibration) return;
        VibrationUtil.VibrateFor((long)(duration), (int)(amplitude));
    }

    #region pattern

    public void ExecuteAdvanceVibration(VibrationUtil.VibrationType vibrateType, VibrationUtil.VibrationStyle vibrateStyle, float vibrateDuration = 1f)
    {
        switch (vibrateStyle)
        {
            case VibrationUtil.VibrationStyle.Single:
                ExecuteVibrationSingle(vibrateType);
                break;

            case VibrationUtil.VibrationStyle.Continuous:
                _cts?.TryCancel();
                _cts = new CancellationTokenSource();
                ExecuteVibrationContinues(vibrateType, vibrateDuration, _cts.Token);
                break;

            case VibrationUtil.VibrationStyle.HeadAndTail:
                _cts?.TryCancel();
                _cts = new CancellationTokenSource();
                ExecuteVibrationHeadAndTail(vibrateType, vibrateDuration, _cts.Token);
                break;
        }
    }

    public void ExecuteAdvanceVibration(int vibrateStrength, VibrationUtil.VibrationStyle vibrateStyle, float vibrateDuration = 1f)
    {
        switch (vibrateStyle)
        {
            case VibrationUtil.VibrationStyle.Single:
                ExecuteVibrationSingle(vibrateStrength);
                break;

            case VibrationUtil.VibrationStyle.Continuous:
                _cts?.TryCancel();
                _cts = new CancellationTokenSource();
                ExecuteVibrationContinues(vibrateStrength, vibrateDuration, _cts.Token);
                break;

            case VibrationUtil.VibrationStyle.HeadAndTail:
                _cts?.TryCancel();
                _cts = new CancellationTokenSource();
                ExecuteVibrationHeadAndTail(vibrateStrength, vibrateDuration, _cts.Token);
                break;
        }
    }

    private async void ExecuteVibrationContinues(VibrationUtil.VibrationType vibrateType, float duration, CancellationToken token)
    {
        float timer = duration;
        try
        {
            while (timer > 0)
            {
                ExecuteVibrationSingle(vibrateType);
                timer -= VibrationRate;
                await UniTask.WaitForSeconds(VibrationRate, cancellationToken: token);
            }
        }
        catch { }
    }

    private async void ExecuteVibrationContinues(int vibrateStrength, float duration, CancellationToken token)
    {
        float timer = duration;
        try
        {
            while (timer > 0)
            {
                ExecuteVibrationSingle(vibrateStrength);
                timer -= VibrationRate;
                await UniTask.WaitForSeconds(VibrationRate, cancellationToken: token);
            }
        }
        catch { }
    }

    private async void ExecuteVibrationHeadAndTail(VibrationUtil.VibrationType vibrateType, float duration, CancellationToken token)
    {
        try
        {
            ExecuteVibrationSingle(vibrateType);
            await UniTask.WaitForSeconds(duration - 0.1f, cancellationToken: token);
            ExecuteVibrationSingle(vibrateType);
        }
        catch { }
    }

    private async void ExecuteVibrationHeadAndTail(int vibrateStrength, float duration, CancellationToken token)
    {
        try
        {
            ExecuteVibrationSingle(vibrateStrength);
            await UniTask.WaitForSeconds(duration - 0.1f, cancellationToken: token);
            ExecuteVibrationSingle(vibrateStrength);
        }
        catch { }
    }

    #endregion

    #endregion
}
