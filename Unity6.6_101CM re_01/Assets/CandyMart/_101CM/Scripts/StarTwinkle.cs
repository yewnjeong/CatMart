using UnityEngine;

/// <summary>
/// 별 가로등 반짝임. 별 부분(재질의 Emission 마스크)이 숨 쉬듯 밝아졌다 어두워지고, 가끔 반짝 빛난다.
/// 같은 재질을 여러 가로등이 쓰므로 MaterialPropertyBlock으로 가로등마다 따로 움직인다.
/// </summary>
public class StarTwinkle : MonoBehaviour
{
    public Renderer star;
    public Light lamp;
    public Color glow = new Color(1f, 0.82f, 0.45f);
    public float minGlow = 0.15f;
    public float maxGlow = 1.1f;
    public float breathSpeed = 0.35f;
    public float twinkleSpeed = 0.9f;
    [Range(0f, 1f)] public float phase;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock mpb;
    float baseIntensity;

    void Start()
    {
        mpb = new MaterialPropertyBlock();
        if (lamp) baseIntensity = lamp.intensity;
    }

    void Update()
    {
        float t = Time.time + phase * 10f;
        float breath = 0.5f + 0.5f * Mathf.Sin(t * breathSpeed * Mathf.PI * 2f);
        // 뾰족한 펄스: 사인 위상을 흔들어 반짝이는 간격이 일정하지 않게
        float twinkle = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * twinkleSpeed * Mathf.PI * 2f + Mathf.Sin(t * 1.7f) * 2f)), 12f);
        float k = Mathf.Lerp(minGlow, maxGlow, Mathf.Clamp01(breath * 0.55f + twinkle * 0.6f));

        if (star)
        {
            star.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, glow * k);
            star.SetPropertyBlock(mpb);
        }
        if (lamp) lamp.intensity = baseIntensity * (0.75f + 0.45f * k);
    }
}
