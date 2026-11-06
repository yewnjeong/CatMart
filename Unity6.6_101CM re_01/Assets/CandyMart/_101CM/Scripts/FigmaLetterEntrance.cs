using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Native font glyphs, separately outlined and animated; never moves a baked title as one image.</summary>
public class FigmaLetterEntrance : BaseMeshEffect
{
    public Color fillColor;
    public float outlineWidth = 16f;
    public Vector2 shadowOffset = new Vector2(28f, -30f);
    public bool softShadow;
    public float delay;
    public float letterDuration = 0.8f;
    public float distance = 40f;

    RectTransform rect;
    Vector2 rest;
    float elapsed;
    int enabledFrame;
    CanvasGroup visibility;
    bool initialized;
    readonly List<UIVertex> source = new List<UIVertex>();
    readonly UIVertex[] quad = new UIVertex[4];

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!initialized)
        {
            rect = (RectTransform)transform;
            rest = rect.anchoredPosition;
            initialized = true;
        }
        elapsed = 0f;
        enabledFrame = Time.frameCount;
        if (!visibility) visibility = GetComponent<CanvasGroup>();
        if (!visibility) visibility = gameObject.AddComponent<CanvasGroup>();
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
        ApplyPosition();
    }

    void Update()
    {
        // Font atlas construction/loading may stall the first frame. Do not skip the entrance.
        if (Time.frameCount != enabledFrame) elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        ApplyPosition();
    }

    void ApplyPosition()
    {
        if (!initialized) return;
        float localTime = elapsed - delay;
        float t = Mathf.Clamp01(localTime / Mathf.Max(0.001f, letterDuration));
        // An incoming text group is invisible until its own turn, rather than all
        // glyphs sitting visibly above the title throughout the stagger delay.
        if (visibility) visibility.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localTime / 0.12f));
        // CSS/Figma ease-in: cubic-bezier(0.42, 0, 1, 1). Solve the x curve first.
        float low = 0f, high = 1f;
        for (int i = 0; i < 16; i++)
        {
            float u = (low + high) * 0.5f;
            float x = 3f * (1f - u) * (1f - u) * u * 0.42f + 3f * (1f - u) * u * u + u * u * u;
            if (x < t) low = u; else high = u;
        }
        float v = (low + high) * 0.5f;
        float eased = t <= 0f ? 0f : t >= 1f ? 1f : 3f * (1f - v) * v * v + v * v * v;
        rect.anchoredPosition = rest + Vector2.up * (distance * (1f - eased));
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount < 4) return;
        source.Clear();
        for (int i = 0; i < 4; i++)
        {
            UIVertex vertex = default;
            vh.PopulateUIVertex(ref vertex, i);
            source.Add(vertex);
        }
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (var vertex in source)
        {
            minX = Mathf.Min(minX, vertex.position.x); maxX = Mathf.Max(maxX, vertex.position.x);
            minY = Mathf.Min(minY, vertex.position.y); maxY = Mathf.Max(maxY, vertex.position.y);
        }
        if (maxX - minX < 0.001f || maxY - minY < 0.001f) return;
        Rect bounds = ((RectTransform)transform).rect;
        for (int i = 0; i < source.Count; i++)
        {
            var vertex = source[i];
            vertex.position = new Vector3(
                Mathf.Lerp(bounds.xMin, bounds.xMax, (vertex.position.x - minX) / (maxX - minX)),
                Mathf.Lerp(bounds.yMin, bounds.yMax, (vertex.position.y - minY) / (maxY - minY)), 0f);
            source[i] = vertex;
        }
        vh.Clear();
        if (softShadow)
        {
            for (int ring = 3; ring >= 1; ring--)
                Ring(vh, shadowOffset + Vector2.down * 20f, outlineWidth + ring * 15f,
                    new Color(0.54f, 0.35f, 0.24f, 0.035f));
        }
        Color extrusion = new Color32(222, 101, 99, 255);
        // Short steps connect the cream outline to its colored extrusion.
        for (int i = 3; i >= 1; i--) Ring(vh, shadowOffset * (i / 3f), outlineWidth, extrusion);
        Ring(vh, Vector2.zero, outlineWidth, new Color32(255, 248, 238, 255));
        AddQuad(vh, Vector2.zero, fillColor);
    }

    void Ring(VertexHelper vh, Vector2 offset, float radius, Color tint)
    {
        const int steps = 24;
        for (int i = 0; i < steps; i++)
        {
            float angle = i * Mathf.PI * 2f / steps;
            AddQuad(vh, offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint);
        }
        AddQuad(vh, offset, tint);
    }

    void AddQuad(VertexHelper vh, Vector2 offset, Color tint)
    {
        for (int i = 0; i < 4; i++)
        {
            quad[i] = source[i];
            quad[i].position += (Vector3)offset;
            quad[i].color = tint;
        }
        vh.AddUIVertexQuad(quad);
    }

    public static void Build(Image baked, Font font, bool ending)
    {
        // Ink bounds measured from the source exports (2 pixels per design pixel).
        string letters = ending ? "CLEAR!" : "101CM";
        Rect[] ink = ending ? new[] {
            new Rect(225,138,361,429), new Rect(627,142,301,420),
            new Rect(962,142,330,419), new Rect(1306,142,420,425),
            new Rect(1752,142,334,421), new Rect(2112,139,114,423)
        } : new[] {
            new Rect(67,74,229,470), new Rect(366,68,359,483),
            new Rect(780,74,229,470), new Rect(1079,70,403,479), new Rect(1551,75,504,467)
        };
        Color pink = new Color32(255,179,199,255), yellow = new Color32(255,224,138,255);
        Color mint = new Color32(158,230,214,255), purple = new Color32(195,177,242,255);
        Color[] colors = ending ? new[] {pink, yellow, (Color)new Color32(255,194,209,255), mint, purple, yellow}
                                : new[] {pink, yellow, pink, mint, purple};
        var parent = baked.rectTransform;
        float imageWidth = ending ? 2460f : 2192f, imageHeight = ending ? 946f : 688f;
        Vector2 factor = new Vector2(parent.rect.width / imageWidth, parent.rect.height / imageHeight);
        // Total entrance is 1.2 seconds. Distribute the remaining time evenly across letters.
        const float totalDuration = 1.2f, singleDuration = 0.8f;
        for (int i = 0; i < letters.Length; i++)
        {
            var go = new GameObject("Letter_" + i + "_" + letters[i], typeof(RectTransform));
            go.SetActive(false);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            Rect r = ink[i];
            rt.sizeDelta = Vector2.Scale(r.size, factor);
            rt.anchoredPosition = new Vector2((r.center.x - imageWidth * 0.5f) * factor.x,
                (imageHeight * 0.5f - r.center.y) * factor.y);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = 335;
            text.text = letters[i].ToString();
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var motion = go.AddComponent<FigmaLetterEntrance>();
            motion.fillColor = colors[i];
            motion.outlineWidth = ending ? 21f : 32f;
            motion.shadowOffset = ending ? new Vector2(0f,-30f) : new Vector2(28f,-30f);
            motion.softShadow = ending;
            motion.delay = i * (totalDuration - singleDuration) / (letters.Length - 1);
            motion.letterDuration = singleDuration;
            go.SetActive(true);
        }
        baked.enabled = false;
    }
}
