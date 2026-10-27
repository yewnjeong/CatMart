using UnityEngine;

/// <summary>
/// Figma(101CM Game UI)에서 내보낸 UI 이미지·폰트 묶음. GameFlowUI가 이름으로 찾아 쓴다.
/// 이미지는 Assets/Textures/UI, 폰트는 Assets/Art/UI/Fonts. 실행 시 Resources/FigmaUISkin에서 읽는다.
/// </summary>
[CreateAssetMenu(menuName = "101CM/Figma UI Skin")]
public class FigmaUISkin : ScriptableObject
{
    public Font titleFont;     // Fredoka Bold      (CM 숫자)
    public Font enFont;        // Fredoka SemiBold  (영어 안내 문구)
    public Font bodyFont;      // Jua               (한글 안내 문구)
    public Sprite[] sprites;

    public Sprite Get(string spriteName)
    {
        foreach (var s in sprites)
            if (s && s.name == spriteName) return s;
        Debug.LogWarning($"[FigmaUISkin] 스프라이트 없음: {spriteName}");
        return null;
    }
}
