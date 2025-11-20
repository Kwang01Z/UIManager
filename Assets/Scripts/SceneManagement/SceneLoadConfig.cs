using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

/// <summary>
/// Cấu hình tải cho từng scene
/// </summary>
[Serializable]
public class SceneLoadConfig
{
    [Tooltip("Tên scene cần load")]
    public string SceneName;

    [Tooltip("Có hiển thị loading screen không")]
    public bool ShowLoadingScreen = false;

    [Tooltip("Có tải nền không (background loading)")]
    public bool LoadInBackGround = false;


    [Tooltip("Có thể hủy quá trình tải không")]
    public bool CanBeCancelled = false;
    
    [Tooltip("Có tắt chức năng unload scene hiện tại không")]
    public bool DontUnloadCurrentScene = false;
    
    [Tooltip("Có tắt chức năng tự động active new scene không")]
    public bool DontActiveNewScene = false;

    // Callbacks
    public Func<float, UniTask> OnLoadingProgress;
    public Func<string, UniTask> OnSceneLoaded;
    public Func<string, UniTask> OnSceneUnloaded;
    public Func<UniTask> OnLoadStarted;
    public Func<UniTask> OnLoadCompleted;
    public Func<UniTask> OnLoadCancelled;

    public SceneLoadConfig(string sceneName)
    {
        SceneName = sceneName;
    }

    public SceneLoadConfig(SCENE_NAME sceneName)
    {
        SceneName = sceneName.ToString();
    }
}
