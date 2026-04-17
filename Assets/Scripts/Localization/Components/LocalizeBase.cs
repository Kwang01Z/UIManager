using UnityEngine;

namespace Runtime.Localization
{
    public abstract class LocalizeBase : MonoBehaviour
    {
        [Header("Localization Settings")]
        [Tooltip("Khóa tìm kiếm trong bộ từ điển CSV hoặc trong Asset Dictionary")]
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

        // Thay vì Odin Button, chúng ta có thể gọi từ Context Menu hoặc đơn giản là để trống nếu không dùng Odin
        [ContextMenu("Force Update")]
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
