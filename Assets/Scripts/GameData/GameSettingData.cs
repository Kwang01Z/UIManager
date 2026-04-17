using UnityEngine;

public static class GameSettingData
{
    public static bool IsPlayMusic { get; private set; } = true;
    public static bool IsPlaySound { get; private set; } = true;
    public static bool IsPlayVibration { get; private set; } = true;
    public static readonly string IsPlayMusicKey = "IsPlayMusic";
    public static readonly string IsPlaySoundKey = "IsPlaySound";
    public static readonly string IsPlayVibrationKey = "IsPlayVibration";

    public static void InitData()
    {
        IsPlayVibration = PlayerPrefs.GetInt(IsPlayVibrationKey, 1) == 1;
        IsPlayMusic = PlayerPrefs.GetInt(IsPlayMusicKey, 1) == 1;
        IsPlaySound = PlayerPrefs.GetInt(IsPlaySoundKey, 1) == 1;
    }

    public static void SetPlayMusic(bool isPlay)
    {
        IsPlayMusic = isPlay;
        PlayerPrefs.SetInt(IsPlayMusicKey, isPlay ? 1 : 0);
    }
    public static void SetPlaySound(bool isPlay)
    {
        IsPlaySound = isPlay;
        PlayerPrefs.SetInt(IsPlaySoundKey, isPlay ? 1 : 0);
    }
    public static void SetVibration(bool isPlay)
    {
        IsPlayVibration = isPlay;
        PlayerPrefs.SetInt(IsPlayVibrationKey, isPlay ? 1 : 0);
    }
}