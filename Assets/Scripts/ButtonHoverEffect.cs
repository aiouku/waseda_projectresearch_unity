using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        image.sprite = normalSprite;
        image.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData e) => Apply(hoverSprite, hoverColor);
    public void OnPointerExit(PointerEventData e)  => Apply(normalSprite, normalColor);
    public void OnPointerDown(PointerEventData e)  => Apply(hoverSprite, hoverColor * 0.85f);
    public void OnPointerUp(PointerEventData e)    => Apply(hoverSprite, hoverColor);

    void Apply(Sprite sprite, Color color)
    {
        image.sprite = sprite;
        image.color  = color;
    }
}
