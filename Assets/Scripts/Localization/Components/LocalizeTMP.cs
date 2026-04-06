using UnityEngine;
using TMPro;

namespace Runtime.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Localization/Localize TMP")]
    public class LocalizeTMP : LocalizeBase
    {
        private TMP_Text _tmpText;

        private void Awake()
        {
            _tmpText = GetComponent<TMP_Text>();
        }

        public override void OnLocalize()
        {
            if (_tmpText == null) return;
            
            // Lấy chuỗi văn bản không tạo rác
            string localizedValue = LocalizationManager.GetText(Key);
            
            // Gán trực tiếp.
            // Để hạn chế Allocation thì đôi khi TextMeshPro cung cấp SetCharArray nhưng API string thông thường
            // TMPro đã tự xử lý tối ưu cache mesh của nó.
            _tmpText.text = localizedValue;
        }
    }
}
