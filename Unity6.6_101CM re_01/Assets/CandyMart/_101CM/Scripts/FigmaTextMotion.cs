using UnityEngine;

/// <summary>
/// Figma 텍스트 애니메이션(CHARACTER 단위, LEFT_RIGHT, direction "in", position: top 40px)을 그대로 옮김.
/// 글자 i는 i * stagger 초에 시작해 groupDuration 동안 위 distance px에서 제자리로 내려온다 (cubic-bezier 이징).
/// 타임라인 길이(timeline)마다 반복 (Figma timelineCohorts loopMode: loop). Esc로 timeScale=0이어도 움직이도록 unscaled 시간.
/// </summary>
public class FigmaTextMotion : MonoBehaviour
{
    public float delay;
    public float groupDuration = 0.4f;
    public float distance = 40f;
    public float timeline = 2f;
    public Vector4 bezier = new Vector4(0.42f, 0f, 1f, 1f);   // x1, y1, x2, y2

    RectTransform rt;
    Vector2 rest;
    float startTime;
    bool ready;

    public static FigmaTextMotion Add(GameObject go, float delay, float groupDuration, Vector4 bezier, float distance = 40f, float timeline = 2f)
    {
        var m = go.AddComponent<FigmaTextMotion>();
        m.delay = delay;
        m.groupDuration = groupDuration;
        m.bezier = bezier;
        m.distance = distance;
        m.timeline = timeline;
        return m;
    }

    void OnEnable()
    {
        startTime = Time.unscaledTime;
        if (ready) Apply(0f);
    }

    void Update()
    {
        if (!ready)
        {
            rt = (RectTransform)transform;
            rest = rt.anchoredPosition;
            ready = true;
        }
        Apply((Time.unscaledTime - startTime) % timeline);
    }

    void Apply(float t)
    {
        float u = Mathf.Clamp01((t - delay) / Mathf.Max(0.001f, groupDuration));
        rt.anchoredPosition = rest + Vector2.up * (distance * (1f - Ease(u)));
    }

    /// <summary>CSS cubic-bezier(x1, y1, x2, y2): x(s)=u 를 이분법으로 풀고 y(s) 반환. y2 &gt; 1이면 살짝 지나쳤다 돌아옴.</summary>
    float Ease(float u)
    {
        if (u <= 0f) return 0f;
        if (u >= 1f) return 1f;
        float lo = 0f, hi = 1f, s = u;
        for (int i = 0; i < 20; i++)
        {
            s = (lo + hi) * 0.5f;
            if (Bez(s, bezier.x, bezier.z) < u) lo = s; else hi = s;
        }
        return Bez((lo + hi) * 0.5f, bezier.y, bezier.w);
    }

    static float Bez(float s, float p1, float p2)
    {
        float r = 1f - s;
        return 3f * r * r * s * p1 + 3f * r * s * s * p2 + s * s * s;
    }
}
