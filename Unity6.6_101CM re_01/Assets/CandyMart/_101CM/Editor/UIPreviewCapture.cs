using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 메뉴 101CM > UI 미리보기 캡처: GameFlowUI.EditorPreview로 화면을 하나씩 켜서 1920x1080 PNG로 저장 (Figma 시안과 비교용).
/// 결과: 프로젝트 폴더/Logs/UICapture/*.png. 플레이 없이 동작하고 씬은 바꾸지 않는다.
/// </summary>
public static class UIPreviewCapture
{
    static readonly (string file, string screen, int page, float progress)[] Shots =
    {
        ("01-1_hud_0", "hud", 0, 0f),
        ("01-2_hud_mid", "hud", 0, 0.5f),
        ("01-3_hud_full", "hud", 0, 1f),
        ("02-1_title", "title", 0, 0f),
        ("02-1_guide_title_settings", "guide-title", 0, 0f),
        ("02-2_guide_title_controls", "guide-title", 1, 0f),
        ("02-3_guide_title_rules", "guide-title", 2, 0f),
        ("02-4_guide_title_goal", "guide-title", 3, 0f),
        ("03-1_menu", "menu", 0, 0.5f),
        ("03-1_guide_settings", "guide", 0, 0.5f),
        ("03-2_guide_controls", "guide", 1, 0.5f),
        ("03-3_guide_rules", "guide", 2, 0.5f),
        ("03-4_guide_goal", "guide", 3, 0.5f),
        ("04_clear", "clear", 0, 1f),
    };

    [MenuItem("101CM/UI 미리보기 캡처")]
    public static void CaptureAll()
    {
        string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "UICapture");
        Directory.CreateDirectory(dir);
        var camGo = new GameObject("UICaptureCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = camGo.AddComponent<Camera>();
        var main = Camera.main;
        if (main) { cam.CopyFrom(main); }
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        foreach (var en in new[] { false, true })
        {
            foreach (var s in Shots)
            {
                var host = new GameObject("UICaptureHost") { hideFlags = HideFlags.HideAndDontSave };
                var ui = host.AddComponent<GameFlowUI>();
                var canvas = ui.EditorPreview(s.screen, s.page, s.progress, en);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, (en ? "EN_" : "KO_") + s.file + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(host);
            }
        }
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(camGo);
        Debug.Log("[UICapture] saved to " + dir);
    }
}
