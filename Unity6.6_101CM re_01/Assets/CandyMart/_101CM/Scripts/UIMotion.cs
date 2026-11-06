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
    Vector2 scaleOffset = Vector2.zero;
    Vector2 scaleRange = Vector2.one;

    public static UIMotion Add(GameObject go, Mode mode, AnimationCurve curve, float duration = 2f, bool loop = true)
    {
        var m = go.AddComponent<UIMotion>();
        m.mode = mode;
        m.curve = curve;
        m.duration = duration;
        m.loop = loop;
        // Configure before applying the first frame; AddComponent calls OnEnable first.
        m.Init();
        m.startTime = Time.unscaledTime;
        m.Apply(0f);
        return m;
    }

    public static UIMotion AddEaseOutScale(GameObject go, Vector2 from, Vector2 to, float start, float end)
    {
        // Figma ease-out is cubic-bezier(0, 0, 0.58, 1).
        var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(start, 0f));
        const int samples = 12;
        float duration = end - start;
        for (int i = 1; i <= samples; i++)
        {
            float u = (float)i / samples;
            float x = u * u * (1.74f - 0.74f * u);
            float y = u * u * (3f - 2f * u);
            float slope = (6f - 6f * u) / ((3.48f - 2.22f * u) * duration);
            curve.AddKey(new Keyframe(start + duration * x, y, slope, slope));
        }
        var first = curve.keys[1];
        first.outTangent = 1f / (0.58f * duration);
        curve.MoveKey(1, first);

        var motion = Add(go, Mode.Scale, curve);
        motion.scaleOffset = from;
        motion.scaleRange = to - from;
        motion.Apply(0f);
        return motion;
    }

    /// <summary>키 (시간초, 값) 쌍으로 부드러운 커브 만들기.</summary>
    public static AnimationCurve Keys(params float[] tv)
    {
        var c = new AnimationCurve();
        for (int i = 0; i + 1 < tv.Length; i += 2) c.AddKey(tv[i], tv[i + 1]);
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    // Add() initializes after configuration; serialized components initialize on enable.
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
        float safeDuration = Mathf.Max(0.0001f, duration);
        if (loop) t %= safeDuration;
        else t = Mathf.Min(t, safeDuration);
        Apply(t);
    }

    void Apply(float t)
    {
        switch (mode)
        {
            case Mode.Scale:
                Vector2 scale = scaleOffset + scaleRange * curve.Evaluate(t);
                rt.localScale = new Vector3(scale.x, scale.y, 1f);
                break;
            case Mode.Alpha:
                group.alpha = curve.Evaluate(t);
                break;
            case Mode.DropIn:
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out
                rt.anchoredPosition = basePos + new Vector2(0f, 40f * (1f - e));
                group.alpha = Mathf.Clamp01(k * 2.5f);
                break;
        }
    }
}
