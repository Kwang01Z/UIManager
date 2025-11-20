using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static class quản lý việc load scene bất đồng bộ với loading screen và hỗ trợ tải nền
/// Sử dụng UniTask để xử lý bất đồng bộ
/// </summary>
public static class SceneLoader
{
    // Cấu hình mặc định
    private const string DEFAULT_LOADING_SCENE = "LoadingScene";

    // Biến static để lưu trữ trạng thái
    private static string _currentActiveScene;
    private static bool _isLoading = false;

    // Danh sách các scene đang được tải
    private static readonly Dictionary<string, (AsyncOperation operation, SceneLoadConfig config, CancellationTokenSource cts)> _loadingScenes =
        new Dictionary<string, (AsyncOperation, SceneLoadConfig, CancellationTokenSource)>();

    /// <summary>
    /// Load scene với cấu hình mặc định
    /// </summary>
    public static void LoadScene(SCENE_NAME sceneName)
    {
        var config = new SceneLoadConfig(sceneName.ToString());

        LoadScene(config).Forget();
    }

    /// <summary>
    /// Load scene với cấu hình tùy chỉnh
    /// </summary>
    public static async UniTaskVoid LoadScene(SceneLoadConfig config)
    {
        if (string.IsNullOrEmpty(config.SceneName))
        {
            Debug.LogError("Tên scene không được để trống!");
            return;
        }

        if (_isLoading && !config.LoadInBackGround)
        {
            Debug.LogWarning($"Đang tải scene khác. Hủy tải scene {config.SceneName} hoặc bật chế độ tải nền.");
            return;
        }

        // Không cần khởi tạo runner nữa vì UniTask đã có sẵn PlayerLoopHelper
        _isLoading = true;
        var cts = new CancellationTokenSource();

        // Lưu lại scene hiện tại để có thể unload sau
        string currentScene = SceneManager.GetActiveScene().name;

        try
        {
            // Gọi callback khi bắt đầu tải
            if (config.OnLoadStarted != null)
                await config.OnLoadStarted();

            if (config.ShowLoadingScreen)
            {
                // Load scene loading
                await SceneManager.LoadSceneAsync(DEFAULT_LOADING_SCENE, LoadSceneMode.Additive)
                    .ToUniTask(cancellationToken: cts.Token);

                // Bắt đầu tải scene đích
                await LoadSceneAsync(config, cts.Token);

                // Unload scene loading và scene cũ
                await SceneManager.UnloadSceneAsync(DEFAULT_LOADING_SCENE)
                    .ToUniTask(cancellationToken: cts.Token);

                if (currentScene != DEFAULT_LOADING_SCENE && currentScene != config.SceneName)
                {
                    await UnloadSceneAsync(currentScene, cts.Token);
                    if (config.OnSceneUnloaded != null)
                        await config.OnSceneUnloaded(currentScene);
                }
            }
            else
            {
                // Tải scene mà không cần loading screen
                await LoadSceneAsync(config, cts.Token);

                // Unload scene cũ nếu cần
                if (currentScene != config.SceneName && !config.DontUnloadCurrentScene)
                {
                    await UnloadSceneAsync(currentScene, cts.Token);
                    if (config.OnSceneUnloaded != null)
                        await config.OnSceneUnloaded(currentScene);
                }
            }

            // Gọi callback khi hoàn thành
            if (config.OnLoadCompleted != null)
                await config.OnLoadCompleted();
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"Đã hủy tải scene: {config.SceneName}");
            if (config.OnLoadCancelled != null)
                await config.OnLoadCancelled();

            // Nếu đang tải với loading screen thì unload nó
            if (config.ShowLoadingScreen && SceneManager.GetSceneByName(DEFAULT_LOADING_SCENE).isLoaded && !config.DontUnloadCurrentScene)
            {
                await SceneManager.UnloadSceneAsync(DEFAULT_LOADING_SCENE);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi tải scene {config.SceneName}: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            cts.Dispose();
        }
    }

    private static async UniTask LoadSceneAsync(SceneLoadConfig config, CancellationToken cancellationToken)
    {
        string sceneName = config.SceneName;
        float startTime = Time.time;

        // Kiểm tra nếu scene đã được load
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"Scene {sceneName} đã được load trước đó!");
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            if (config.OnSceneLoaded != null)
                await config.OnSceneLoaded(sceneName);
            return;
        }

        // Bắt đầu load scene
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOperation.allowSceneActivation = false;

        // Lưu lại operation để có thể hủy nếu cần
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadingScenes[sceneName] = (loadOperation, config, linkedCts);

        try
        {
            // Chờ cho đến khi load xong 90% hoặc bị hủy
            while (!loadOperation.isDone && !cancellationToken.IsCancellationRequested)
            {
                // Cập nhật tiến độ
                float progress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                if (config.OnLoadingProgress != null)
                    await config.OnLoadingProgress(progress);

                // Nếu đã load xong 90% thì thoát khỏi vòng lặp
                if (loadOperation.progress >= 0.9f)
                    break;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            // Kiểm tra nếu bị hủy
            cancellationToken.ThrowIfCancellationRequested();


            // Kích hoạt scene mới
            loadOperation.allowSceneActivation = true;

            // Chờ cho đến khi load hoàn tất
            await UniTask.WaitUntil(() => loadOperation.isDone, cancellationToken: cancellationToken);

            // Đặt scene mới làm active
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                _currentActiveScene = sceneName;
                await UniTask.NextFrame();
                if (config.OnSceneLoaded != null)
                    await config.OnSceneLoaded(sceneName);
                if(!config.DontActiveNewScene) SceneManager.SetActiveScene(loadedScene);
            }
        }
        finally
        {
            // Xóa khỏi danh sách đang tải
            _loadingScenes.Remove(sceneName);
            linkedCts.Dispose();
        }
    }

    public static bool ActiveScene(string sceneName)
    {
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            return SceneManager.SetActiveScene(loadedScene);
        }
        return false;
    }

    /// <summary>
    /// Hủy tải scene đang được tải
    /// </summary>
    public static void CancelLoading(string sceneName)
    {
        if (_loadingScenes.TryGetValue(sceneName, out var loadingInfo))
        {
            var (_, config, cts) = loadingInfo;

            if (config.CanBeCancelled && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                Debug.Log($"Đã yêu cầu hủy tải scene: {sceneName}");
            }
        }
    }

    /// <summary>
    /// Hủy tất cả các scene đang được tải
    /// </summary>
    public static void CancelAllLoadings()
    {
        foreach (var kvp in _loadingScenes)
        {
            var (_, config, cts) = kvp.Value;
            if (config.CanBeCancelled && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }
    }

    /// <summary>
    /// Unload scene bất đồng bộ
    /// </summary>
    public static async UniTask UnloadSceneAsync(string sceneName, CancellationToken cancellationToken = default)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            return;

        var unloadOperation = SceneManager.UnloadSceneAsync(sceneName);
        await unloadOperation.ToUniTask(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Kiểm tra xem một scene có đang được tải không
    /// </summary>
    public static bool IsSceneLoading(string sceneName)
    {
        return _loadingScenes.ContainsKey(sceneName);
    }

    /// <summary>
    /// Lấy tiến độ tải của một scene
    /// </summary>
    public static float GetLoadingProgress(string sceneName)
    {
        if (_loadingScenes.TryGetValue(sceneName, out var loadingInfo))
        {
            var (operation, _, _) = loadingInfo;
            return Mathf.Clamp01(operation.progress / 0.9f);
        }
        return 0f;
    }

    /// <summary>
    /// Lấy tên scene đang active
    /// </summary>
    public static string GetCurrentActiveScene()
    {
        return _currentActiveScene;
    }
}

public enum SCENE_NAME
{
    S_Loading,
    S_Main,
    S_GamePlay,
}
