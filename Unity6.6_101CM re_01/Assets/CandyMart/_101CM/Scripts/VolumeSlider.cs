using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 설정 화면 볼륨 슬라이더 (Figma Slider). 트랙 위를 누르거나 끌면 값(0~1)이 바뀐다.
/// 채움 막대는 마스크 폭으로 자르고, 손잡이는 채움 끝(값 위치)에 놓인다. GameFlowUI가 만든다.
/// </summary>
public class VolumeSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform track;      // 클릭 영역 겸 기준 (트랙 이미지 폭 = 값 0~1 범위)
    public RectTransform fillMask;   // 왼쪽 기준, 폭 = 값 x 트랙 폭
    public RectTransform knob;
    public System.Action<float> onChanged;
    public System.Action onReleased;

    public float Value { get; private set; }

    public void SetValue(float v, bool notify = false)
    {
        Value = Mathf.Clamp01(v);
        float w = track.rect.width;
        fillMask.sizeDelta = new Vector2(w * Value, fillMask.sizeDelta.y);
        knob.anchoredPosition = new Vector2(track.anchoredPosition.x - w * 0.5f + w * Value, knob.anchoredPosition.y);
        if (notify) onChanged?.Invoke(Value);
    }

    public void OnPointerDown(PointerEventData e) => Drag(e);
    public void OnDrag(PointerEventData e) => Drag(e);
    public void OnPointerUp(PointerEventData e) => onReleased?.Invoke();

    void Drag(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var p)) return;
        var r = track.rect;
        SetValue((p.x - r.xMin) / r.width, true);
    }
}
