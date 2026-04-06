using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Selectable selectable;
    [SerializeField] private List<ClickEffectImage> effectImages;
    private void Reset()
    {
        QuickSetup();
    }

    private bool IsInteractable = true;

    private void Awake()
    {
        if (selectable) IsInteractable = selectable.interactable;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!selectable.interactable || !IsInteractable) return;
        foreach (var clickEffectImage in effectImages)
        {
            clickEffectImage.PressDown();
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!selectable.interactable || !IsInteractable) return;
        foreach (var clickEffectImage in effectImages)
        {
            clickEffectImage.PressUp();
        }
    }


    public void SetInteractable(bool interacable)
    {
        IsInteractable = interacable;
        foreach (var clickEffectImage in effectImages)
        {
            clickEffectImage.SetInteractable(interacable);
        }
    }

    [Button]
    public void QuickSetup()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }
        if (effectImages == null)
        {
            effectImages = new List<ClickEffectImage>();
        }
        var imageComponents = GetComponentsInChildren<Image>();
        if (selectable)
        {
            foreach (var image in imageComponents)
            {
                var clickEffectImage = new ClickEffectImage
                {
                    Image = image,
                    NormalColor = selectable.colors.normalColor,
                    PressColor = selectable.colors.pressedColor,
                };
                effectImages.Add(clickEffectImage);
            }
        }
    }
}

[Serializable]
public class ClickEffectImage
{
    public Graphic Image;
    public Color NormalColor = new(255, 255, 255, 1);
    public Color PressColor = new(255, 255, 255, 1);

    public void PressDown()
    {
        Image.color = PressColor;
    }
    public void PressUp()
    {
        Image.color = NormalColor;
    }

    public void SetInteractable(bool isInteractable)
    {
        Image.color = isInteractable ? NormalColor : PressColor;
    }
}

[Serializable]
public class ColorEffectBlock
{
    public Color NormalColor = new(255, 255, 255, 1);
    public Color PressColor = new(255, 255, 255, 1);
}

