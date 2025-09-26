using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace UIManager.Editor
{
    public class LayerGeneratorWindow : EditorWindow
    {
        private string layerName = "";
        private LayerReferenceSO layerReferenceSOAsset;
        private string prefabFolderPath = "Assets/Prefabs/UI";
        private string scriptFolderPath = "Assets/Scripts/UI";
        private string layerSourcePathPath = "Assets/Scripts/UIManager/LayerSourcePath.cs";
        private Vector2 scrollPosition;
        private string resultMessage = "";
        private string prefabPath = "";
        private GameObject createdPrefab;

        private const string PREFAB_FOLDER_KEY = "LayerGenerator_PrefabFolderPath";
        private const string SCRIPT_FOLDER_KEY = "LayerGenerator_ScriptFolderPath";
        private const string LAYER_SOURCE_PATH_KEY = "LayerGenerator_LayerSourcePath";

        [MenuItem("Tools/UI/Generate Layer Window")]
        public static void ShowWindow()
        {
            GetWindow<LayerGeneratorWindow>("Layer Generator");
        }

        private void OnEnable()
        {
            // Load saved paths from PlayerPrefs
            prefabFolderPath = PlayerPrefs.GetString(PREFAB_FOLDER_KEY, "Assets/Prefabs/UI");
            scriptFolderPath = PlayerPrefs.GetString(SCRIPT_FOLDER_KEY, "Assets/Scripts/UI");
            layerSourcePathPath = PlayerPrefs.GetString(LAYER_SOURCE_PATH_KEY, "Assets/Scripts/UIManager/LayerSourcePath.cs");
        }

        private void OnDisable()
        {
            // Save current paths to PlayerPrefs
            PlayerPrefs.SetString(PREFAB_FOLDER_KEY, prefabFolderPath);
            PlayerPrefs.SetString(SCRIPT_FOLDER_KEY, scriptFolderPath);
            PlayerPrefs.SetString(LAYER_SOURCE_PATH_KEY, layerSourcePathPath);
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            GUILayout.Label("Tạo Layer Mới Nhanh Chóng", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Nhập tên layer
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Thông tin đầu vào", EditorStyles.boldLabel);
            layerName = EditorGUILayout.TextField("Tên Layer", layerName);

            // Chọn LayerReferenceSO
            layerReferenceSOAsset = EditorGUILayout.ObjectField("LayerReferenceSO", layerReferenceSOAsset, typeof(LayerReferenceSO), false) as LayerReferenceSO;

            // Chọn đường dẫn thư mục prefab
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Thư mục Prefab", prefabFolderPath);
            if (GUILayout.Button("Chọn", GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Chọn thư mục chứa prefab", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Chuyển đổi đường dẫn đầy đủ thành đường dẫn trong Assets
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        prefabFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        PlayerPrefs.SetString(PREFAB_FOLDER_KEY, prefabFolderPath);
                        PlayerPrefs.Save();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Chọn đường dẫn thư mục script
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Thư mục Script", scriptFolderPath);
            if (GUILayout.Button("Chọn", GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Chọn thư mục chứa script", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Chuyển đổi đường dẫn đầy đủ thành đường dẫn trong Assets
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        scriptFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        PlayerPrefs.SetString(SCRIPT_FOLDER_KEY, scriptFolderPath);
                        PlayerPrefs.Save();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Chọn đường dẫn file LayerSourcePath
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("LayerType enum Path", layerSourcePathPath);
            if (GUILayout.Button("Chọn", GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Chọn file LayerSourcePath.cs", "Assets", "cs");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Chuyển đổi đường dẫn đầy đủ thành đường dẫn trong Assets
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        layerSourcePathPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        PlayerPrefs.SetString(LAYER_SOURCE_PATH_KEY, layerSourcePathPath);
                        PlayerPrefs.Save();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Nút xác nhận
            if (GUILayout.Button("Tạo Layer Mới"))
            {
                if (string.IsNullOrEmpty(layerName))
                {
                    EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập tên layer", "OK");
                    return;
                }

                if (layerReferenceSOAsset == null)
                {
                    EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn LayerReferenceSO", "OK");
                    return;
                }

                // Kiểm tra tên layer có hợp lệ không
                if (!Regex.IsMatch(layerName, @"^[a-zA-Z][a-zA-Z0-9]*$"))
                {
                    EditorUtility.DisplayDialog("Lỗi", "Tên layer không hợp lệ. Chỉ chấp nhận chữ cái và số, bắt đầu bằng chữ cái.", "OK");
                    return;
                }

                GenerateLayer();
            }

            if (GUILayout.Button("Add Prefab to SO"))
            {
                // Thêm prefab vào LayerReferenceSO
                AddPrefabToLayerReferenceSO();
            }

            // Hiển thị kết quả
            if (!string.IsNullOrEmpty(resultMessage))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Kết quả:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(resultMessage, MessageType.Info);
                
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    EditorGUILayout.LabelField("Đường dẫn prefab:", prefabPath);
                }
                
                if (createdPrefab != null)
                {
                    EditorGUILayout.LabelField("Prefab Reference:");
                    EditorGUILayout.ObjectField(createdPrefab, typeof(GameObject), false);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void GenerateLayer()
        {
            try
            {
                // Tạo enum mới trong LayerSourcePath.cs
                CreateNewEnum();

                // Tạo script UI+(tên layer)
                CreateScriptClass();

                // Yêu cầu Unity biên dịch trước khi tiếp tục
                AssetDatabase.Refresh();

                // Tạo prefab
                CreatePrefab();

                // Tạo hàm helper trong ShowLayerHelper.cs
                CreateHelperMethod();

                // Cập nhật thông tin kết quả
                prefabPath = prefabFolderPath + "/" + layerName + ".prefab";
                createdPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
                resultMessage = "Đã tạo layer '" + layerName + "' thành công!\\n\\n" +
                    "Prefab được tạo tại: " + prefabPath + "\\n" +
                    "Script được tạo tại: " + scriptFolderPath + "/" + layerName + "/UI" + layerName + ".cs\\n" +
                    "Enum đã được thêm vào LayerSourcePath.cs\\n" +
                    "Hàm helper đã được thêm vào ShowLayerHelper.cs";
                
                // In log để dễ theo dõi
                Debug.Log(resultMessage);
            }
            catch (System.Exception e)
            {
                resultMessage = "Lỗi khi tạo layer: " + e.Message;
                prefabPath = "";
                createdPrefab = null;
                Debug.LogError("Lỗi khi tạo layer: " + e.Message);
            }
        }

        private void CreateNewEnum()
        {
            // Kiểm tra xem file có tồn tại không
            if (!File.Exists(layerSourcePathPath))
            {
                EditorUtility.DisplayDialog("Lỗi", "File LayerSourcePath không tồn tại tại: " + layerSourcePathPath, "OK");
                return;
            }
            
            // Đọc nội dung LayerSourcePath.cs
            string[] lines = File.ReadAllLines(layerSourcePathPath);

            // Xác định giá trị enum tiếp theo dựa trên enum hiện tại
            int lastValue = 6; // Layer06 = 6 là giá trị cuối cùng trong enum hiện tại
            string newEnumLine = "    " + layerName + " = " + (lastValue + 1) + ",";

            // Tìm vị trí cuối cùng của enum LayerType (trước dấu })
            int enumEndIndex = -1;
            bool inEnum = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("public enum LayerType"))
                {
                    inEnum = true;
                }
                
                if (inEnum && lines[i].Trim().Equals("{"))
                {
                    continue; // Bắt đầu nội dung enum
                }
                
                if (inEnum && lines[i].Trim().Equals("}"))
                {
                    enumEndIndex = i;
                    inEnum = false;
                    break;
                }
            }

            // Chèn enum mới trước dấu đóng ngoặc của enum
            if (enumEndIndex != -1)
            {
                var newLines = new string[lines.Length + 1];
                
                for (int i = 0; i < enumEndIndex; i++)
                {
                    newLines[i] = lines[i];
                }
                
                newLines[enumEndIndex] = newEnumLine;
                
                for (int i = enumEndIndex; i < lines.Length; i++)
                {
                    newLines[i + 1] = lines[i];
                }

                // Thêm hằng số đường dẫn cho layer mới
                int lastSourcePathIndex = -1;
                for (int i = 0; i < newLines.Length; i++)
                {
                    if (newLines[i].Trim().StartsWith("public const string") && newLines[i].Contains("Layer"))
                    {
                        lastSourcePathIndex = i;
                    }
                }

                if (lastSourcePathIndex >= 0)
                {
                    // Chèn hằng số đường dẫn mới sau dòng cuối cùng
                    string newSourcePathLine = "    public const string " + layerName + " = \"Layers/" + layerName + "\";";
                    var finalLines = new string[newLines.Length + 1];
                    
                    for (int i = 0; i < lastSourcePathIndex + 1; i++)
                    {
                        finalLines[i] = newLines[i];
                    }
                    
                    finalLines[lastSourcePathIndex + 1] = newSourcePathLine;
                    
                    for (int i = lastSourcePathIndex + 1; i < newLines.Length; i++)
                    {
                        finalLines[i + 1] = newLines[i];
                    }

                    File.WriteAllLines(layerSourcePathPath, finalLines);
                    AssetDatabase.Refresh();
                }
                else
                {
                    File.WriteAllLines(layerSourcePathPath, newLines);
                    AssetDatabase.Refresh();
                }
            }
        }

        private void CreateScriptClass()
        {
            // Tạo thư mục nếu chưa tồn tại
            if (!AssetDatabase.IsValidFolder(scriptFolderPath))
            {
                string[] pathParts = scriptFolderPath.Split('/');
                string currentPath = pathParts[0];

                for (int i = 1; i < pathParts.Length; i++)
                {
                    string nextPath = currentPath + "/" + pathParts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                    }
                    currentPath = nextPath;
                }
            }

            // Tạo thư mục cho layer
            string layerScriptFolderPath = scriptFolderPath + "/" + layerName;
            if (!AssetDatabase.IsValidFolder(layerScriptFolderPath))
            {
                string folderName = layerName;
                AssetDatabase.CreateFolder(scriptFolderPath, folderName);
            }

            // Đường dẫn đến script mới
            string scriptPath = layerScriptFolderPath + "/UI" + layerName + ".cs";

            // Nội dung script mới
            StringBuilder scriptContent = new StringBuilder();
            scriptContent.AppendLine("using System.Collections;");
            scriptContent.AppendLine("using System.Collections.Generic;");
            scriptContent.AppendLine("using UnityEngine;");
            scriptContent.AppendLine("");
            scriptContent.AppendLine("public class UI" + layerName + " : LayerBase");
            scriptContent.AppendLine("{");
            scriptContent.AppendLine("    // Thêm các trường cụ thể cho layer của bạn");
            scriptContent.AppendLine("");
            scriptContent.AppendLine("    public override void InitData()");
            scriptContent.AppendLine("    {");
            scriptContent.AppendLine("        base.InitData();");
            scriptContent.AppendLine("        // Khởi tạo dữ liệu cụ thể cho layer tại đây");
            scriptContent.AppendLine("    }");
            scriptContent.AppendLine("");
            scriptContent.AppendLine("    public override void ShowLayerAsync()");
            scriptContent.AppendLine("    {");
            scriptContent.AppendLine("        base.ShowLayerAsync();");
            scriptContent.AppendLine("        // Thêm logic hiển thị tùy chỉnh tại đây");
            scriptContent.AppendLine("    }");
            scriptContent.AppendLine("");
            scriptContent.AppendLine("    public override void CloseLayerAsync(bool force = false)");
            scriptContent.AppendLine("    {");
            scriptContent.AppendLine("        base.CloseLayerAsync(force);");
            scriptContent.AppendLine("        // Thêm logic đóng tùy chỉnh tại đây");
            scriptContent.AppendLine("    }");
            scriptContent.AppendLine("}");

            // Ghi file
            File.WriteAllText(scriptPath, scriptContent.ToString());
            AssetDatabase.Refresh();
        }

        private void CreatePrefab()
        {
            // Tạo thư mục prefab nếu chưa tồn tại
            if (!AssetDatabase.IsValidFolder(prefabFolderPath))
            {
                string[] pathParts = prefabFolderPath.Split('/');
                string currentPath = pathParts[0];

                for (int i = 1; i < pathParts.Length; i++)
                {
                    string nextPath = currentPath + "/" + pathParts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                    }
                    currentPath = nextPath;
                }
            }

            // Tạo GameObject tạm thời để làm prefab
            GameObject tempGO = new GameObject(layerName);
            
            // Thêm các thành phần yêu cầu
            Canvas canvas = tempGO.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;
            
            CanvasGroup canvasGroup = tempGO.AddComponent<CanvasGroup>();
            
            RectTransform rectTransform = tempGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            tempGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Chú ý: Không thêm script vào prefab ở đây, vì sẽ thêm sau trong AddPrefabToLayerReferenceSO
            // Sau khi đã chắc chắn rằng script đã được biên dịch

            // Tạo prefab
            string prefabPath = prefabFolderPath + "/" + layerName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);

            // Xóa GameObject tạm thời
            Object.DestroyImmediate(tempGO);

            AssetDatabase.Refresh();
        }

        private void CreateHelperMethod()
        {
            // Đọc nội dung ShowLayerHelper.cs
            string helperPath = "Assets/Scripts/UIManager/Test/ShowLayerHelper.cs";
            string[] lines = File.ReadAllLines(helperPath);

            // Tìm vị trí cuối cùng của hàm helper (trước dấu })
            int lastClosingBracketIndex = -1;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].Trim().Equals("}"))
                {
                    lastClosingBracketIndex = i;
                    break;
                }
            }

            if (lastClosingBracketIndex != -1)
            {
                // Tạo nội dung hàm mới
                string[] newMethodLines = new string[]
                {
                    "", // Dòng trống
                    "    private ShowLayerGroupData _show" + layerName + "Data;",
                    "    public void Show" + layerName + "(LayerGroupType layerGroupType, System.Action<LayerGroup> onDone = null)",
                    "    {",
                    "        _show" + layerName + "Data ??= LayerGroupBuilder.Build(layerGroupType, LayerType." + layerName + ");",
                    "        _show" + layerName + "Data.OnInitData = SetupData" + layerName + ";",
                    "        _show" + layerName + "Data.OnShowComplete = onDone;",
                    "        ShowGroupLayerAsync(_show" + layerName + "Data);",
                    "        return;",
                    "",
                    "        void SetupData" + layerName + "(LayerGroup layerGroup)",
                    "        {",
                    "            if(layerGroup == null) return;",
                    "            if (layerGroup.GetLayerBase(LayerType." + layerName + ", out var layerBase))",
                    "            {",
                    "                layerBase.InitData();",
                    "            }",
                    "        }",
                    "    }"
                };

                // Tạo mảng mới với các dòng cũ và thêm các dòng mới trước dấu }
                string[] finalLines = new string[lines.Length + newMethodLines.Length];
                
                int j = 0;
                for (int i = 0; i < lastClosingBracketIndex; i++)
                {
                    finalLines[j] = lines[i];
                    j++;
                }
                
                foreach (string newLine in newMethodLines)
                {
                    finalLines[j] = newLine;
                    j++;
                }
                
                for (int i = lastClosingBracketIndex; i < lines.Length; i++)
                {
                    finalLines[j] = lines[i];
                    j++;
                }

                File.WriteAllLines(helperPath, finalLines);
                AssetDatabase.ImportAsset(helperPath);
            }
        }

        private void AddPrefabToLayerReferenceSO()
        {
            // Lấy asset LayerReferenceSO
            LayerReferenceSO referenceSO = (LayerReferenceSO)layerReferenceSOAsset;
            
            if (referenceSO == null)
            {
                Debug.LogError("LayerReferenceSO không hợp lệ");
                return;
            }

            // Lấy prefab từ đường dẫn
            string prefabPath = prefabFolderPath + "/" + layerName + ".prefab";
            Object prefabAsset = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(Object));
            
            if (prefabAsset == null)
            {
                Debug.LogError("Không tìm thấy prefab tại: " + prefabPath);
                return;
            }

            // Thêm prefab vào LayerReferenceSO
            // Chuyển đổi Object thành LayerBase
            GameObject prefabGO = (GameObject)prefabAsset;
            
            // Đảm bảo script đã được biên dịch, sau đó thêm vào prefab
            System.Type layerType = System.Type.GetType("UI" + layerName + ", Assembly-CSharp");
            if (layerType != null)
            {
                // Kiểm tra xem prefab đã có script UI+(layerName) chưa, nếu chưa có thì thêm vào
                Component layerScript = prefabGO.GetComponent(layerType);
                if (layerScript == null)
                {
                    // Mở prefab ra để chỉnh sửa
                    GameObject loadedPrefab = (GameObject)PrefabUtility.LoadPrefabContents(prefabPath);
                    
                    // Thêm component vào prefab đang mở
                    loadedPrefab.AddComponent(layerType);
                    
                    // Lưu prefab đã chỉnh sửa
                    PrefabUtility.SaveAsPrefabAsset(loadedPrefab, prefabPath);
                    
                    // Đóng prefab
                    PrefabUtility.UnloadPrefabContents(loadedPrefab);
                }
            }
            else
            {
                Debug.LogError("Không thể tìm thấy lớp UI" + layerName + " trong Assembly. Có thể script chưa được biên dịch đúng cách.");
                return;
            }
            
            // Tải lại prefab sau khi đã thêm script
            GameObject finalPrefabGO = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            LayerBase layerBase = finalPrefabGO.GetComponent<LayerBase>();
            
            // Thêm dữ liệu mới vào danh sách
            LayerReferenceData newReferenceData = new LayerReferenceData();
            newReferenceData.layerType = (LayerType)System.Enum.Parse(typeof(LayerType), layerName);
            newReferenceData.layerBase = layerBase;

            if (referenceSO.layerReferenceList == null)
            {
                referenceSO.layerReferenceList = new System.Collections.Generic.List<LayerReferenceData>();
            }

            referenceSO.layerReferenceList.Add(newReferenceData);

            // Lưu thay đổi
            EditorUtility.SetDirty(referenceSO);
            AssetDatabase.SaveAssets();
            resultMessage = "Add Prefab To LayerReferenceSO Success!!!";
        }
    }
}