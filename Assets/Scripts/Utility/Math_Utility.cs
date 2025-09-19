using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Math_Utility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FastAbs(float x) => x < 0 ? -x : x;
}
