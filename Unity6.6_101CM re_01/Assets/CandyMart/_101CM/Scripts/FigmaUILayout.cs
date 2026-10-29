using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자동 생성 (Figma 101CM Game UI → 스프라이트 굽기 스크립트). 손으로 고치지 말 것.
/// 각 스프라이트가 Figma 1920x1080 프레임에서 차지하는 영역(왼쪽 위 x, y, 폭, 높이 px). 그림자 포함.
/// </summary>
public static class FigmaUILayout
{
    public static readonly Dictionary<string, Rect> Rects = new Dictionary<string, Rect>
    {
        ["T_Logo"] = new Rect(634.0f, 211.0f, 1096.0f, 344.0f),
        ["T_LineA"] = new Rect(1652.0f, 109.0f, 131.0f, 127.0f),
        ["T_LineB"] = new Rect(1678.0f, 175.0f, 105.0f, 72.0f),
        ["T_StarA"] = new Rect(634.0f, 116.0f, 153.0f, 156.0f),
        ["T_StarB"] = new Rect(604.0f, 210.0f, 102.0f, 104.0f),
        ["T_Cat"] = new Rect(252.0f, 551.0f, 440.0f, 453.0f),
        ["T_BtnStart"] = new Rect(853.0f, 699.0f, 650.0f, 153.0f),
        ["T_HowTo"] = new Rect(963.0f, 874.0f, 433.0f, 50.0f),
        ["T_LangKOR"] = new Rect(1634.0f, 969.0f, 245.0f, 80.0f),
        ["T_LangENG"] = new Rect(1634.0f, 969.0f, 245.0f, 80.0f),
        ["H_GaugeBG"] = new Rect(600.0f, 103.0f, 721.0f, 76.0f),
        ["H_GaugeFill"] = new Rect(600.0f, 103.0f, 721.0f, 76.0f),
        ["H_GaugeBorder"] = new Rect(590.0f, 93.0f, 741.0f, 96.0f),
        ["H_Ears"] = new Rect(827.0f, 40.0f, 272.0f, 60.0f),
        ["M_Panel"] = new Rect(567.0f, 169.0f, 787.0f, 810.0f),
        ["M_BtnResumeOn"] = new Rect(685.0f, 423.0f, 551.0f, 105.0f),
        ["M_BtnResumeOff"] = new Rect(686.0f, 424.0f, 549.0f, 103.0f),
        ["M_BtnRetryOn"] = new Rect(685.0f, 509.0f, 551.0f, 105.0f),
        ["M_BtnRetryOff"] = new Rect(686.0f, 510.0f, 549.0f, 103.0f),
        ["M_BtnHowToOn"] = new Rect(685.0f, 595.0f, 551.0f, 105.0f),
        ["M_BtnHowToOff"] = new Rect(686.0f, 596.0f, 549.0f, 103.0f),
        ["M_BtnHomeOn"] = new Rect(685.0f, 681.0f, 551.0f, 105.0f),
        ["M_BtnHomeOff"] = new Rect(686.0f, 682.0f, 549.0f, 103.0f),
        ["G_Panel"] = new Rect(567.0f, 169.0f, 787.0f, 810.0f),
        ["G_Close"] = new Rect(1213.0f, 294.0f, 59.0f, 63.0f),
        ["G_Prev"] = new Rect(840.0f, 799.0f, 59.0f, 64.0f),
        ["G_Next"] = new Rect(1022.0f, 799.0f, 59.0f, 64.0f),
        ["G_DotOn"] = new Rect(918.0f, 820.0f, 37.0f, 18.0f),
        ["G_DotOff"] = new Rect(918.0f, 820.0f, 17.0f, 18.0f),
        ["G_Illust1"] = new Rect(709.0f, 475.0f, 503.0f, 174.0f),
        ["G_Keys"] = new Rect(755.0f, 507.0f, 411.0f, 191.0f),
        ["G_Rules"] = new Rect(690.0f, 483.0f, 541.0f, 246.0f),
        ["C_Cats"] = new Rect(338.0f, 574.0f, 734.0f, 427.0f),
        ["C_Title"] = new Rect(309.0f, 64.0f, 1230.0f, 473.0f),
        ["C_StarA"] = new Rect(391.0f, 309.0f, 103.0f, 103.0f),
        ["C_StarB"] = new Rect(361.0f, 266.0f, 83.0f, 81.0f),
        ["C_StarC"] = new Rect(1478.0f, 56.0f, 106.0f, 103.0f),
        ["C_StarD"] = new Rect(1480.0f, 137.0f, 73.0f, 73.0f),
        ["C_SparkA"] = new Rect(338.0f, 350.0f, 32.0f, 45.0f),
        ["C_SparkB"] = new Rect(323.0f, 340.0f, 41.0f, 30.0f),
        ["C_SparkC"] = new Rect(1571.0f, 124.0f, 41.0f, 22.0f),
        ["C_SparkD"] = new Rect(1518.0f, 175.0f, 36.0f, 27.0f),
        ["C_BubbleKO"] = new Rect(1063.0f, 452.0f, 508.0f, 229.0f),
        ["C_BubbleEN"] = new Rect(1063.0f, 444.0f, 561.0f, 237.0f),
        ["C_BtnRetry"] = new Rect(1146.0f, 743.0f, 472.0f, 133.0f),
        ["C_BtnHome"] = new Rect(1147.0f, 864.0f, 470.0f, 131.0f),
        ["M_BtnSettingsOn"] = new Rect(685.0f, 767.0f, 551.0f, 105.0f),
        ["M_BtnSettingsOff"] = new Rect(686.0f, 768.0f, 549.0f, 103.0f),
        ["S_Panel"] = new Rect(567.0f, 169.0f, 787.0f, 810.0f),
        ["S_Track"] = new Rect(842.0f, 536.0f, 281.0f, 24.0f),
        ["S_Fill"] = new Rect(842.0f, 536.0f, 281.0f, 24.0f),
        ["S_Knob"] = new Rect(961.0f, 526.0f, 43.0f, 48.0f),
        ["S_LangKOR"] = new Rect(837.0f, 688.0f, 245.0f, 80.0f),
        ["S_LangENG"] = new Rect(837.0f, 688.0f, 245.0f, 80.0f),
    };
}
