using UnityEditor;
using UnityEngine;

/// <summary>
/// 메뉴 101CM > 자동 플레이 테스트 (본체: Scripts/AutoPlayTest.cs, 에디터 전용)
///  1. 처음부터 클리어까지: 음식마다 순간이동해 실제 Eat()으로 먹고, 박스 더미 → 100CM → 마지막 1CM → 출구 → 클리어 화면.
///  2. 장애물·재시작: 음식 닿는지(걸어서/점프로), 카트·빗자루, 흩어진 고양이 복구, Esc 화면들, 영어 글자, 다시 시작/홈, 콘솔 에러.
/// 결과는 콘솔 [AutoTest]와 AutoPlayTest.LastReport.
/// (에디터 창이 포커스를 잃으면 플레이 시간이 멈추므로 끝날 때까지 Unity 창을 앞에 둘 것)
/// </summary>
[InitializeOnLoad]
public static class AutoPlayTestMenu
{
    const string Pending = "101CM_AutoTestPending";   // 0 = 없음, 1 = 테스트 1, 2 = 테스트 2

    static AutoPlayTestMenu()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("101CM/자동 플레이 테스트")]
    static void Run1() => Run(1);

    [MenuItem("101CM/자동 테스트 2 (장애물·재시작)")]
    static void Run2() => Run(2);

    static void Run(int which)
    {
        if (EditorApplication.isPlaying) { Start(which); return; }
        EditorPrefs.SetInt(Pending, which);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayMode(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredPlayMode) return;
        int which = EditorPrefs.GetInt(Pending, 0);
        if (which == 0) return;
        EditorPrefs.SetInt(Pending, 0);
        Start(which);
    }

    static void Start(int which)
    {
        var go = new GameObject("AutoPlayTest");
        if (which == 2) go.AddComponent<AutoPlayTest2>();
        else go.AddComponent<AutoPlayTest>();
    }
}
