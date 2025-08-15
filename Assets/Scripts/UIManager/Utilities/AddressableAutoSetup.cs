#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Utility class cho việc auto-setup Addressable entries cho UI layers
/// Hỗ trợ batch operations và validation
/// </summary>
public static class AddressableAutoSetup
{
    private const string LAYER_PREFIX = "Layers/";
    private const string UI_LAYER_GROUP_NAME = "UI Layers";

    /// <summary>
    /// Auto-setup Addressable cho tất cả LayerBase prefabs trong project
    /// </summary>
    [MenuItem("UIManager/Addressables/Auto-Setup All Layer Prefabs")]
    public static void AutoSetupAllLayerPrefabs()
    {
        var results = SetupAllLayerPrefabs();
        
        Debug.Log("=== Addressable Auto-Setup Results ===");
        Debug.Log($"✅ Created: {results.CreatedCount}");
        Debug.Log($"🔄 Updated: {results.UpdatedCount}");  
        Debug.Log($"✅ Already Configured: {results.AlreadyConfiguredCount}");
        Debug.Log($"❌ Failed: {results.FailedCount}");
        
        if (results.FailedPrefabs.Count > 0)
        {
            Debug.LogWarning($"Failed prefabs: {string.Join(", ", results.FailedPrefabs)}");
        }
    }

    /// <summary>
    /// Validate tất cả LayerBase prefabs có đúng Addressable setup không
    /// </summary>
    [MenuItem("UIManager/Addressables/Validate All Layer Prefabs")]
    public static void ValidateAllLayerPrefabs()
    {
        var issues = ValidateLayerPrefabsSetup();
        
        if (issues.Count == 0)
        {
            Debug.Log("✅ All LayerBase prefabs have correct Addressable setup!");
        }
        else
        {
            Debug.LogWarning($"⚠️ Found {issues.Count} Addressable setup issues:");
            foreach (var issue in issues)
            {
                Debug.LogWarning($"  - {issue}");
            }
        }
    }

    /// <summary>
    /// Tạo dedicated Addressable group cho UI layers
    /// </summary>
    [MenuItem("UIManager/Addressables/Create UI Layers Group")]
    public static void CreateUILayersGroup()
    {
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        if (addressableSettings == null)
        {
            Debug.LogError("Addressable settings not found. Please setup Addressables first.");
            return;
        }

        // Check if group already exists
        var existingGroup = addressableSettings.groups.FirstOrDefault(g => g.Name == UI_LAYER_GROUP_NAME);
        if (existingGroup != null)
        {
            Debug.Log($"✅ UI Layers group already exists: {UI_LAYER_GROUP_NAME}");
            return;
        }

        // Create new group
        var groupTemplate = addressableSettings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
        var newGroup = addressableSettings.CreateGroup(UI_LAYER_GROUP_NAME, false, false, true, null, groupTemplate.GetTypes());
        
        if (newGroup != null)
        {
            Debug.Log($"✅ Created Addressable group: {UI_LAYER_GROUP_NAME}");
            
            // Optional: Configure group settings for UI optimization
            ConfigureUILayerGroupSettings(newGroup);
        }
        else
        {
            Debug.LogError($"❌ Failed to create Addressable group: {UI_LAYER_GROUP_NAME}");
        }
    }

    /// <summary>
    /// Move tất cả layer entries sang UI Layers group
    /// </summary>
    [MenuItem("UIManager/Addressables/Move Layers to UI Group")]
    public static void MoveLayersToUIGroup()
    {
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        if (addressableSettings == null)
        {
            Debug.LogError("Addressable settings not found.");
            return;
        }

        var uiGroup = addressableSettings.groups.FirstOrDefault(g => g.Name == UI_LAYER_GROUP_NAME);
        if (uiGroup == null)
        {
            Debug.LogWarning($"UI Layers group not found. Creating it first...");
            CreateUILayersGroup();
            uiGroup = addressableSettings.groups.FirstOrDefault(g => g.Name == UI_LAYER_GROUP_NAME);
        }

        if (uiGroup == null)
        {
            Debug.LogError("Failed to create or find UI Layers group.");
            return;
        }

        // Find all entries starting with "Layers/"
        var layerEntries = new List<AddressableAssetEntry>();
        foreach (var group in addressableSettings.groups)
        {
            var entries = group.entries.Where(e => e.address.StartsWith(LAYER_PREFIX)).ToList();
            layerEntries.AddRange(entries);
        }

        if (layerEntries.Count == 0)
        {
            Debug.Log("No layer entries found to move.");
            return;
        }

        int movedCount = 0;
        foreach (var entry in layerEntries)
        {
            if (entry.parentGroup != uiGroup)
            {
                addressableSettings.MoveEntry(entry, uiGroup);
                movedCount++;
            }
        }

        Debug.Log($"✅ Moved {movedCount} layer entries to {UI_LAYER_GROUP_NAME} group");
    }

    /// <summary>
    /// Setup Addressable cho tất cả LayerBase prefabs
    /// </summary>
    public static SetupResults SetupAllLayerPrefabs()
    {
        var results = new SetupResults();
        
        // Find all LayerBase prefabs in project
        var layerPrefabs = FindAllLayerBasePrefabs();
        
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        if (addressableSettings == null)
        {
            Debug.LogError("Addressable settings not found. Please setup Addressables first.");
            return results;
        }

        foreach (var prefabPath in layerPrefabs)
        {
            try
            {
                var setupResult = SetupAddressableForPrefab(prefabPath, addressableSettings);
                switch (setupResult)
                {
                    case SetupResult.Created:
                        results.CreatedCount++;
                        break;
                    case SetupResult.Updated:
                        results.UpdatedCount++;
                        break;
                    case SetupResult.AlreadyConfigured:
                        results.AlreadyConfiguredCount++;
                        break;
                    case SetupResult.Failed:
                        results.FailedCount++;
                        results.FailedPrefabs.Add(Path.GetFileNameWithoutExtension(prefabPath));
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error setting up {prefabPath}: {e.Message}");
                results.FailedCount++;
                results.FailedPrefabs.Add(Path.GetFileNameWithoutExtension(prefabPath));
            }
        }

        return results;
    }

    /// <summary>
    /// Validate LayerBase prefabs setup
    /// </summary>
    public static List<string> ValidateLayerPrefabsSetup()
    {
        var issues = new List<string>();
        
        var layerPrefabs = FindAllLayerBasePrefabs();
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (addressableSettings == null)
        {
            issues.Add("Addressable settings not found");
            return issues;
        }

        foreach (var prefabPath in layerPrefabs)
        {
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            string expectedKey = $"{LAYER_PREFIX}{prefabName}";
            
            var entry = addressableSettings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
            
            if (entry == null)
            {
                issues.Add($"❌ {prefabName}: Not in Addressables");
            }
            else if (entry.address != expectedKey)
            {
                issues.Add($"⚠️ {prefabName}: Key mismatch - Expected: '{expectedKey}', Actual: '{entry.address}'");
            }
        }

        return issues;
    }

    private static List<string> FindAllLayerBasePrefabs()
    {
        var prefabPaths = new List<string>();
        
        // Search for all prefabs in project
        string[] allPrefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in allPrefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null && prefab.GetComponent<LayerBase>() != null)
            {
                prefabPaths.Add(path);
            }
        }
        
        return prefabPaths;
    }

    private static SetupResult SetupAddressableForPrefab(string assetPath, AddressableAssetSettings settings)
    {
        string prefabName = Path.GetFileNameWithoutExtension(assetPath);
        string addressableKey = $"{LAYER_PREFIX}{prefabName}";
        
        var existingEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
        
        if (existingEntry == null)
        {
            // Create new entry
            var defaultGroup = settings.DefaultGroup;
            var newEntry = settings.CreateOrMoveEntry(
                AssetDatabase.AssetPathToGUID(assetPath),
                defaultGroup,
                false,
                false
            );
            
            if (newEntry != null)
            {
                newEntry.address = addressableKey;
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, newEntry, true);
                return SetupResult.Created;
            }
            else
            {
                return SetupResult.Failed;
            }
        }
        else if (existingEntry.address != addressableKey)
        {
            // Update existing entry
            existingEntry.address = addressableKey;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, existingEntry, true);
            return SetupResult.Updated;
        }
        else
        {
            // Already configured correctly
            return SetupResult.AlreadyConfigured;
        }
    }

    private static void ConfigureUILayerGroupSettings(AddressableAssetGroup group)
    {
        // Optional: Configure group for UI-specific optimizations
        // This could include bundle settings, compression, etc.
        Debug.Log($"✅ Configured settings for group: {group.Name}");
    }

    /// <summary>
    /// Generate report of current Addressable setup
    /// </summary>
    [MenuItem("UIManager/Addressables/Generate Setup Report")]
    public static void GenerateSetupReport()
    {
        var layerPrefabs = FindAllLayerBasePrefabs();
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== UI Layer Addressable Setup Report ===");
        report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        if (addressableSettings == null)
        {
            report.AppendLine("❌ Addressable settings not found");
        }
        else
        {
            report.AppendLine($"Total LayerBase prefabs found: {layerPrefabs.Count}");
            report.AppendLine();
            
            int configuredCount = 0;
            int incorrectCount = 0;
            
            foreach (var prefabPath in layerPrefabs)
            {
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                string expectedKey = $"{LAYER_PREFIX}{prefabName}";
                
                var entry = addressableSettings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
                
                if (entry == null)
                {
                    report.AppendLine($"❌ {prefabName}: Not in Addressables");
                    incorrectCount++;
                }
                else if (entry.address != expectedKey)
                {
                    report.AppendLine($"⚠️ {prefabName}: '{entry.address}' (Expected: '{expectedKey}')");
                    incorrectCount++;
                }
                else
                {
                    report.AppendLine($"✅ {prefabName}: '{entry.address}'");
                    configuredCount++;
                }
            }
            
            report.AppendLine();
            report.AppendLine("=== Summary ===");
            report.AppendLine($"✅ Correctly configured: {configuredCount}");
            report.AppendLine($"❌ Needs attention: {incorrectCount}");
        }
        
        Debug.Log(report.ToString());
    }

    public enum SetupResult
    {
        Created,
        Updated, 
        AlreadyConfigured,
        Failed
    }

    public class SetupResults
    {
        public int CreatedCount;
        public int UpdatedCount;
        public int AlreadyConfiguredCount;
        public int FailedCount;
        public List<string> FailedPrefabs = new List<string>();
    }
}
#endif
