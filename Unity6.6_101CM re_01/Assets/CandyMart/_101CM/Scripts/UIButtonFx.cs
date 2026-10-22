using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>버튼 눌림감: 마우스를 올리면 살짝 커지고, 누르면 살짝 작아진다.</summary>
public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.04f;
    public float pressScale = 0.96f;

    public System.Action onHover;   // 마우스가 올라갈 때

    bool hover, down;
    float current = 1f;

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        onHover?.Invoke();
    }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnPointerDown(PointerEventData e) => down = true;
    public void OnPointerUp(PointerEventData e) => down = false;

    void OnDisable()
    {
        hover = down = false;
        current = 1f;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        float target = down ? pressScale : hover ? hoverScale : 1f;
        current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * 1.5f);
        transform.localScale = new Vector3(current, current, 1f);
    }
}
