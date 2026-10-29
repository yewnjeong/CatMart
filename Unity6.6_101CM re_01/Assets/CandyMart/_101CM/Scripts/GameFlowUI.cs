using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Figma 시안(101CM Game UI · UI SunSet KO/EN)대로 만든 Canvas UI 전체.
/// 시작 화면(02-1) · 게임 설명 1~3(02-2~4 / 03-2~4) · 진행 HUD(01-x) · Esc 메뉴(03-1) · 클리어(04).
/// 이미지 위치는 FigmaUILayout(Figma 1920x1080 프레임 기준 왼쪽 위 x, y, 폭, 높이). 이미지·폰트는 Resources/FigmaUISkin.
/// 글자 중 언어에 따라 바뀌는 것만 Text로 두고, 시작 화면의 KOR/ENG 토글로 바꾼다 (PlayerPrefs에 저장).
/// GameManager가 씬에서 못 찾으면 실행 시 자동으로 붙인다.
/// </summary>
public class GameFlowUI : MonoBehaviour
{
    public static GameFlowUI I { get; private set; }

    const string LangKey = "101CM_Lang";
    /// <summary>true면 영어(EN 페이지), false면 한국어(KO 페이지).</summary>
    public static bool English
    {
        get => PlayerPrefs.GetInt(LangKey, 0) == 1;
        set { PlayerPrefs.SetInt(LangKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public FigmaUISkin skin;              // 비우면 Resources/FigmaUISkin
    public float endingDelay = 1.2f;      // 다리 끝 도착 → 클리어 화면까지 (축하 모션 보여줄 시간)

    [Header("진행 HUD")]
    // 씬에 예전 배율 값(0.5, 1436px 시안 기준)이 저장돼 있어 필드 이름을 바꿈 — 아래 기본값부터 시작
    [Tooltip("Figma 시안 크기(718px 게이지) 대비 배율. 1 = 시안 그대로")]
    public float hudSize = 1.15f;
    [Tooltip("화면 위 끝에서 고양이 귀 끝까지 거리, px (1920x1080 기준). 배율과 상관없이 유지")]
    public float hudTopMargin = 16f;

    const float EarTipY = 41f;   // 시안에서 HUD 귀 끝의 y (H_Ears)

    static readonly Color Brown = new Color32(0x8A, 0x5A, 0x4E, 0xFF);
    static readonly Color DarkBrown = new Color32(0x6E, 0x4A, 0x40, 0xFF);
    static readonly Color Coral = new Color32(0xF2, 0x73, 0x7E, 0xFF);
    // Dim: rgba(255,196,192) — Esc 위(03): 0.6 x 0.94 / 시작 화면 위(02): 0.3 x 0.94
    static readonly Color DimPlay = new Color(1f, 0.769f, 0.753f, 0.564f);
    static readonly Color DimTitle = new Color(1f, 0.769f, 0.753f, 0.282f);
    // 시작 화면 배경은 0.75, 게임 설명(02-2~4) 배경은 0.55 불투명 → 흰색 0.27을 덮어 맞춤
    static readonly Color TitleFade = new Color(1f, 1f, 1f, 0.267f);

    GameObject titleRoot, hudRoot, menuRoot, guideRoot, clearRoot;
    RectTransform gaugeMask;
    Text cmText, catText, clearTime;
    float shownProgress;
    bool endingShown;

    // 언어별 글자
    struct LocText { public Text text; public string ko, en; public bool enFredoka; }
    readonly List<LocText> locs = new List<LocText>();
    Image langToggle, clearBubble;

    // 게임 설명
    Image guideDim, guideFade;
    Text guideHeader;
    readonly GameObject[] pages = new GameObject[3];
    CanvasGroup prevGroup, nextGroup;
    Button prevButton, nextButton;
    readonly Image[] dots = new Image[3];
    int page;

    void Awake() => I = this;

    void Start()
    {
        if (!skin) skin = Resources.Load<FigmaUISkin>("FigmaUISkin");
        if (!skin)
        {
            Debug.LogError("[GameFlowUI] Resources/FigmaUISkin 이 없습니다.");
            return;
        }
        EnsureEventSystem();
        BuildCanvas();
        var gm = GameManager.I;
        titleRoot.SetActive(gm && gm.OnTitle);
    }

    void Update()
    {
        var gm = GameManager.I;
        if (!gm || !hudRoot) return;

        if (gm.IsWon && !endingShown)
        {
            endingShown = true;
            StartCoroutine(ShowEnding());
        }

        hudRoot.SetActive(!gm.OnTitle && !gm.IsWon);
        bool menuOn = gm.MenuOpen && !guideRoot.activeSelf;
        if (gm.MenuOpen && !lastMenuOpen) SetMenuFocus(0);   // Esc로 새로 열 때만 RESUME부터 (게임 설명에서 돌아올 땐 유지)
        lastMenuOpen = gm.MenuOpen;
        menuRoot.SetActive(menuOn);
        if (hudRoot.activeSelf) UpdateHud(gm);
    }

    void UpdateHud(GameManager gm)
    {
        shownProgress = Mathf.MoveTowards(shownProgress, gm.Progress, Time.unscaledDeltaTime * 0.8f);
        SetGauge(shownProgress, gm.CM, gm.player ? gm.player.FollowerCount : 0);
    }

    void SetGauge(float progress, int cm, int cats)
    {
        gaugeMask.sizeDelta = new Vector2(718f * progress, gaugeMask.sizeDelta.y);
        cmText.text = $"{cm} / {GameManager.Goal} CM";
        catText.text = English ? $"Cats following: {cats}" : $"따라오는 고양이 {cats}마리";
    }

    // ---------------- 버튼 동작 ----------------
    void OnStart()
    {
        titleRoot.SetActive(false);
        if (GameManager.I) GameManager.I.BeginGame();
    }

    void OnResume() { if (GameManager.I) GameManager.I.SetMenu(false); }
    void OnRetry() { if (GameManager.I) GameManager.I.Restart(false); }
    void OnHome() { if (GameManager.I) GameManager.I.Restart(true); }

    void OpenGuide()
    {
        // 시작 화면 위(02-2~4)와 게임 중 Esc 위(03-2~4)는 뒤 배경 처리가 다름
        bool overTitle = titleRoot.activeSelf;
        guideFade.gameObject.SetActive(overTitle);
        guideDim.color = overTitle ? DimTitle : DimPlay;
        ShowPage(0);
        guideRoot.SetActive(true);
    }

    /// <summary>Esc·닫기 버튼·바깥 클릭. 게임 설명이 열려 있었으면 닫고 true.</summary>
    public bool CloseGuide()
    {
        if (!guideRoot || !guideRoot.activeSelf) return false;
        guideRoot.SetActive(false);
        return true;
    }

    void ShowPage(int p)
    {
        page = Mathf.Clamp(p, 0, 2);
        for (int i = 0; i < 3; i++) pages[i].SetActive(i == page);
        string[] ko = { "목표", "조작 방법", "게임 규칙" };
        string[] en = { "Goal", "Controls", "Rules" };
        guideHeader.text = English ? en[page] : ko[page];
        guideHeader.font = English ? skin.enFont : skin.bodyFont;
        // 점: 현재 페이지만 34px 알약, 나머지 14px 원 (간격 10). 스프라이트는 그림자 여백만큼 1~3px 큼
        float x = 919f;
        for (int i = 0; i < 3; i++)
        {
            bool on = i == page;
            float w = on ? 34f : 14f;
            dots[i].sprite = skin.Get(on ? "G_DotOn" : "G_DotOff");
            Place(dots[i].rectTransform, x - 1f, 820f, w + 3f, 18f);
            x += w + 10f;
        }
        SetArrow(prevButton, prevGroup, page > 0);
        SetArrow(nextButton, nextGroup, page < 2);
    }

    static void SetArrow(Button b, CanvasGroup g, bool on)
    {
        b.interactable = on;
        g.alpha = on ? 1f : 0.4f;
    }

    void SetLanguage(bool english)
    {
        English = english;
        ApplyLanguage();
    }

    void ApplyLanguage()
    {
        bool en = English;
        foreach (var l in locs)
        {
            l.text.text = en ? l.en : l.ko;
            l.text.font = en && l.enFredoka ? skin.enFont : skin.bodyFont;
        }
        langToggle.sprite = skin.Get(en ? "T_LangENG" : "T_LangKOR");
        // 클리어 말풍선: EN은 글자가 길어 더 넓은 풍선, 글자 중심도 오른쪽으로 25px
        clearBubble.sprite = skin.Get(en ? "C_BubbleEN" : "C_BubbleKO");
        PlaceSprite(clearBubble.rectTransform, en ? "C_BubbleEN" : "C_BubbleKO");
        float cx = en ? 1388.5f : 1363.5f;
        foreach (var l in locs)
            if (l.text.name.StartsWith("ClearLabel"))
                PlaceCenter(l.text.rectTransform, cx, en ? 536f : 536.5f, 500f, 70f);
        PlaceCenter(clearTime.rectTransform, cx, en ? 593f : 593.5f, 300f, 60f);
        if (guideRoot && guideRoot.activeSelf) ShowPage(page);
        var gm = GameManager.I;
        catText.font = en ? skin.enFont : skin.bodyFont;
        if (catText) catText.text = en ? $"Cats following: {(gm && gm.player ? gm.player.FollowerCount : 0)}"
                                       : $"따라오는 고양이 {(gm && gm.player ? gm.player.FollowerCount : 0)}마리";
    }

    IEnumerator ShowEnding()
    {
        yield return new WaitForSecondsRealtime(endingDelay);
        int sec = GameManager.I ? Mathf.FloorToInt(GameManager.I.RecordTime) : 0;
        clearTime.text = $"{sec / 60:00}:{sec % 60:00}";
        clearRoot.SetActive(true);
        CameraFollow.SetCursorLocked(false);
    }

    // ---------------- 화면 만들기 ----------------
    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>()) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void BuildCanvas()
    {
        var cgo = new GameObject("FlowCanvas");
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // 1920x1080 시안 영역이 항상 화면 안에 들어오게
        cgo.AddComponent<GraphicRaycaster>();
        var root = cgo.transform;

        BuildHud(root);
        BuildMenu(root);
        BuildTitle(root);
        BuildClear(root);
        BuildGuide(root);   // 시작 화면·메뉴 위에 뜨므로 마지막
        ApplyLanguage();
    }

    // ===== 01 진행 HUD =====
    void BuildHud(Transform root)
    {
        // 시안 프레임(1920x1080)을 화면 위쪽 가운데에 붙인다 — 게이지가 항상 화면 맨 위에 오도록.
        hudRoot = new GameObject("HUD", typeof(RectTransform));
        var hrt = (RectTransform)hudRoot.transform;
        hrt.SetParent(root, false);
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(1920f, 1080f);
        hrt.anchoredPosition = new Vector2(0f, EarTipY * hudSize - hudTopMargin);   // 귀 끝이 화면 위에서 hudTopMargin 떨어지게
        hrt.localScale = new Vector3(hudSize, hudSize, 1f);

        Spr("GaugeBG", hrt, "H_GaugeBG");
        var maskGo = new GameObject("GaugeFillMask", typeof(RectTransform), typeof(RectMask2D));
        gaugeMask = (RectTransform)maskGo.transform;
        gaugeMask.SetParent(hrt, false);
        gaugeMask.anchorMin = gaugeMask.anchorMax = gaugeMask.pivot = new Vector2(0f, 1f);
        gaugeMask.anchoredPosition = new Vector2(601f - 960f, 540f - 104f);
        gaugeMask.sizeDelta = new Vector2(0f, 73f);
        var fill = Spr("GaugeFill", gaugeMask, "H_GaugeFill");
        var fr = FigmaUILayout.Rects["H_GaugeFill"];
        var frt = fill.rectTransform;
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0f, 1f);
        frt.anchoredPosition = new Vector2(fr.x - 601f, -(fr.y - 104f));
        Spr("GaugeBorder", hrt, "H_GaugeBorder");
        Spr("CatEars", hrt, "H_Ears");

        cmText = Label("CM", hrt, skin.titleFont, 50, Brown, 960f, 140.5f, 718f, 73f);
        catText = Label("Cats", hrt, skin.bodyFont, 32, Brown, 960.5f, 210f, 700f, 44f);
        hudRoot.SetActive(false);
    }

    // ===== 03-1 Esc 메뉴 =====
    void BuildMenu(Transform root)
    {
        menuRoot = FullScreen("Menu", root);
        Dim(menuRoot.transform, null).color = DimPlay;
        var d = Design("Design", menuRoot.transform);
        Spr("Panel", d, "M_Panel");
        MenuButton(0, d, "Resume", OnResume);
        MenuButton(1, d, "Retry", OnRetry);
        MenuButton(2, d, "HowTo", OpenGuide);
        MenuButton(3, d, "Home", OnHome);
        SetMenuFocus(0);
        menuRoot.SetActive(false);
    }

    // 메뉴 버튼: 마우스가 올라간 버튼만 핑크(Primary), 나머지는 크림(Secondary). 열 때마다 RESUME부터.
    readonly Image[] menuImg = new Image[4];
    readonly string[] menuKey = new string[4];
    bool lastMenuOpen;

    void MenuButton(int i, RectTransform d, string key, UnityEngine.Events.UnityAction onClick)
    {
        // 시안의 버튼 칸: x 700, y 431 + 106*i, 520x88
        var slotRect = new Rect(700f, 431f + 106f * i, 520f, 88f);
        var slot = new GameObject(key, typeof(RectTransform), typeof(Image));
        var srt = (RectTransform)slot.transform;
        srt.SetParent(d, false);
        Place(srt, slotRect.x, slotRect.y, slotRect.width, slotRect.height);
        slot.GetComponent<Image>().color = Color.clear;   // 클릭 영역
        menuKey[i] = key;
        menuImg[i] = new GameObject("Visual", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        menuImg[i].transform.SetParent(srt, false);
        menuImg[i].raycastTarget = false;

        var b = slot.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(GameAudio.UIClick);
        b.onClick.AddListener(onClick);
        b.onClick.AddListener(() => { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); });
        var fx = slot.AddComponent<UIButtonFx>();
        fx.onHover = () => SetMenuFocus(i);
    }

    void SetMenuFocus(int focus)
    {
        for (int i = 0; i < 4; i++)
        {
            string name = $"M_Btn{menuKey[i]}{(i == focus ? "On" : "Off")}";
            menuImg[i].sprite = skin.Get(name);
            var r = FigmaUILayout.Rects[name];
            float slotY = 431f + 106f * i;
            Place(menuImg[i].rectTransform, r.x - 700f, r.y - slotY, r.width, r.height, 520f, 88f);
        }
    }

    // ===== 02-1 시작 화면 =====
    void BuildTitle(Transform root)
    {
        titleRoot = FullScreen("Title", root);
        Background(titleRoot.transform, "BG_Title", out _);
        var d = Design("Design", titleRoot.transform);
        Spr("Cat", d, "T_Cat");
        var logo = Spr("Logo", d, "T_Logo");
        UIMotion.Add(logo.gameObject, UIMotion.Mode.DropIn, null, 1.2f, false);

        UIMotion.Add(Spr("DecoLineA", d, "T_LineA").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.3f, 1.2f, 0.6f, 1f, 2f, 1f));
        UIMotion.Add(Spr("DecoLineB", d, "T_LineB").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1.2f, 0.3f, 1f, 2f, 1f));
        var popIn = UIMotion.Keys(0f, 0f, 0.15f, 0f, 0.6f, 1.15f, 0.95f, 0.97f, 1.35f, 1f, 2f, 1f);
        UIMotion.Add(Spr("StarA", d, "T_StarA").gameObject, UIMotion.Mode.Scale, popIn);
        UIMotion.Add(Spr("StarB", d, "T_StarB").gameObject, UIMotion.Mode.Scale, popIn);

        Btn("GameStart", d, "T_BtnStart", OnStart);
        Btn("HowToPlay", d, "T_HowTo", OpenGuide);

        // 언어 토글: 그림 한 장(선택된 쪽이 크림색) + 왼쪽 반 KOR / 오른쪽 반 ENG 클릭 영역
        langToggle = Spr("LangToggle", d, "T_LangKOR");
        var lr = FigmaUILayout.Rects["T_LangKOR"];
        HitArea("KOR", d, lr.x, lr.y, lr.width * 0.5f, lr.height, () => SetLanguage(false));
        HitArea("ENG", d, lr.x + lr.width * 0.5f, lr.y, lr.width * 0.5f, lr.height, () => SetLanguage(true));
    }

    // ===== 02-2~4 / 03-2~4 게임 설명 =====
    void BuildGuide(Transform root)
    {
        guideRoot = FullScreen("Guide", root);
        guideFade = FullScreen("TitleFade", guideRoot.transform).AddComponent<Image>();
        guideFade.color = TitleFade;
        guideDim = Dim(guideRoot.transform, () => CloseGuide());   // 바깥 클릭으로도 닫힘
        var d = Design("Design", guideRoot.transform);
        Spr("Panel", d, "G_Panel").raycastTarget = true;   // 패널 클릭은 닫기로 안 넘어가게
        Btn("Close", d, "G_Close", () => CloseGuide());

        guideHeader = Label("Header", d, skin.bodyFont, 32, Brown, 0f, 0f, 400f, 44f);
        guideHeader.alignment = TextAnchor.MiddleLeft;
        Place(guideHeader.rectTransform, 718f, 373f, 400f, 44f);

        // 1/3 목표
        pages[0] = Page("Goal", d);
        Spr("Illustration", pages[0].transform, "G_Illust1");
        var goal = Loc(Label("Text", pages[0].transform, skin.bodyFont, 26, DarkBrown, 960f, 696.5f, 560f, 90f),
            "마트 곳곳에 있는 음식을 모아\n<color=#F2737E>101CM</color>까지 성장하세요!",
            "Collect food all around the mart\nand grow to <color=#F2737E>101CM</color>!");
        goal.lineSpacing = 1.15f;

        // 2/3 조작 방법
        pages[1] = Page("Controls", d);
        Spr("Keys", pages[1].transform, "G_Keys");
        Loc(Label("MoveCaption", pages[1].transform, skin.bodyFont, 22, Brown, 854f, 676f, 160f, 30f), "이동", "Move");
        Loc(Label("JumpCaption", pages[1].transform, skin.bodyFont, 22, Brown, 1079f, 580.5f, 160f, 30f), "점프", "Jump");
        Loc(Label("EatCaption", pages[1].transform, skin.bodyFont, 22, Brown, 1079f, 688.5f, 160f, 30f), "음식 먹기", "Eat");

        // 3/3 게임 규칙
        pages[2] = Page("Rules", d);
        Spr("Rows", pages[2].transform, "G_Rules");
        string[] ko =
        {
            "음식을 먹어서 동료 고양이를 데려와요!",
            "쇼핑카트에 부딪히면 고양이들이 흩어져요. 조심!",
            "100CM를 달성하면 마지막 고양이가 나타나요!",
            "무지개 고양이를 데려와 101CM를 달성해서 탈출해요!",
        };
        string[] en =
        {
            "Eat food to bring friend cats along!",
            "Hit a shopping cart and your cats scatter!",
            "Reach 100CM and the last cat appears!",
            "Bring the rainbow cat to 101CM and escape!",
        };
        for (int i = 0; i < 4; i++)
        {
            bool last = i == 3;
            var t = Loc(Label("Rule" + (i + 1), pages[2].transform, skin.bodyFont, last ? 19 : 20, last ? Coral : DarkBrown, 0f, 0f, 440f, 30f), ko[i], en[i]);
            t.alignment = TextAnchor.MiddleLeft;
            Place(t.rectTransform, 775f, 497.5f + 62f * i, 440f, 30f);
        }

        prevButton = Btn("Prev", d, "G_Prev", () => ShowPage(page - 1));
        prevGroup = prevButton.gameObject.AddComponent<CanvasGroup>();
        nextButton = Btn("Next", d, "G_Next", () => ShowPage(page + 1));
        nextGroup = nextButton.gameObject.AddComponent<CanvasGroup>();
        for (int i = 0; i < 3; i++) dots[i] = Spr("Dot" + (i + 1), d, "G_DotOff");
        guideRoot.SetActive(false);
    }

    // ===== 04 클리어 =====
    void BuildClear(Transform root)
    {
        clearRoot = FullScreen("Clear", root);
        var white = FullScreen("White", clearRoot.transform).AddComponent<Image>();
        white.color = Color.white;
        Background(clearRoot.transform, "BG_Clear", out var bg);
        UIMotion.Add(bg.gameObject, UIMotion.Mode.Alpha, UIMotion.Keys(0f, 0.8f, 1.2f, 1f, 2f, 1f));
        var d = Design("Design", clearRoot.transform);
        Spr("Cats", d, "C_Cats");
        var title = Spr("Title", d, "C_Title");
        UIMotion.Add(title.gameObject, UIMotion.Mode.DropIn, null, 1.2f, false);

        var settle = UIMotion.Keys(0f, 1.2f, 0.065f, 1.2f, 1.2f, 1f, 2f, 1f);
        UIMotion.Add(Spr("StarA", d, "C_StarA").gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Spr("StarB", d, "C_StarB").gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Spr("StarC", d, "C_StarC").gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Spr("StarD", d, "C_StarD").gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Spr("SparkA", d, "C_SparkA").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.575f, 1f, 1.2f, 1.15f, 2f, 1.15f));
        UIMotion.Add(Spr("SparkB", d, "C_SparkB").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.065f, 1f, 0.79f, 1.15f, 2f, 1.15f));
        UIMotion.Add(Spr("SparkC", d, "C_SparkC").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.065f, 1f, 0.7f, 1.15f, 2f, 1.15f));
        Spr("SparkD", d, "C_SparkD");

        // 클리어 시간 말풍선 (KO: 클리어 시간 / EN: CLEAR TIME) — 위치는 ApplyLanguage에서
        clearBubble = Spr("Bubble", d, "C_BubbleKO");
        var label = Loc(Label("ClearLabel", d, skin.bodyFont, 50, Brown, 1363.5f, 536.5f, 500f, 70f), "클리어 시간", "CLEAR TIME");
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        clearTime = Label("ClearTime", d, skin.bodyFont, 45, Brown, 1363.5f, 593.5f, 300f, 60f);
        clearTime.text = "00:00";

        Btn("Retry", d, "C_BtnRetry", OnRetry);
        Btn("Home", d, "C_BtnHome", OnHome);
        clearRoot.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>에디터 미리보기용: 플레이 없이 Canvas를 만들고 화면 하나만 켠다. screen = title/hud/menu/guide/guide-title/clear</summary>
    public Canvas EditorPreview(string screen, int guidePage = 0, float progress = 0.5f, int menuFocus = 0, bool english = false)
    {
        if (!hudRoot)
        {
            if (!skin) skin = Resources.Load<FigmaUISkin>("FigmaUISkin");
            BuildCanvas();
        }
        bool prevLang = English;
        English = english;
        ApplyLanguage();
        English = prevLang;
        titleRoot.SetActive(screen == "title" || screen == "guide-title");
        hudRoot.SetActive(screen == "hud" || screen == "menu" || screen == "guide");
        menuRoot.SetActive(screen == "menu");
        SetMenuFocus(menuFocus);
        guideRoot.SetActive(false);
        if (screen.StartsWith("guide")) OpenGuide();
        clearRoot.SetActive(screen == "clear");
        if (guideRoot.activeSelf) ShowPage(guidePage);
        int cm = Mathf.RoundToInt(progress * GameManager.Goal);
        gaugeMask.sizeDelta = new Vector2(718f * progress, gaugeMask.sizeDelta.y);
        cmText.text = $"{cm} / {GameManager.Goal} CM";
        catText.text = english ? "Cats following: 6" : "따라오는 고양이 6마리";
        return GetComponentInChildren<Canvas>(true);
    }
#endif

    // ---------------- UI 헬퍼 ----------------
    static GameObject FullScreen(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    /// <summary>Figma 프레임과 같은 1920x1080 영역 (화면 가운데).</summary>
    static RectTransform Design(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1920f, 1080f);
        return rt;
    }

    static GameObject Page(string name, RectTransform design)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(design, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1920f, 1080f);
        return go;
    }

    static Image Dim(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var go = FullScreen("Dim", parent);
        var img = go.AddComponent<Image>();
        img.color = DimPlay;
        if (onClick != null)
        {
            var b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
        }
        else img.raycastTarget = true;   // 뒤의 게임 화면 클릭 막기
        return img;
    }

    void Background(Transform parent, string sprite, out Image bg)
    {
        var go = new GameObject("Background", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1920f, 1080f);
        var fit = go.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1920f / 1080f;
        bg = go.AddComponent<Image>();
        bg.sprite = skin.Get(sprite);
        bg.raycastTarget = true;   // 뒤의 게임 화면 클릭 막기
    }

    /// <summary>Figma 좌표(왼쪽 위 x, y, 폭, 높이)로 RectTransform 배치. 부모는 frameW x frameH 크기, 가운데 기준.</summary>
    static void Place(RectTransform rt, float x, float y, float w, float h, float frameW = 1920f, float frameH = 1080f)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w * 0.5f - frameW * 0.5f, frameH * 0.5f - (y + h * 0.5f));
    }

    /// <summary>Figma 좌표의 중심(cx, cy)에 w x h 상자 배치.</summary>
    static void PlaceCenter(RectTransform rt, float cx, float cy, float w, float h) => Place(rt, cx - w * 0.5f, cy - h * 0.5f, w, h);

    static void PlaceSprite(RectTransform rt, string sprite)
    {
        var r = FigmaUILayout.Rects[sprite];
        Place(rt, r.x, r.y, r.width, r.height);
    }

    /// <summary>스프라이트를 FigmaUILayout에 적힌 시안 위치에 놓는다 (부모는 1920x1080 프레임).</summary>
    Image Spr(string name, Transform parent, string sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        PlaceSprite(rt, sprite);
        var img = go.GetComponent<Image>();
        img.sprite = skin.Get(sprite);
        img.raycastTarget = false;
        return img;
    }

    Button Btn(string name, Transform parent, string sprite, UnityEngine.Events.UnityAction onClick)
    {
        var img = Spr(name, parent, sprite);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.ColorTint;
        var cb = b.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f);
        cb.pressedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.selectedColor = Color.white;
        cb.disabledColor = Color.white;   // 비활성 화살표는 CanvasGroup 알파로 표시
        b.colors = cb;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(GameAudio.UIClick);
        b.onClick.AddListener(onClick);
        // 클릭한 버튼이 선택된 채로 남으면 Space(점프)가 버튼을 다시 누르므로 선택 해제
        b.onClick.AddListener(() => { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); });
        img.gameObject.AddComponent<UIButtonFx>();
        return b;
    }

    static void HitArea(string name, Transform parent, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, x, y, w, h);
        go.GetComponent<Image>().color = Color.clear;
        var b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(GameAudio.UIClick);
        b.onClick.AddListener(onClick);
        b.onClick.AddListener(() => { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); });
    }

    /// <summary>언어에 따라 바뀌는 글자로 등록. 영어일 때는 Fredoka SemiBold(EN 페이지 폰트).</summary>
    Text Loc(Text t, string ko, string en, bool enFredoka = true)
    {
        locs.Add(new LocText { text = t, ko = ko, en = en, enFredoka = enFredoka });
        return t;
    }

    /// <summary>Figma 좌표의 중심(cx, cy)에 가운데 정렬 글자.</summary>
    static Text Label(string name, Transform parent, Font font, int size, Color color, float cx, float cy, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        PlaceCenter(rt, cx, cy, w, h);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
