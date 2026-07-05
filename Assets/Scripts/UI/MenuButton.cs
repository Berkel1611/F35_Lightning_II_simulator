using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
public class MenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Image activeBar;
    [SerializeField] TextMeshProUGUI label;

    [Header("Kolory")]
    [SerializeField] Color normalColor = new Color(0.541f, 0.671f, 0.722f);
    [SerializeField] Color hoverColor = new Color(0.784f, 0.867f, 0.910f);
    [SerializeField] Color barColor = new Color(0.290f, 0.604f, 0.729f);

    void Start() => SetState(false);

    public void OnPointerEnter(PointerEventData e) => SetState(true);
    public void OnPointerExit(PointerEventData e) => SetState(false);
    public void OnPointerClick(PointerEventData e) => SetState(false);

    void SetState(bool hover)
    {
        if (label != null)
            label.color = hover ? hoverColor : normalColor;

        if (activeBar != null)
            activeBar.color = new Color(barColor.r, barColor.g, barColor.b,
                                        hover ? 1f : 0f);
    }
}