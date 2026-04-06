using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DownloadBundleManager : MonoBehaviour
{
    [SerializeField] private List<string> keys;
    public bool DownloadAllBundleOnEnable = true;
    readonly DownloadBundleTaskQueue _downloadTasks = new();

    private void Start()
    {
        if (DownloadAllBundleOnEnable)
        {
            LoadBundleAllAsync();
        }
    }

    private async UniTask UpdateCatalogs()
    {
        List<string> catalogsToUpdate = new List<string>();
        AsyncOperationHandle<List<string>> checkForUpdateHandle = Addressables.CheckForCatalogUpdates();
        checkForUpdateHandle.Completed += op => { catalogsToUpdate.AddRange(op.Result); };
        await checkForUpdateHandle;
        if (catalogsToUpdate.Count > 0)
        {
            AsyncOperationHandle<List<IResourceLocator>> updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate);
            await updateHandle;
            if (updateHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Catalogs updated");
            }
            else
            {
                Debug.LogError("Failed to update catalogs");
            }
        }
    }

    public async UniTask<DownloadBundleTask> LoadBundleAsync(string keyName)
    {
        if (NoInternet()) return null;
        return await DownloadBundleAsync(keyName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyNames"></param>
    public async UniTask<List<DownloadBundleTask>> LoadBundleManyAsync(List<string> keyNames)
    {
        if (NoInternet()) return new();

        return await DownloadBundleManyAsync(keyNames);
    }

    public async UniTask<DownloadBundleTask> GetDownloadBundleTask(string keyName)
    {
        var key = await GetBundleKey(keyName);
        return _downloadTasks.GetTask(key);
    }

    private async UniTask<DownloadBundleTask> DownloadBundleAsync(string keyName)
    {
        DownloadBundleTask result;
        var bundleKey = await GetBundleKey(keyName);
        if (bundleKey == -1) return null;
        if (_downloadTasks.TryGetTask(bundleKey, out var downloadTask))
        {
            return downloadTask;
        }

        result = new DownloadBundleTask();
        result.BundleKey = bundleKey;
        result.MainTask = async () => await DownloadBundle(keyName, result);
        _downloadTasks.Add(result);
        return result;
    }

    // Cập nhật phương thức DownloadBundle
    private async UniTask<List<DownloadBundleTask>> DownloadBundleManyAsync(List<string> keyNames)
    {
        List<DownloadBundleTask> result = new();
        foreach (var keyName in keyNames)
        {
            result.Add(await DownloadBundleAsync(keyName));
        }

        return result;
    }

    private async UniTask DownloadBundle(string keyName, DownloadBundleTask downloadBundleTask)
    {
        if (downloadBundleTask == null) return;

        AsyncOperationHandle? downloadHandle = null;
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            downloadBundleTask.CancellationTokenSource.Token,
            this.GetCancellationTokenOnDestroy()
        );

        try
        {
            downloadHandle = Addressables.DownloadDependenciesAsync(
                new List<string>() { keyName },
                Addressables.MergeMode.Union
            );

            while (!downloadHandle.Value.IsDone)
            {
                // Kiểm tra nếu có yêu cầu hủy
                if (linkedCts.Token.IsCancellationRequested || !FakeInternet)
                {
                    Debug.Log($"⏹️ Đã dừng tải bundle: {keyName}");
                    downloadBundleTask.OnCancelled?.Invoke();
                    return;
                }

                float percent = downloadHandle.Value.PercentComplete;
                downloadBundleTask.OnProgress?.Invoke(percent);
                Debug.Log($"keyName download: {keyName} percent: {percent}");

                await UniTask.DelayFrame(5, cancellationToken: linkedCts.Token);
            }

            if (downloadHandle.Value.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"✅ Tải bundle {keyName} thành công");
                downloadBundleTask.OnComplete?.Invoke();
            }
            else
            {
                Debug.LogError($"❌ Tải bundle {keyName} thất bại");
                downloadBundleTask.OnFailed?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"⏹️ Đã hủy tải bundle: {keyName}");
            downloadBundleTask.OnCancelled?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Lỗi khi tải bundle {keyName}: {e.Message}");
            downloadBundleTask.OnFailed?.Invoke();
        }
        finally
        {
            if (downloadHandle.HasValue && downloadHandle.Value.IsValid())
            {
                Addressables.Release(downloadHandle.Value);
            }
        }
    }

    public async UniTask<int> GetBundleKey(string keyName)
    {
        var resourceLocations = await Addressables.LoadResourceLocationsAsync(keyName).Task;
        if (resourceLocations == null || resourceLocations.Count == 0) return -1;
        var location = resourceLocations.First();
        return location?.DependencyHashCode ?? -1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyNames"></param>
    /// <returns></returns>
    public async UniTask<bool> CheckFileExist(List<string> keyNames)
    {
        var sizeHandle = Addressables.GetDownloadSizeAsync(keyNames);
        await sizeHandle.ToUniTask();
        bool result = false;
        if (sizeHandle.Result == 0)
        {
            Debug.Log("✅ Bundle đã có trong cache.");
            result = true;
        }
        else
        {
            Debug.Log($"📦 Cần tải thêm: {sizeHandle.Result} bytes.");
        }

        Addressables.Release(sizeHandle);
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    public async UniTask<GameObject> InstantiateAsync(string key, Transform parentTransform = null)
    {
        var handle = Addressables.InstantiateAsync(key, parentTransform);
        await handle.ToUniTask();
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"✅ Instantiate {key} thành công");
        }
        else
        {
            Debug.LogError($"❌ Lỗi khi instantiate prefab: {key}");
        }

        return handle.Result;
    }

    public async void LoadBundleAllAsync()
    {
        await LoadBundleManyAsync(keys);
        StartDownload();
    }

    public void StartDownload(long key = 0)
    {
        _downloadTasks.Invoke(key);
    }

    #region Tools

    [Button]
    public async void CheckAllFileExist()
    {
        await CheckFileExist(keys);
    }

    [Button]
    public async void DownloadAllBundles()
    {
        LoadBundleAllAsync();
    }

    [Button]
    public async UniTaskVoid Instantiate()
    {
        foreach (var key in keys)
        {
            await InstantiateAsync(key);
            // Không release nếu muốn giữ instance tồn tại
        }
    }

    [Button]
    public void ClearCache()
    {
        Addressables.ClearDependencyCacheAsync(keys);
        Caching.ClearCache();
        Debug.Log("✅ Đã xóa cache.");
    }

    /// <summary>
    /// Xóa toàn bộ dữ liệu bundle đã tải về, bao gồm cả cache của Addressables và Unity
    /// </summary>
    [Button]
    public async UniTask ClearAllBundles()
    {
        try
        {
            // 1. Xóa tất cả cache của Addressables
            var catalogs = await Addressables.CheckForCatalogUpdates().ToUniTask();
            if (catalogs != null && catalogs.Count > 0)
            {
                await Addressables.UpdateCatalogs(catalogs, true).ToUniTask();
            }

            // 2. Xóa tất cả AssetBundles đã tải
            await Resources.UnloadUnusedAssets();
            await Addressables.ClearDependencyCacheAsync(keys, true).ToUniTask();

            // 3. Xóa toàn bộ cache của Unity
            if (Caching.ClearCache())
            {
                Debug.Log("✅ Đã xóa toàn bộ cache Unity");
            }
            else
            {
                Debug.LogWarning("⚠ Không thể xóa cache Unity, có thể đang bị sử dụng");
            }

            // 4. Xóa thư mục cache của Addressables
            string cachePath = $"{Application.persistentDataPath}/com.unity.addressables";
            if (System.IO.Directory.Exists(cachePath))
            {
                System.IO.Directory.Delete(cachePath, true);
                Debug.Log($"✅ Đã xóa thư mục cache: {cachePath}");
            }

            // 5. Xóa bộ nhớ đệm
            await Resources.UnloadUnusedAssets();
            GC.Collect();
            Debug.Log("✅ Đã xóa toàn bộ dữ liệu bundle thành công");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Lỗi khi xóa dữ liệu bundle: {e.Message}");
            throw;
        }
    }

    #endregion

    #region Download Control

    /// <summary>
    /// Dừng tải một bundle cụ thể
    /// </summary>
    public void StopDownload(string keyName)
    {
        var bundleKey = GetBundleKey(keyName).GetAwaiter().GetResult();
        if (bundleKey == -1) return;

        if (_downloadTasks.TryGetTask(bundleKey, out var task))
        {
            task.Cancel();
        }

        _downloadTasks.Remove(bundleKey);
    }

    /// <summary>
    /// Dừng tất cả các tác vụ tải đang chạy
    /// </summary>
    [Button]
    public void StopAllDownloads()
    {
        _downloadTasks.Stop();
    }

    #endregion

    public bool FakeInternet = true;
    bool _hasInternet = true;

    private float _timeUpdate = 0f;
    [SerializeField] private float TimeUpdateRoutine = 2f;

    private void Update()
    {
        _timeUpdate += Time.deltaTime;
        if (_timeUpdate > TimeUpdateRoutine)
        {
            _timeUpdate = 0f;
            MaintainInternet();
        }
    }

    private void MaintainInternet()
    {
        var hasInternet = !NoInternet() && FakeInternet;
        if (_hasInternet == hasInternet) return;
        _hasInternet = hasInternet;
        if (hasInternet)
        {
            _downloadTasks.Invoke();
        }
        else
        {
            _downloadTasks.Stop();
        }
    }

    public static bool NoInternet()
    {
        //False = có internet , True = không có internet
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
       return Application.internetReachability.Equals(NetworkReachability.NotReachable);
#endif
        return false;
    }
}


public class DownloadBundleTask : IDisposable
{
    public long BundleKey { get; set; }
    public Func<UniTask> MainTask { get; set; }
    public ActionSealed OnComplete { get; set; } = new();
    public ActionSealed OnFailed { get; set; } = new();
    public ActionSealed OnCancelled { get; set; } = new();
    public ActionSealed<float> OnProgress { get; set; } = new();
    public CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    /// <summary>
    /// Hủy tác vụ tải hiện tại
    /// </summary>
    public void Cancel()
    {
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource?.Dispose();
        }
    }

    public void Invoke()
    {
        Debug.Log($"Tải bundle: {BundleKey}");
        CancellationTokenSource = new();
        MainTask?.Invoke().Forget();
    }

    public void Dispose()
    {
        CancellationTokenSource?.Dispose();
    }
}

public class DownloadBundleTaskQueue
{
    public List<DownloadBundleTask> DownloadBundleTasks = new();

    public DownloadBundleTask CurrenTask;

    public void Stop()
    {
        CurrenTask?.Cancel();
        CurrenTask = null;
    }

    public void Add(DownloadBundleTask task)
    {
        DownloadBundleTasks.Add(task);
    }

    public DownloadBundleTask GetTask(long bundleKey)
    {
        return DownloadBundleTasks.FirstOrDefault(x => x.BundleKey == bundleKey);
    }

    public void Remove(long bundleKey)
    {
        DownloadBundleTasks.RemoveAll(x => x.BundleKey == bundleKey);
    }

    public bool TryGetTask(long bundleKey, out DownloadBundleTask task)
    {
        task = GetTask(bundleKey);
        return task != null;
    }

    public void Invoke(long bundleKey = 0)
    {
        if (DownloadBundleTasks.Count == 0) return;
        var newTask = bundleKey == 0
            ? DownloadBundleTasks.First()
            : GetTask(bundleKey);
        Invoke(newTask);
    }

    public void Invoke(DownloadBundleTask newTask)
    {
        if (CurrenTask != null)
        {
            if (newTask.BundleKey == CurrenTask.BundleKey)
            {
                return;
            }

            CurrenTask.OnComplete.UnRegister(OnComplete);
            CurrenTask.Cancel();
        }

        CurrenTask = newTask;

        DownloadBundleTasks.Remove(CurrenTask);
        DownloadBundleTasks.Insert(0, CurrenTask);
        CurrenTask.OnComplete.Register(OnComplete);
        CurrenTask.OnFailed.Register(ReleaseCurrentTask);
        CurrenTask.OnCancelled.Register(OnCurrentTaskCancel);
        CurrenTask.Invoke();
    }

    private void OnCurrentTaskCancel()
    {
        CurrenTask = null;
    }

    private void OnComplete()
    {
        if (DownloadBundleTasks.Count > 0) DownloadBundleTasks.RemoveAt(0);
        Invoke();
    }

    private void ReleaseCurrentTask()
    {
        if (DownloadBundleTasks.Count > 0) DownloadBundleTasks.RemoveAt(0);
        CurrenTask = null;
    }
}
