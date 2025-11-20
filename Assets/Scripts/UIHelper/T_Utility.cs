using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine.UI;

public static class T_Utility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ZeroReplace(this int value, int target)
    {
        return value > 0 ? value : target;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NullReplace(this string str, string replace = "null")
    {
        return string.IsNullOrEmpty(str)
            ? replace
            : str;
    }

    public static void TryCancel(this CancellationTokenSource cancellation)
    {
        if (cancellation == null) return;
        try
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static CultureInfo EN_Culture = new CultureInfo("en-US");

    #region Button

    public static void RegisterListener(this List<Button> buttons, Action onClick)
    {
        if (buttons == null || buttons.Count == 0) return;
        foreach (var button in buttons)
        {
            button?.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public static void RemoveListener(this List<Button> buttons, Action onClick)
    {
        if (buttons == null || buttons.Count == 0) return;
        foreach (var button in buttons)
        {
            button?.onClick.RemoveListener(() => onClick?.Invoke());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetActive(this Button button, bool active)
    {
        if (button == null) return;
        button.gameObject.SetActive(active);
    }

    #endregion
}
