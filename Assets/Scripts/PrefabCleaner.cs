using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PrefabCleaner
{
    [MenuItem("Tools/Clean Missing Scripts In Open Prefab")]
    private static void CleanMissingScriptsInOpenPrefab()
    {
        // Lấy stage hiện tại của prefab
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null)
        {
            Debug.LogWarning("Không có prefab nào đang mở trong Prefab Mode.");
            return;
        }

        GameObject prefabRoot = stage.prefabContentsRoot;
        int count = 0;

        // Xóa script null
        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabRoot);

        // Xóa cho toàn bộ con
        foreach (Transform child in prefabRoot.GetComponentsInChildren<Transform>(true))
        {
            count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        if (count > 0)
        {
            Debug.Log($"Đã xóa {count} script null trong prefab '{prefabRoot.name}'");
            EditorSceneManager.MarkSceneDirty(stage.scene); // đánh dấu scene để có thể lưu
        }
        else
        {
            Debug.Log("Không tìm thấy script null trong prefab.");
        }
    }
}