using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

public static class Debug
{
    // --- Log ---
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Log(object message) => UnityEngine.Debug.Log(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Log(object message, Object context) => UnityEngine.Debug.Log(message, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogFormat(string format, params object[] args) => UnityEngine.Debug.LogFormat(format, args);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogFormat(Object context, string format, params object[] args) => UnityEngine.Debug.LogFormat(context, format, args);

    // --- Warning ---
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogWarning(object message) => UnityEngine.Debug.LogWarning(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogWarning(object message, Object context) => UnityEngine.Debug.LogWarning(message, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogWarningFormat(string format, params object[] args) => UnityEngine.Debug.LogWarningFormat(format, args);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogWarningFormat(Object context, string format, params object[] args) => UnityEngine.Debug.LogWarningFormat(context, format, args);

    // --- Error ---
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogError(object message) => UnityEngine.Debug.LogError(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogError(object message, Object context) => UnityEngine.Debug.LogError(message, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogErrorFormat(string format, params object[] args) => UnityEngine.Debug.LogErrorFormat(format, args);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogErrorFormat(Object context, string format, params object[] args) => UnityEngine.Debug.LogErrorFormat(context, format, args);

    // --- Exception ---
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void LogException(Exception exception, Object context) => UnityEngine.Debug.LogException(exception, context);

    // --- Assert ---
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Assert(bool condition) => UnityEngine.Debug.Assert(condition);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Assert(bool condition, object message) => UnityEngine.Debug.Assert(condition, message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Assert(bool condition, string message) => UnityEngine.Debug.Assert(condition, message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Assert(bool condition, object message, Object context) => UnityEngine.Debug.Assert(condition, message, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Assert(bool condition, string message, Object context) => UnityEngine.Debug.Assert(condition, message, context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void AssertFormat(bool condition, string format, params object[] args) => UnityEngine.Debug.AssertFormat(condition, format, args);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void AssertFormat(bool condition, Object context, string format, params object[] args) => UnityEngine.Debug.AssertFormat(condition, context, format, args);

    // --- Misc ---
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void Break() => UnityEngine.Debug.Break();

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void ClearDeveloperConsole() => UnityEngine.Debug.ClearDeveloperConsole();
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawLine(Vector3 start, Vector3 end) => UnityEngine.Debug.DrawLine(start, end);
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color) => UnityEngine.Debug.DrawLine(start, end, color);
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) => UnityEngine.Debug.DrawLine(start, end, color, duration);
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest) => UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawRay(Vector3 start, Vector3 dir) => UnityEngine.Debug.DrawRay(start, dir);
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawRay(Vector3 start, Vector3 dir, Color color) => UnityEngine.Debug.DrawRay(start, dir, color);
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) => UnityEngine.Debug.DrawRay(start, dir, color, duration);
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("ENABLE_LOG")]
    public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration, bool depthTest) => UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);

    // --- Properties ---
    public static bool developerConsoleVisible
    {
        get => UnityEngine.Debug.developerConsoleVisible;
        set => UnityEngine.Debug.developerConsoleVisible = value;
    }

    public static bool isDebugBuild => UnityEngine.Debug.isDebugBuild;
}
