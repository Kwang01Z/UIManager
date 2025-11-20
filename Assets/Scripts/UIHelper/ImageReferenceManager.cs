using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Utilities
{
    [Serializable]
    public class ImageReferenceData
    {
        public Image image;
        public Sprite newSprite;
        public string path;
        public bool newRaycastTarget = true;
        public bool newMaskable = true;
        public bool isSelected;
    }

    public class ImageReferenceManager : MonoBehaviour
    {
        [HideInInspector]
        public List<ImageReferenceData> imageReferences = new List<ImageReferenceData>();

        public void RefreshReferences()
        {
            imageReferences.Clear();
            Image[] images = GetComponentsInChildren<Image>(true);

            foreach (Image img in images)
            {
                imageReferences.Add(new ImageReferenceData
                {
                    image = img,
                    newSprite = img.sprite,
                    path = GetGameObjectPath(img.transform),
                    newRaycastTarget = img.raycastTarget,
                    newMaskable = img.maskable
                });
            }
        }

        private string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null && parent != this.transform)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public void ApplyAllSprites()
        {
            foreach (var data in imageReferences)
            {
                if (data.image != null && data.newSprite != null)
                {
                    data.image.sprite = data.newSprite;
                }
            }
        }

        public void ApplyAllProperties()
        {
            foreach (var data in imageReferences)
            {
                if (data.image != null)
                {
                    if (data.newSprite != null)
                    {
                        data.image.sprite = data.newSprite;
                    }
                    data.image.raycastTarget = data.newRaycastTarget;
                    data.image.maskable = data.newMaskable;
                }
            }
        }

        public void SetAllRaycastTarget(bool value)
        {
            foreach (var data in imageReferences)
            {
                data.newRaycastTarget = value;
            }
        }

        public void SetAllMaskable(bool value)
        {
            foreach (var data in imageReferences)
            {
                data.newMaskable = value;
            }
        }

        public void SelectAll(bool value)
        {
            foreach (var data in imageReferences)
            {
                data.isSelected = value;
            }
        }

        public void ApplySelectedProperties()
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected && data.image != null)
                {
                    if (data.newSprite != null)
                    {
                        data.image.sprite = data.newSprite;
                    }
                    data.image.raycastTarget = data.newRaycastTarget;
                    data.image.maskable = data.newMaskable;
                }
            }
        }

        public void SetSelectedRaycastTarget(bool value)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newRaycastTarget = value;
                }
            }
        }

        public void SetSelectedMaskable(bool value)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newMaskable = value;
                }
            }
        }

        public void SetSelectedSprite(Sprite sprite)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newSprite = sprite;
                }
            }
        }

        public int GetSelectedCount()
        {
            int count = 0;
            foreach (var data in imageReferences)
            {
                if (data.isSelected) count++;
            }
            return count;
        }

        public void SetSelectedBoth(bool raycast, bool maskable)
        {
            foreach (var data in imageReferences)
            {
                if (data.isSelected)
                {
                    data.newRaycastTarget = raycast;
                    data.newMaskable = maskable;
                }
            }
        }

        public void SetAllBoth(bool raycast, bool maskable)
        {
            foreach (var data in imageReferences)
            {
                data.newRaycastTarget = raycast;
                data.newMaskable = maskable;
            }
        }
    }
}
