using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Localization
{
    [AddComponentMenu("Localization/Localize Sprite")]
    public class LocalizeSprite : LocalizeBase
    {
        private Image _image;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void OnLocalize()
        {
            Sprite sprite = LocalizationManager.GetAsset<Sprite>(Key);
            if (sprite != null)
            {
                if (_image != null)
                {
                    _image.sprite = sprite;
                }
                else if (_spriteRenderer != null)
                {
                    _spriteRenderer.sprite = sprite;
                }
            }
        }
    }
}
