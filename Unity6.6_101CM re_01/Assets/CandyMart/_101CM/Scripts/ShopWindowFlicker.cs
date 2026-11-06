using System.Collections.Generic;
using UnityEngine;

/// <summary>Slow, independent shop-window shimmer. Emission always stays at 65–135%.</summary>
public sealed class ShopWindowFlicker : MonoBehaviour
{
    sealed class Window
    {
        public Renderer renderer;
        public int slot;
        public Color emission;
        public float phase, period;
        public MaterialPropertyBlock original, animated;
    }
    readonly List<Window> windows = new List<Window>();
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void OnEnable()
    {
        foreach (var renderer in FindObjectsByType<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (!material || !material.name.StartsWith("M_ShopLight_") || !material.HasProperty(EmissionId)) continue;
                var position = renderer.transform.position;
                float seed = position.x * 0.73f + position.y * 1.17f + position.z * 0.39f + i * 0.61f;
                var original = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original, i);
                windows.Add(new Window
                {
                    renderer = renderer, slot = i, emission = material.GetColor(EmissionId),
                    phase = seed, period = 2.2f + Mathf.Repeat(seed * 1.37f, 1.2f),
                    original = original, animated = new MaterialPropertyBlock()
                });
            }
        }
    }

    void Update()
    {
        foreach (var window in windows)
        {
            if (!window.renderer) continue;
            float t = Time.time * (2f * Mathf.PI / window.period) + window.phase;
            // Weighted sine waves remain between -1 and +1: no dark/off frames.
            float shimmer = 0.8f * Mathf.Sin(t) + 0.2f * Mathf.Sin(t * 1.7f + window.phase);
            Color color = window.emission * (1f + 0.35f * shimmer);
            color.a = window.emission.a;
            window.renderer.GetPropertyBlock(window.animated, window.slot);
            window.animated.SetColor(EmissionId, color);
            window.renderer.SetPropertyBlock(window.animated, window.slot);
        }
    }

    void OnDisable()
    {
        foreach (var window in windows)
            if (window.renderer) window.renderer.SetPropertyBlock(window.original, window.slot);
        windows.Clear();
    }
}
