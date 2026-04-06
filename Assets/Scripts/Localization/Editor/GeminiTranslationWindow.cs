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

            var config = Resources.Load<LocalizationConfig>("LocalizationConfig");
            if (config == null || string.IsNullOrEmpty(config.GeminiApiKey))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng thiết lập GeminiApiKey trong file LocalizationConfig tại Resources.", "OK");
                return;
            }

            TranslatedText = "Đang dịch...";
            Repaint();

            string prompt = $"Dịch đoạn văn bản sau sang tiếng {TargetLanguage.ToString()}:\n\n\"{SourceText}\"\n\nChú ý: Chỉ trả về nội dung đã dịch, không in thêm thông tin dư thừa nào khác, không kèm ngoặc kép bọc ngoài.";
            
            try
            {
                string result = await RequestGeminiAPI(prompt, config.GeminiApiKey);
                TranslatedText = result.Trim();
            }
            catch (Exception ex)
            {
                TranslatedText = "Lỗi: " + ex.Message;
            }
            
            Repaint();
        }

        private async UniTask<string> RequestGeminiAPI(string prompt, string apiKey)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            
            string jsonPayload = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{EscapeJson(prompt)}\"}}]}}]}}";
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception(request.error + "\n" + request.downloadHandler.text);
                }

                // Parse Json thủ công cơ bản (không dùng thư viện ngoài để tránh crash)
                string responseText = request.downloadHandler.text;
                return ExtractTextFromJsonPattern(responseText);
            }
        }

        private string EscapeJson(string str)
        {
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        // Tác giả dùng Regex hoặc trích xuất dựa trên Substring để tránh phụ thuộc thư viện Json.
        // Response format thường là: { "candidates": [ { "content": { "parts": [ { "text": "KẾT QUẢ" } ] } } ] }
        private string ExtractTextFromJsonPattern(string json)
        {
            string searchKey = "\"text\": \"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return "Lỗi: Không lấy được kết quả JSON trả về.";
            
            startIndex += searchKey.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            // Xử lý escaped quotes
            while (endIndex > 0 && json[endIndex - 1] == '\\')
            {
                endIndex = json.IndexOf("\"", endIndex + 1);
            }

            if (endIndex == -1) return "Lỗi: Parse JSON thất bại.";

            string rawText = json.Substring(startIndex, endIndex - startIndex);
            // Giải mã ngược các ký tự escaped
            return rawText.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
