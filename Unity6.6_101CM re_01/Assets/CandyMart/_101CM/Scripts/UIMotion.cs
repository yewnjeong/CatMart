using UnityEngine;

/// <summary>
/// Figma 프로토타입 모션을 옮긴 간단한 UI 애니메이션 (Esc 메뉴에서 timeScale=0이어도 움직이도록 unscaled 시간 사용).
/// Scale  : curve 값을 localScale로 (반복)
/// Alpha  : curve 값을 CanvasGroup.alpha로 (반복)
/// DropIn : 위에서 40px 떨어지며 나타남 (켜질 때 한 번)
/// </summary>
public class UIMotion : MonoBehaviour
{
    public enum Mode { Scale, Alpha, DropIn }

    public Mode mode;
    public AnimationCurve curve;
    public float duration = 2f;
    public bool loop = true;

    RectTransform rt;
    CanvasGroup group;
    Vector2 basePos;
    float startTime;

    public static UIMotion Add(GameObject go, Mode mode, AnimationCurve curve, float duration = 2f, bool loop = true)
    {
        var m = go.AddComponent<UIMotion>();
        m.mode = mode;
        m.curve = curve;
        m.duration = duration;
        m.loop = loop;
        return m;
    }

    /// <summary>키 (시간초, 값) 쌍으로 부드러운 커브 만들기.</summary>
    public static AnimationCurve Keys(params float[] tv)
    {
        var c = new AnimationCurve();
        for (int i = 0; i + 1 < tv.Length; i += 2) c.AddKey(tv[i], tv[i + 1]);
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    // AddComponent 직후 OnEnable이 먼저 불리므로(설정값 넣기 전) 초기화는 첫 Update에서 한다.
    bool ready;

    void Init()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
        if (mode != Mode.Scale)
        {
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
        }
        ready = true;
    }

    void OnEnable()
    {
        startTime = Time.unscaledTime;
        if (ready) Apply(0f);
    }

    void Update()
    {
        if (!ready) Init();
        float t = Time.unscaledTime - startTime;
        if (loop) t %= duration;
        else t = Mathf.Min(t, duration);
        Apply(t);
    }

    void Apply(float t)
    {
        switch (mode)
        {
            case Mode.Scale:
                float s = curve.Evaluate(t);
                rt.localScale = new Vector3(s, s, 1f);
                break;
            case Mode.Alpha:
                group.alpha = curve.Evaluate(t);
                break;
            case Mode.DropIn:
                float k = Mathf.Clamp01(t / duration);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out
                rt.anchoredPosition = basePos + new Vector2(0f, 40f * (1f - e));
                group.alpha = Mathf.Clamp01(k * 2.5f);
                break;
        }
    }
}
