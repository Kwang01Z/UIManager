using UnityEngine;

namespace Runtime.Localization
{
    public abstract class LocalizeBase : MonoBehaviour
    {
        [Title("Localization Settings")]
        [InfoBox("Khóa tìm kiếm trong bộ từ điển CSV hoặc trong Asset Dictionary")]
        public string Key;

        protected virtual void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLocalize;
            if (!string.IsNullOrEmpty(Key))
            {
                OnLocalize();
            }
        }

        protected virtual void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLocalize;
        }

        public abstract void OnLocalize();

        // Nút bấm tiện ích trong Edit Mode (yêu cầu gọi sau khi game Init)
        [Button("Force Update", ButtonSizes.Medium)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        protected void ForceUpdate()
        {
            if (Application.isPlaying)
            {
                OnLocalize();
            }
            else
            {
                Debug.LogWarning("Chỉ hoạt động trong lúc PlayMode do Dictionary khởi tạo vào Runtime.");
            }
        }
    }
}
