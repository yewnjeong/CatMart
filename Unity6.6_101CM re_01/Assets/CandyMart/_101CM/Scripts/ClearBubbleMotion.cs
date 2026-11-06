using UnityEngine;
using UnityEngine.UI;

// Figma 04 Clear: Union and Ellipse 4/5/6, two-second looping timeline.
// Split the existing bilingual texture at runtime; keep the original art untouched.
public sealed class ClearBubbleMotion : MonoBehaviour
{
    Image source;
    Sprite current;
    readonly RectTransform[] parts = new RectTransform[4];
    float started;

    void OnEnable()
    {
        started = Time.unscaledTime;
        Refresh();
        Animate(0f);
    }

    void Update()
    {
        Refresh();
        Animate((Time.unscaledTime - started) % 2f);
    }

    void Refresh()
    {
        if (!source) source = GetComponent<Image>();
        if (!source || !source.sprite || current == source.sprite) return;
        current = source.sprite;
        for (int i = 0; i < parts.Length; i++)
            if (parts[i]) { parts[i].gameObject.SetActive(false); Destroy(parts[i].gameObject); }
        bool english = current.name.Contains("EN");
        float w = english ? 1122f : 1016f;
        float h = english ? 474f : 458f;
        float dy = english ? 16f : 0f;
        parts[0] = Part("Body", new Vector2(601f + (english ? 50f : 0f), 200f + dy), w, h);
        // The left edge and the large dot overlap in X, so use two non-overlapping tiles.
        Tile(parts[0], new Rect(188f, 0f, 10f, 280f + dy), w, h);
        Tile(parts[0], new Rect(198f, 0f, w - 198f, h), w, h);
        parts[1] = Part("DotLarge", new Vector2(164f, 316f + dy), w, h);
        Tile(parts[1], new Rect(130f, 284f + dy, 68f, 72f), w, h);
        parts[2] = Part("DotMedium", new Vector2(91f, 377f + dy), w, h);
        Tile(parts[2], new Rect(68f, 356f + dy, 46f, 50f), w, h);
        parts[3] = Part("DotSmall", new Vector2(19f, 431f + dy), w, h);
        Tile(parts[3], new Rect(0f, 414f + dy, 38f, 44f), w, h);
        source.enabled = false;
    }

    RectTransform Part(string label, Vector2 center, float w, float h)
    {
        var go = new GameObject(label, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(center.x / w, 1f - center.y / h);
        rt.sizeDelta = new Vector2(w, h) * 0.5f;
        rt.anchoredPosition = new Vector2((center.x - w * 0.5f) * 0.5f, (h * 0.5f - center.y) * 0.5f);
        return rt;
    }

    void Tile(RectTransform parent, Rect pixels, float w, float h)
    {
        var go = new GameObject("Artwork", typeof(RectTransform), typeof(RawImage));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(pixels.x, -pixels.y) * 0.5f;
        rt.sizeDelta = pixels.size * 0.5f;
        var image = go.GetComponent<RawImage>();
        image.texture = current.texture;
        var r = current.textureRect;
        image.uvRect = new Rect(
            (r.x + pixels.x / w * r.width) / current.texture.width,
            (r.y + (1f - pixels.yMax / h) * r.height) / current.texture.height,
            pixels.width / w * r.width / current.texture.width,
            pixels.height / h * r.height / current.texture.height);
        image.raycastTarget = false;
    }

    void Animate(float t)
    {
        Scale(0, Mathf.Lerp(1f, 0.95f, Ease((t - 0.544f) / 0.5f)));
        Scale(1, Mathf.Lerp(1.2f, 1f, Ease((t - 0.044f) / 1.156f)));
        Scale(2, Mathf.Lerp(1f, 0.9f, Ease((t - 0.044f) / 1.156f)));
        Scale(3, Mathf.Lerp(1f, 0.9f, Ease((t - 0.044f) / 1.156f)));
    }

    void Scale(int index, float value)
    {
        if (parts[index]) parts[index].localScale = new Vector3(value, value, 1f);
    }

    // CSS/Figma ease-out: cubic-bezier(0, 0, 0.58, 1).
    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        float lo = 0f, hi = 1f;
        for (int i = 0; i < 20; i++)
        {
            float s = (lo + hi) * 0.5f;
            float x = 3f * (1f - s) * s * s * 0.58f + s * s * s;
            if (x < t) lo = s; else hi = s;
        }
        float u = (lo + hi) * 0.5f;
        return 3f * (1f - u) * u * u + u * u * u;
    }
}
