using System;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Runtime.Localization.Editor
{
    public class GeminiTranslationWindow : EditorWindow
    {
        [MenuItem("Tools/Localization/Gemini Translator")]
        private static void OpenWindow()
        {
            var window = GetWindow<GeminiTranslationWindow>("Gemini Translator");
            window.Show();
        }

        public string SourceText = "";
        public LanguageCode TargetLanguage = LanguageCode.vi;
        public string TranslatedText = "";

        private void OnGUI()
        {
            GUILayout.Label("Văn bản gốc (Tiếng Anh)", EditorStyles.boldLabel);
            SourceText = EditorGUILayout.TextArea(SourceText, GUILayout.Height(60));

            GUILayout.Space(10);
            TargetLanguage = (LanguageCode)EditorGUILayout.EnumPopup("Ngôn ngữ đích", TargetLanguage);

            GUILayout.Space(10);
            GUILayout.Label("Kết quả dịch", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(TranslatedText, GUILayout.Height(60));
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(15);
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("Dịch (Translate)", GUILayout.Height(40)))
            {
                StartTranslate();
            }
            GUI.backgroundColor = defaultColor;
        }

        private async void StartTranslate()
        {
            if (string.IsNullOrEmpty(SourceText))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập văn bản gốc.", "OK");
                return;
            }

            // Cố gắng tìm config bằng AssetDatabase thay vì Resources.Load để linh hoạt hơn
            LocalizationConfig config = null;
            string[] guids = AssetDatabase.FindAssets("t:LocalizationConfig");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(path);
            }

            if (config == null || string.IsNullOrEmpty(config.GeminiApiKey))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng thiết lập GeminiApiKey trong file LocalizationConfig.\n(Đảm bảo file asset này tồn tại trong project và đã nhập Key)", "OK");
                return;
            }

            TranslatedText = "Đang dịch...";
            Repaint();

            string prompt = $"Dịch đoạn văn bản sau sang tiếng {TargetLanguage.ToString()}:\n\n\"{SourceText}\"\n\nChú ý: Chỉ trả về nội dung đã dịch, không in thêm thông tin dư thừa nào khác, không kèm ngoặc kép bọc ngoài.";
            
            try
            {
                string result = await GeminiTranslatorHelper.RequestGeminiAPI(prompt, config.GeminiApiKey, config.GeminiModel);
                TranslatedText = result.Trim();
            }
            catch (Exception ex)
            {
                TranslatedText = "Lỗi: " + ex.Message;
            }
            
            Repaint();
        }
    }
}
