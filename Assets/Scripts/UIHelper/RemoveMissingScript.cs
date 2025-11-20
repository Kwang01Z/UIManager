#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;



public class RemoveMissingScript : Editor
{
    [MenuItem("GameObject/RemoveMissingScripts", false, 0)]
    public static void RemoveMissingScripts()
    {
        GameObject[] gos;

        if (PrefabStageUtility.GetCurrentPrefabStage() != null) // In Prefab Mode
        {
            var root = PrefabStageUtility.GetCurrentPrefabStage()
                .prefabContentsRoot;
            gos = root.GetComponentsInChildren<Transform>(true)
                .Select(x => x.gameObject)
                .ToArray();
        }
        else
        {
            gos = Object.FindObjectsOfType<GameObject>(true);
        }

        foreach (var go in gos)
        {
            RemoveMissingScriptsFromGameObject(go);
        }
    }
    private static void RemoveMissingScriptsFromGameObject(GameObject current)
    {
        if (current == null) return;

        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(current);
        if (missingCount == 0) return;

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(current);
        EditorUtility.SetDirty(current);

        Debug.Log($"Removed {missingCount} Missing Scripts from {current.name}", current);
    }
}
#endif

