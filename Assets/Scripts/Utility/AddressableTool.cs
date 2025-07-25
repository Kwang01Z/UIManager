#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UI;
using Object = UnityEngine.Object;


/// <summary>
/// Công cụ hỗ trợ thiết lập địa chỉ Addressable tự động cho các prefab
/// </summary>
public class AddressableTool : EditorWindow
{
    private string targetFolder = "Assets/";
    private string addressableGroupName = "Default Local Group";
    private string keyRule = "Levels/{0}";
    private string message = "";
    private MessageType messageType = MessageType.Info;
    private Vector2 scrollPosition;
    private Dictionary<string, string> customKeyRules = new Dictionary<string, string>();
    private Dictionary<string, string> customGroupRules = new Dictionary<string, string>();
    
    [System.Serializable]
    public class LevelRangeRule
    {
        public int startLevel;
        public int endLevel;
        public string targetGroupName;
        public bool isEnabled = true;
        
        public bool ContainsLevel(int level) => isEnabled && level >= startLevel && level <= endLevel;
    }
    
    //[SerializeField] private LevelConfigSO levelConfig;
    [SerializeField] private List<LevelRangeRule> levelGroupRules = new List<LevelRangeRule>();
    private Vector2 levelRulesScrollPos;
    private bool showLevelRules = true;
    private string[] allGroupNames = new string[0];

    [MenuItem("Tools/Addressable/Setup Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<AddressableTool>("Addressable Setup");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        // Lấy danh sách tất cả các group khi khởi tạo
        UpdateGroupNames();
    }

    private void UpdateGroupNames()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            allGroupNames = settings.groups
                .Where(g => g != null)
                .Select(g => g.name)
                .ToArray();
        }
    }

    private void OnGUI()
    {
        // Bắt đầu scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        
        GUILayout.Space(10);
        
        // Phần thông tin hướng dẫn
        EditorGUILayout.HelpBox("Công cụ thiết lập Addressable cho prefabs theo thư mục và level", MessageType.Info);
        
        // === PHẦN CHỌN THƯ MỤC ===
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Cấu hình chung", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Chọn thư mục
        EditorGUILayout.BeginHorizontal();
        targetFolder = EditorGUILayout.TextField("Thư mục đích", targetFolder);
        if (GUILayout.Button("Duyệt...", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Chọn thư mục", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                targetFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
            else if (!string.IsNullOrEmpty(path))
            {
                ShowMessage("Vui lòng chọn thư mục trong thư mục dự án", MessageType.Warning);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (!string.IsNullOrEmpty(targetFolder) && !AssetDatabase.IsValidFolder(targetFolder))
        {
            EditorGUILayout.HelpBox("Thư mục không tồn tại hoặc không hợp lệ", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
        
        // Dấu phân cách giữa các phần
        EditorGUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space(10);
        
        // === PHẦN LEVEL GROUP RULES ===
        DrawLevelGroupRulesSection();
        
        // Dấu phân cách giữa các phần
        EditorGUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        EditorGUILayout.Space(10);

        // Kiểm tra và lấy cài đặt Addressable
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Không tìm thấy cấu hình Addressable. Vui lòng cài đặt Addressable trước.", MessageType.Error);
            if (GUILayout.Button("Open Addressables Groups"))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        // Chọn nhóm mặc định
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Addressable Settings", EditorStyles.boldLabel);
        
        var groups = new List<string>();
        int defaultIndex = 0;
        
        foreach (var group in settings.groups)
        {
            if (group != null)
            {
                groups.Add(group.name);
                if (group.name == addressableGroupName)
                {
                    defaultIndex = groups.Count - 1;
                }
            }
        }
        
        if (groups.Count > 0)
        {
            int newIndex = EditorGUILayout.Popup("Default Group", defaultIndex, groups.ToArray());
            addressableGroupName = groups[newIndex];
        }
        
        // Rule mặc định cho key
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Key Naming", EditorStyles.boldLabel);
        keyRule = EditorGUILayout.TextField("Key Format", keyRule);
        EditorGUILayout.HelpBox("Use {0} as placeholder for prefab name.\nExample: 'Level/{0}' will create keys like 'Level/MyPrefab'", MessageType.Info);
        
        // Custom rules section
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Custom Key Rules", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Rules will be applied to prefabs whose names start with the specified prefix.", MessageType.None);
        
        // Hiển thị các rule hiện có
        List<string> rulesToRemove = new List<string>();
        foreach (var rule in customKeyRules.ToList())
        {
            EditorGUILayout.BeginHorizontal();
            
            // Prefix input
            EditorGUIUtility.labelWidth = 60;
            string newKey = EditorGUILayout.TextField("Prefix", rule.Key, GUILayout.Width(150));
            
            // Key format input
            EditorGUIUtility.labelWidth = 40;
            string newValue = EditorGUILayout.TextField("Format", rule.Value);
            
            // Update dictionary if changed
            if (newKey != rule.Key || newValue != rule.Value)
            {
                customKeyRules.Remove(rule.Key);
                customKeyRules[newKey] = newValue;
                break; // Exit loop to avoid collection modified exception
            }
            
            // Remove button
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                rulesToRemove.Add(rule.Key);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0; // Reset label width
        }
        
        // Add new rule button
        if (GUILayout.Button("Add Key Rule", GUILayout.Width(120)))
        {
            string newKey = "Prefix";
            int counter = 1;
            while (customKeyRules.ContainsKey(newKey))
            {
                newKey = $"Prefix_{counter}";
                counter++;
            }
            customKeyRules[newKey] = "Custom/Path/{0}";
        }
        
        // Remove marked rules
        foreach (var key in rulesToRemove)
        {
            customKeyRules.Remove(key);
        }

        // Custom Group Rules
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Custom Group Assignments", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign specific groups to prefabs based on name prefixes.", MessageType.None);
        
        // Hiển thị các group rule hiện có
        List<string> groupRulesToRemove = new List<string>();
        foreach (var rule in customGroupRules.ToList())
        {
            EditorGUILayout.BeginHorizontal();
            
            // Lưu key cũ để xử lý sau
            string oldKey = rule.Key;
            
            // Prefix input
            EditorGUIUtility.labelWidth = 60;
            string newKey = EditorGUILayout.TextField("Prefix", oldKey, GUILayout.Width(150));
            
            // Group dropdown
            var groupNames = new List<string>();
            foreach (var group in settings.groups)
            {
                if (group != null)
                {
                    groupNames.Add(group.name);
                }
            }
            
            string selectedGroup = rule.Value;
            if (groupNames.Count > 0)
            {
                int currentIndex = groupNames.IndexOf(selectedGroup);
                if (currentIndex < 0) currentIndex = 0;
                
                int newIndex = EditorGUILayout.Popup(currentIndex, groupNames.ToArray());
                if (newIndex >= 0 && newIndex < groupNames.Count)
                {
                    selectedGroup = groupNames[newIndex];
                }
            }
            
            // Cập nhật rule nếu có thay đổi
            if (newKey != oldKey || selectedGroup != rule.Value)
            {
                // Xóa rule cũ và thêm rule mới với key mới
                customGroupRules.Remove(oldKey);
                customGroupRules[newKey] = selectedGroup;
                break; // Thoát khỏi vòng lặp để tránh lỗi collection modified
            }
            
            // Remove button
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                groupRulesToRemove.Add(rule.Key);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0; // Reset label width
        }
        
        // Add new group rule button
        if (GUILayout.Button("Add Group Rule", GUILayout.Width(140)))
        {
            string newKey = "Prefix";
            int counter = 1;
            while (customGroupRules.ContainsKey(newKey))
            {
                newKey = $"Prefix_{counter}";
                counter++;
            }
            customGroupRules[newKey] = addressableGroupName;
        }
        
        // Remove marked group rules
        foreach (var key in groupRulesToRemove)
        {
            customGroupRules.Remove(key);
        }

        // Nút thực hiện
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        GUI.enabled = !string.IsNullOrEmpty(targetFolder) && Directory.Exists(targetFolder);
        if (GUILayout.Button("Setup Addressables", GUILayout.Width(200), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", 
                $"Bạn có chắc chắn muốn thiết lập Addressable cho tất cả prefab trong thư mục:\n{targetFolder}?", 
                "Đồng ý", "Hủy"))
            {
                ProcessPrefabs();
            }
        }
        GUI.enabled = true;
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Hiển thị thông báo
        if (!string.IsNullOrEmpty(message))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(message, messageType);
        }
        
        // Kết thúc scroll view
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Tìm tất cả prefab trong thư mục chỉ định
    /// </summary>
    private List<Object> FindAllPrefabsInFolder(string folderPath)
    {
        List<Object> prefabs = new List<Object>();
        
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Debug.LogError("Đường dẫn thư mục không hợp lệ");
            return prefabs;
        }

        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object prefab = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        return prefabs;
    }

    /// <summary>
    /// Thiết lập Addressable cho một prefab
    /// </summary>
    private bool SetupAddressableForPrefab(Object prefab, string key, AddressableAssetGroup targetGroup)
    {
        if (prefab == null) 
        {
            Debug.LogError("Prefab không hợp lệ");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefab);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("Không tìm thấy cấu hình Addressable");
            return false;
        }

        if (targetGroup == null)
        {
            Debug.LogError("Nhóm đích không hợp lệ");
            return false;
        }

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"Không thể lấy GUID cho {assetPath}");
            return false;
        }

        try
        {
            // Lấy entry hiện tại
            var entry = settings.FindAssetEntry(guid);
            
            // Nếu entry chưa tồn tại, tạo mới
            if (entry == null)
            {
                Debug.Log($"Tạo mới entry cho {prefab.name} với key: {key} trong group: {targetGroup.name}");
                entry = settings.CreateOrMoveEntry(guid, targetGroup);
            }
            // Nếu entry đã tồn tại trong group khác, di chuyển vào group đích
            else if (entry.parentGroup != targetGroup)
            {
                Debug.Log($"Di chuyển {prefab.name} vào group: {targetGroup.name}");
                settings.MoveEntry(entry, targetGroup);
            }

            // Cập nhật địa chỉ nếu cần
            if (entry.address != key)
            {
                Debug.Log($"Cập nhật địa chỉ của {prefab.name} thành: {key}");
                entry.address = key;
            }

            // Đảm bảo các thay đổi được lưu
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi thiết lập Addressable cho {prefab.name}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Xử lý chính để thiết lập Addressable cho tất cả prefab
    /// </summary>
    private void ProcessPrefabs()
    {
        if (string.IsNullOrEmpty(targetFolder))
        {
            ShowMessage("Vui lòng chọn thư mục hợp lệ", MessageType.Error);
            return;
        }

        var prefabs = FindAllPrefabsInFolder(targetFolder);
        if (prefabs.Count == 0)
        {
            ShowMessage("Không tìm thấy prefab nào trong thư mục đã chọn", MessageType.Warning);
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            ShowMessage("Không tìm thấy cấu hình Addressable", MessageType.Error);
            return;
        }

        // Tìm group đã chọn hoặc sử dụng group mặc định
        AddressableAssetGroup selectedGroup = null;
        foreach (var group in settings.groups)
        {
            if (group != null && group.name == addressableGroupName)
            {
                selectedGroup = group;
                break;
            }
        }
        
        // Nếu không tìm thấy group, sử dụng group mặc định
        if (selectedGroup == null)
        {
            selectedGroup = settings.DefaultGroup;
            addressableGroupName = selectedGroup.name;
        }
        int successCount = 0;

        // Xử lý từng prefab
        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            // Áp dụng rule cho key
            string key = "";
            bool hasCustomRule = false;
            
            // Kiểm tra rule tùy chỉnh
            foreach (var rule in customKeyRules)
            {
                if (prefab.name.StartsWith(rule.Key))
                {
                    key = string.Format(rule.Value, prefab.name);
                    hasCustomRule = true;
                    break;
                }
            }
            
            // Nếu không có rule tùy chỉnh, sử dụng rule mặc định
            if (!hasCustomRule)
            {
                key = string.Format(keyRule, prefab.name);
            }
            
            // Áp dụng group
            AddressableAssetGroup groupToUse = selectedGroup;
            foreach (var rule in customGroupRules)
            {
                if (prefab.name.StartsWith(rule.Key))
                {
                    // Tìm group tương ứng
                    foreach (var group in settings.groups)
                    {
                        if (group != null && group.name == rule.Value)
                        {
                            groupToUse = group;
                            break;
                        }
                    }
                    break;
                }
            }
            
            if (SetupAddressableForPrefab(prefab, key, groupToUse))
            {
                successCount++;
            }

            // Cập nhật tiến trình
            if (EditorUtility.DisplayCancelableProgressBar(
                "Đang xử lý...", 
                $"Đang thiết lập {prefab.name} ({i+1}/{prefabs.Count})", 
                (float)i / prefabs.Count))
            {
                break;
            }
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        ShowMessage($"Đã xử lý xong {successCount}/{prefabs.Count} object", 
                   successCount > 0 ? MessageType.Info : MessageType.Warning);
    }

    /// <summary>
    /// Vẽ giao diện quản lý Level Group Rules
    /// </summary>
    private void DrawLevelGroupRulesSection()
    {
        /*EditorGUILayout.Space(15);
        showLevelRules = EditorGUILayout.Foldout(showLevelRules, "Cấu hình Level Group", EditorStyles.foldoutHeader);
            
        if (!showLevelRules)
            return;
                
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox("Cấu hình các nhóm Addressable cho từng khoảng level", MessageType.Info);

        // Kéo thả LevelConfigSO
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        levelConfig = (LevelConfigSO)EditorGUILayout.ObjectField("Level Config", levelConfig, typeof(LevelConfigSO), false);
        /*if (EditorGUI.EndChangeCheck() && levelConfig != null)
        {
            InitializeRulesFromConfig();
        }#1#
        
        // Nút tạo rule tự động
        if (GUILayout.Button("Tạo Rule Tự Động", GUILayout.Width(120)) && levelConfig != null)
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc chắn muốn tạo rule tự động? Các rule cũ sẽ bị xóa.", "Đồng ý", "Hủy"))
            {
                InitializeRulesFromConfig();
            }
        }
        if (GUILayout.Button("Clear Rule All", GUILayout.Width(120)) && levelConfig != null)
        {
            levelGroupRules.Clear();
        }
        EditorGUILayout.EndHorizontal();

        // Hiển thị danh sách rules
        levelRulesScrollPos = EditorGUILayout.BeginScrollView(levelRulesScrollPos, GUILayout.MaxHeight(200));
        
        for (int i = 0; i < levelGroupRules.Count; i++)
        {
            var rule = levelGroupRules[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            // Enable/Disable
            rule.isEnabled = EditorGUILayout.Toggle(rule.isEnabled, GUILayout.Width(20));
            
            // Level range
            EditorGUIUtility.labelWidth = 40;
            rule.startLevel = EditorGUILayout.IntField("From", rule.startLevel, GUILayout.Width(120));
            rule.endLevel = EditorGUILayout.IntField("To", rule.endLevel, GUILayout.Width(120));
            
            // Group selection
            int selectedIndex = Mathf.Max(0, Array.IndexOf(allGroupNames, rule.targetGroupName));
            selectedIndex = EditorGUILayout.Popup(selectedIndex, allGroupNames, GUILayout.Width(150));
            if (selectedIndex >= 0 && selectedIndex < allGroupNames.Length)
            {
                rule.targetGroupName = allGroupNames[selectedIndex];
            }
            
            // Remove button
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                levelGroupRules.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        
        // Nút thêm rule và áp dụng
        EditorGUILayout.BeginHorizontal();
        
        // Nút thêm rule mới
        if (GUILayout.Button("+ Thêm Rule", GUILayout.Width(120)))
        {
            levelGroupRules.Add(new LevelRangeRule {
                startLevel = levelGroupRules.Count > 0 ? levelGroupRules.Last().endLevel + 1 : 1,
                endLevel = levelGroupRules.Count > 0 ? levelGroupRules.Last().endLevel + 5 : 5,
                targetGroupName = allGroupNames.Length > 0 ? allGroupNames[0] : "Default",
                isEnabled = true
            });
        }
        
        GUILayout.FlexibleSpace();
        
        // Nút áp dụng
        if (GUILayout.Button("Áp dụng Level Group Rules", GUILayout.Width(180), GUILayout.Height(25)))
        {
            if (levelGroupRules.Count == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng thêm ít nhất một rule trước khi áp dụng", "OK");
            }
            else if (string.IsNullOrEmpty(targetFolder) || !AssetDatabase.IsValidFolder(targetFolder))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn thư mục đích hợp lệ trước khi áp dụng", "OK");
            }
            else
            {
                ApplyLevelGroupRules();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        _groupFormat = EditorGUILayout.TextField("Format", _groupFormat);
        EditorGUILayout.EndVertical(); // Kết thúc help box*/
    }

    public string _groupFormat = "{0}";
    
    /// <summary>
    /// Khởi tạo rules từ LevelConfig
    /// </summary>
    private void InitializeRulesFromConfig()
    {
        /*if (levelConfig == null || levelConfig.Datas == null)
            return;
            
        // Tạo rules dựa trên levelDatas trong LevelConfigSO
        levelGroupRules.Clear();
        
        // Lấy danh sách level duy nhất và sắp xếp
        var levels = levelConfig.levelConfigDataList
            .Where(x => x != null)
            .Select(x => x.Level)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
            
        if (levels.Count == 0)
            return;
            
        // Tạo các rule mặc định (ví dụ: cứ 5 level 1 nhóm)
        for (int i = 0; i < levels.Count; i += 10)
        {
            int start = levels[i];
            int end = (i + 4 < levels.Count) ? levels[Mathf.Min(i + 4, levels.Count - 1)] : levels[levels.Count - 1];
            
            levelGroupRules.Add(new LevelRangeRule {
                startLevel = start,
                endLevel = end,
                targetGroupName = allGroupNames.Length > 0 ? allGroupNames[0] : "Default",
                isEnabled = true
            });
        }*/
    }
    
    /// <summary>
    /// Lấy danh sách các prefab trong thư mục đích
    /// </summary>
    private List<Object> GetPrefabsInTargetFolder()
    {
        var prefabs = new List<Object>();
        
        if (string.IsNullOrEmpty(targetFolder) || !AssetDatabase.IsValidFolder(targetFolder))
            return prefabs;
            
        try
        {
            // Lấy tất cả file prefab trong thư mục đích
            string[] prefabGuids = AssetDatabase.FindAssets("", new[] { targetFolder });
            
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi đọc thư mục: {e.Message}");
        }
        
        return prefabs;
    }
    
    /// <summary>
    /// Lấy key addressable của một prefab
    /// </summary>
    private string GetAddressableKey(GameObject prefab)
    {
        if (prefab == null) return string.Empty;
        
        // Lấy địa chỉ từ AddressableAssetSettings
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return string.Empty;
        
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        var entry = settings.FindAssetEntry(guid);
        
        return entry?.address ?? string.Empty;
    }

    private string GetPrefabNameFromKey(string key)
    {
        var strs = key.Split('/').ToList();
        var result = strs.Count > 0 ? string.Format(_groupFormat, strs.Last()) : key;
        return result;
    }

    /// <summary>
    /// Áp dụng Level Group Rules cho các prefab trong thư mục target
    /// </summary>
    private void ApplyLevelGroupRules()
    {
        /*try
        {
            // Kiểm tra LevelConfig
            if (levelConfig == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn LevelConfigSO trước", "OK");
                return;
            }

            if (levelConfig.Datas == null || levelConfig.Datas.Count == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "LevelConfig không có dữ liệu level nào", "OK");
                return;
            }

            // Kiểm tra thư mục đích
            if (string.IsNullOrEmpty(targetFolder) || !AssetDatabase.IsValidFolder(targetFolder))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn thư mục đích hợp lệ", "OK");
                return;
            }

            // Kiểm tra cài đặt Addressable
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy cấu hình Addressable", "OK");
                return;
            }

            // Lấy danh sách prefab trong thư mục đích
            var targetPrefabs = GetPrefabsInTargetFolder();
            if (targetPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy prefab nào trong thư mục đích", "OK");
                return;
            }

            // Tạo dictionary để tra cứu nhanh prefab theo key addressable
            var prefabByKey = new Dictionary<string, Object>();
            foreach (var prefab in targetPrefabs)
            {
                prefabByKey[prefab.name] = prefab;
                /*string key = GetAddressableKey(prefab);
                if (!string.IsNullOrEmpty(key) && !prefabByKey.ContainsKey(key))
                {
                    prefabByKey[key] = prefab;
                }#1#
            }

            int processedCount = 0;
            int totalLevels = levelConfig.Datas.Count;
            int skippedCount = 0;
            bool hasError = false;

            // Xử lý từng level
            for (int i = 0; i < totalLevels; i++)
            {
                var levelData = levelConfig.Datas[i];
                
                // Kiểm tra dữ liệu level
                if (levelData == null)
                {
                    Debug.LogWarning($"Level tại index {i} là null, bỏ qua...");
                    continue;
                }

                // Cập nhật tiến trình
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Đang xử lý...", 
                    $"Đang xử lý level {levelData.Level} ({i+1}/{totalLevels})", 
                    (float)i / totalLevels))
                {
                    Debug.Log("Đã hủy quá trình xử lý");
                    break;
                }
                

                // Kiểm tra key addressable từ MainPrefabPath
                if (string.IsNullOrEmpty(levelData.MainPrefabPath))
                {
                    Debug.LogWarning($"Level {levelData.Level} không có key addressable, bỏ qua...");
                    skippedCount++;
                    continue;
                }
                
                var prefabName = GetPrefabNameFromKey(levelData.MainPrefabPath);

                // Tìm prefab tương ứng với key addressable
                if (!prefabByKey.TryGetValue(prefabName, out var mainPrefab))
                {
                    Debug.Log($"Không tìm thấy prefab với key addressable: {levelData.MainPrefabPath}");
                    skippedCount++;
                    continue;
                }

                // Tìm rule phù hợp
                bool ruleApplied = false;
                foreach (var rule in levelGroupRules.Where(r => r.isEnabled))
                {
                    if (rule.ContainsLevel(levelData.Level))
                    {
                        // Tìm group đích
                        var targetGroup = settings.FindGroup(rule.targetGroupName);
                        if (targetGroup == null)
                        {
                            Debug.LogWarning($"Không tìm thấy group: {rule.targetGroupName}");
                            hasError = true;
                            continue;
                        }

                        var newKey = string.Format(keyRule, prefabName);
                        // Thiết lập Addressable
                        if (SetupAddressableForPrefab(mainPrefab, newKey, targetGroup))
                        {
                            processedCount++;
                            Debug.Log($"Đã áp dụng {rule.targetGroupName} cho level {levelData.Level}");
                            ruleApplied = true;
                            break; // Chỉ áp dụng rule đầu tiên phù hợp
                        }
                        else
                        {
                            hasError = true;
                        }
                    }
                }


                if (!ruleApplied)
                {
                    Debug.LogWarning($"Không tìm thấy rule phù hợp cho level {levelData.Level}");
                    hasError = true;
                }
            }

            // Hiển thị kết quả
            if (processedCount > 0 || skippedCount > 0)
            {
                string resultMessage = $"Đã xử lý xong {processedCount}/{totalLevels} object";
                
                if (skippedCount > 0)
                {
                    resultMessage += $" (Đã bỏ qua {skippedCount} object không nằm trong thư mục đích)";
                }
                
                if (hasError)
                {
                    resultMessage += " (có lỗi)";
                    ShowMessage(resultMessage, MessageType.Warning);
                }
                else
                {
                    ShowMessage(resultMessage, MessageType.Info);
                }
            }
            else
            {
                ShowMessage("Không có object nào được xử lý. Vui lòng kiểm tra lại cấu hình và thư mục đích.", MessageType.Warning);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Lỗi khi xử lý: {e}");
            EditorUtility.DisplayDialog("Lỗi", $"Đã xảy ra lỗi: {e.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }*/
    }

    /// <summary>
    /// Hiển thị thông báo trên giao diện
    /// </summary>
    private void ShowMessage(string msg, MessageType type = MessageType.Info)
    {
        message = msg;
        messageType = type;
        Repaint();
    }
}
#endif
