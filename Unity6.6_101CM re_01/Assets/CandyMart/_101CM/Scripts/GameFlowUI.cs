using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Figma 시안(101CM Game UI)대로 만든 Canvas UI 전체.
/// 시작 화면(02-1) · 게임 설명 1~3(02-2~4 / 03-2~4) · 진행 HUD(01-x) · Esc 메뉴(03-1) · 클리어(04).
/// 좌표는 Figma 1920x1080 프레임 기준(왼쪽 위 x, y, 폭, 높이). 이미지·폰트는 Resources/FigmaUISkin.
/// GameManager가 씬에서 못 찾으면 실행 시 자동으로 붙인다.
/// </summary>
public class GameFlowUI : MonoBehaviour
{
    public static GameFlowUI I { get; private set; }

    public FigmaUISkin skin;              // 비우면 Resources/FigmaUISkin
    public float endingDelay = 1.2f;      // 다리 끝 도착 → 클리어 화면까지 (축하 모션 보여줄 시간)

    [Header("진행 HUD")]
    [Tooltip("Figma 시안 크기(1436px 게이지) 대비 배율. 1이면 시안 그대로 — 화면 가운데를 많이 가려서 줄여 씀")]
    public float hudScale = 0.5f;
    [Tooltip("화면 위쪽에서 HUD(고양이 귀 끝)까지 거리, px")]
    public float hudTop = 16f;

    static readonly Color Brown = new Color32(0x8A, 0x5A, 0x4E, 0xFF);
    static readonly Color DimColor = new Color(1f, 0.769f, 0.753f, 0.28f);   // 03 프레임 Dim: rgba(255,196,192,0.3) x 0.94

    GameObject titleRoot, hudRoot, menuRoot, guideRoot, clearRoot;
    RectTransform gaugeMask;
    Text cmText, catText;
    float shownProgress;
    bool endingShown;

    // 게임 설명
    Image guideHeader, guideContent;
    CanvasGroup prevGroup, nextGroup;
    Button prevButton, nextButton;
    readonly RectTransform[] dots = new RectTransform[3];
    readonly Image[] dotImages = new Image[3];
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
        if (gm.MenuOpen && !lastMenuOpen) SetMenuFocus(0);   // Esc로 새로 열 때만 RETRY부터 (게임 설명에서 돌아올 땐 유지)
        lastMenuOpen = gm.MenuOpen;
        menuRoot.SetActive(menuOn);
        if (hudRoot.activeSelf) UpdateHud(gm);
    }

    void UpdateHud(GameManager gm)
    {
        shownProgress = Mathf.MoveTowards(shownProgress, gm.Progress, Time.unscaledDeltaTime * 0.8f);
        gaugeMask.sizeDelta = new Vector2(1436f * shownProgress, gaugeMask.sizeDelta.y);
        cmText.text = $"{gm.CM} / {GameManager.Goal} CM";
        catText.text = $"따라오는 고양이 {(gm.player ? gm.player.FollowerCount : 0)}마리";
    }

    // ---------------- 버튼 동작 ----------------
    void OnStart()
    {
        titleRoot.SetActive(false);
        if (GameManager.I) GameManager.I.BeginGame();
    }

    void OnRetry() { if (GameManager.I) GameManager.I.Restart(false); }
    void OnHome() { if (GameManager.I) GameManager.I.Restart(true); }

    void OpenGuide()
    {
        ShowPage(0);
        guideRoot.SetActive(true);
    }

    /// <summary>Esc 또는 바깥 클릭. 게임 설명이 열려 있었으면 닫고 true.</summary>
    public bool CloseGuide()
    {
        if (!guideRoot || !guideRoot.activeSelf) return false;
        guideRoot.SetActive(false);
        return true;
    }

    void ShowPage(int p)
    {
        page = Mathf.Clamp(p, 0, 2);
        guideHeader.sprite = skin.Get("Guide_Header" + (page + 1));
        guideHeader.SetNativeSize();
        guideContent.sprite = skin.Get("Guide_Content" + (page + 1));
        // 점: 현재 페이지만 34px 알약, 나머지 14px 원 (간격 10)
        float x = 919f;
        for (int i = 0; i < 3; i++)
        {
            bool on = i == page;
            float w = on ? 34f : 14f;
            dotImages[i].sprite = skin.Get(on ? "Guide_DotOn" : "Guide_DotOff");
            Place(dots[i], x, 821f, w, 15f);
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

    IEnumerator ShowEnding()
    {
        yield return new WaitForSecondsRealtime(endingDelay);
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
    }

    // ===== 01 진행 HUD =====
    void BuildHud(Transform root)
    {
        // 시안의 게이지 묶음(x 222~1698, y 268~645)을 화면 위쪽 가운데에 붙인다.
        hudRoot = new GameObject("HUD", typeof(RectTransform));
        var hrt = (RectTransform)hudRoot.transform;
        hrt.SetParent(root, false);
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(1476f, 377f);
        hrt.anchoredPosition = new Vector2(0f, -hudTop);
        hrt.localScale = new Vector3(hudScale, hudScale, 1f);
        const float ox = 222f, oy = 268f;

        Img("GaugeBG", hrt, "HUD_GaugeBG", 242 - ox, 394 - oy, 1436, 146, 1476, 377);
        var maskGo = new GameObject("GaugeFillMask", typeof(RectTransform), typeof(RectMask2D));
        gaugeMask = (RectTransform)maskGo.transform;
        gaugeMask.SetParent(hrt, false);
        gaugeMask.anchorMin = gaugeMask.anchorMax = gaugeMask.pivot = new Vector2(0f, 1f);
        gaugeMask.anchoredPosition = new Vector2(242 - ox, -(394 - oy));
        gaugeMask.sizeDelta = new Vector2(0f, 146f);
        var fill = Img("GaugeFill", gaugeMask, "HUD_GaugeFill", 0, 0, 1436, 146, 0, 0);
        var frt = (RectTransform)fill.transform;
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0f, 1f);
        frt.anchoredPosition = Vector2.zero;
        Img("GaugeBorder", hrt, "HUD_GaugeBorder", 222 - ox, 374 - oy, 1476, 186, 1476, 377);
        Img("CatEars", hrt, "HUD_Ears", 696 - ox, 268 - oy, 537, 114, 1476, 377);

        cmText = Label("CM", hrt, skin.titleFont, 100, Brown, 242 - ox, 394 - oy, 1436, 146, 1476, 377);
        catText = Label("Cats", hrt, skin.bodyFont, 60, Brown, 584 - ox, 570 - oy, 752, 75, 1476, 377);
        hudRoot.SetActive(false);
    }

    // ===== 03-1 Esc 메뉴 =====
    void BuildMenu(Transform root)
    {
        menuRoot = FullScreen("Menu", root);
        Dim(menuRoot.transform, null);
        var d = Design("Design", menuRoot.transform);
        Img("CatEars", d, "Panel_Ears", 691, 170, 537, 114);
        Img("Panel", d, "Panel_BG", 568, 243, 784, 734);
        Img("Title", d, "Menu_Title", 857, 342, 206, 55);
        MenuButton(0, d, "Retry", "Menu_LabelRetry", 762, 287, 32, OnRetry);
        MenuButton(1, d, "HowToPlay", "Menu_LabelHowToPlay", 754, 360, 31, OpenGuide);
        MenuButton(2, d, "Home", "Menu_LabelHome", 754, 286, 31, OnHome);
        SetMenuFocus(0);
        menuRoot.SetActive(false);
    }

    // 메뉴 버튼: 마우스가 올라간 버튼만 핑크(Primary), 나머지는 아이보리(Secondary). 열 때마다 RETRY부터.
    readonly Image[] menuBg = new Image[3];
    readonly Image[] menuLabel = new Image[3];
    bool lastMenuOpen;

    void MenuButton(int i, RectTransform d, string name, string label, float labelX, float labelW, float labelH, UnityEngine.Events.UnityAction onClick)
    {
        // 시안의 버튼 칸: x 700, y 437 + 120*i, 520x96. 배경·글자는 칸 기준으로 놓는다.
        float slotY = 437f + 120f * i;
        var slot = new GameObject(name, typeof(RectTransform), typeof(Image));
        var srt = (RectTransform)slot.transform;
        srt.SetParent(d, false);
        Place(srt, 700f, slotY, 520f, 96f);
        slot.GetComponent<Image>().color = Color.clear;   // 클릭 영역
        menuBg[i] = Img("Bg", srt, "Menu_BtnBgIvory", 0, 0, 0, 0, 520f, 96f);
        menuLabel[i] = Img("Label", srt, label, labelX - 700f, 31f, labelW, labelH, 520f, 96f);

        var b = slot.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(onClick);
        b.onClick.AddListener(() => { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); });
        var fx = slot.AddComponent<UIButtonFx>();
        fx.onHover = () => SetMenuFocus(i);
    }

    void SetMenuFocus(int focus)
    {
        for (int i = 0; i < 3; i++)
        {
            bool pink = i == focus;
            var bg = menuBg[i];
            bg.sprite = skin.Get(pink ? "Menu_BtnBgPink" : "Menu_BtnBgIvory");
            // 그림자 크기가 달라 이미지별 위치가 조금 다름 (칸 왼쪽 위 기준)
            if (pink) Place(bg.rectTransform, -15f, -4f, 550f, 128f, 520f, 96f);
            else Place(bg.rectTransform, -14f, -3f, 548f, 126f, 520f, 96f);
            menuLabel[i].color = pink ? Color.white : Brown;
        }
    }

    // ===== 02-1 시작 화면 =====
    void BuildTitle(Transform root)
    {
        titleRoot = FullScreen("Title", root);
        Background(titleRoot.transform, out _);
        var d = Design("Design", titleRoot.transform);
        Img("Cat", d, "Start_Cat", 134, 464, 542, 560);
        var logo = Img("Logo", d, "Start_Logo", 645.352f, 212.165f, 1087, 335);
        UIMotion.Add(logo.gameObject, UIMotion.Mode.DropIn, null, 1.2f, false);

        var lineA = Img("DecoLineA", d, "Start_DecoLineA", 1652.59f, 110.174f, 128, 123);
        UIMotion.Add(lineA.gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.3f, 1.2f, 0.6f, 1f, 2f, 1f));
        var lineB = Img("DecoLineB", d, "Start_DecoLineB", 1681.79f, 177.933f, 101, 68);
        UIMotion.Add(lineB.gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1.2f, 0.3f, 1f, 2f, 1f));
        var popIn = UIMotion.Keys(0f, 0f, 0.15f, 0f, 0.6f, 1.15f, 0.95f, 0.97f, 1.35f, 1f, 2f, 1f);
        UIMotion.Add(Img("StarA", d, "Start_StarA", 635.682f, 118.459f, 148, 151).gameObject, UIMotion.Mode.Scale, popIn);
        UIMotion.Add(Img("StarB", d, "Start_StarB", 604.839f, 211.563f, 99, 100).gameObject, UIMotion.Mode.Scale, popIn);

        Btn("GameStart", d, "Start_BtnGameStart", 833, 710, 690, 160, OnStart);
        Btn("HowToPlay", d, "Start_BtnHowToPlay", 963, 893, 431, 47, OpenGuide);
    }

    // ===== 02-2~4 / 03-2~4 게임 설명 =====
    void BuildGuide(Transform root)
    {
        guideRoot = FullScreen("Guide", root);
        Dim(guideRoot.transform, () => CloseGuide());   // 시안에 닫기 버튼이 없어 바깥 클릭 / Esc로 닫음
        var d = Design("Design", guideRoot.transform);
        Img("CatEars", d, "Panel_Ears", 691, 170, 537, 114);
        Img("Panel", d, "Panel_BG", 568, 243, 784, 734).raycastTarget = true;   // 패널 클릭은 닫기로 안 넘어가게
        Img("Title", d, "Guide_Title", 802, 318, 317, 35);
        guideHeader = Img("Header", d, "Guide_Header1", 681, 380, 87, 27);
        var hrt = guideHeader.rectTransform;
        hrt.pivot = new Vector2(0f, 0.5f);
        hrt.anchoredPosition = new Vector2(681 - 960, 540 - (380 + 13.5f));
        guideContent = Img("Content", d, "Guide_Content1", 676, 427, 568, 357);

        prevButton = Btn("Prev", d, "Guide_Prev", 841, 800, 56, 61, () => ShowPage(page - 1));
        prevGroup = prevButton.gameObject.AddComponent<CanvasGroup>();
        nextButton = Btn("Next", d, "Guide_Next", 1023, 800, 56, 61, () => ShowPage(page + 1));
        nextGroup = nextButton.gameObject.AddComponent<CanvasGroup>();
        for (int i = 0; i < 3; i++)
        {
            dotImages[i] = Img("Dot" + (i + 1), d, "Guide_DotOff", 919 + 24 * i, 821, 14, 15);
            dots[i] = dotImages[i].rectTransform;
        }
        guideRoot.SetActive(false);
    }

    // ===== 04 클리어 =====
    void BuildClear(Transform root)
    {
        clearRoot = FullScreen("Clear", root);
        Background(clearRoot.transform, out var bg);
        UIMotion.Add(bg.gameObject, UIMotion.Mode.Alpha, UIMotion.Keys(0f, 0.8f, 1.2f, 1f, 2f, 1f));
        var d = Design("Design", clearRoot.transform);
        Img("Cats", d, "Clear_Cats", 577, 374, 795, 461);
        var title = Img("Title", d, "Clear_Title", 354, 111, 1207, 348);
        UIMotion.Add(title.gameObject, UIMotion.Mode.DropIn, null, 1.2f, false);

        var settle = UIMotion.Keys(0f, 1.2f, 0.065f, 1.2f, 1.2f, 1f, 2f, 1f);
        UIMotion.Add(Img("StarA", d, "Clear_StarA", 387, 310, 100, 100).gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Img("StarB", d, "Clear_StarB", 369, 282, 69, 68).gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Img("StarC", d, "Clear_StarC", 1480, 58, 101, 99).gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Img("StarD", d, "Clear_StarD", 1482, 139, 68, 68).gameObject, UIMotion.Mode.Scale, settle);
        UIMotion.Add(Img("SparkA", d, "Clear_SparkA", 366, 363, 24, 37).gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.575f, 1f, 1.2f, 1.15f, 2f, 1.15f));
        UIMotion.Add(Img("SparkB", d, "Clear_SparkB", 361, 340, 28, 20).gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.065f, 1f, 0.79f, 1.15f, 2f, 1.15f));
        UIMotion.Add(Img("SparkC", d, "Clear_SparkC", 1545, 133, 38, 19).gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.065f, 1f, 0.7f, 1.15f, 2f, 1.15f));
        Img("SparkD", d, "Clear_SparkD", 1543, 158, 29, 15);

        Btn("Retry", d, "Clear_BtnRetry", 690, 785, 540, 136, OnRetry);
        Btn("Home", d, "Clear_BtnHome", 690, 910, 540, 134, OnHome);
        clearRoot.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>에디터 미리보기용: 플레이 없이 Canvas를 만들고 화면 하나만 켠다. screen = title/hud/menu/guide/clear</summary>
    public Canvas EditorPreview(string screen, int guidePage = 0, float progress = 0.37f, int menuFocus = 0)
    {
        if (!hudRoot)
        {
            if (!skin) skin = Resources.Load<FigmaUISkin>("FigmaUISkin");
            BuildCanvas();
        }
        titleRoot.SetActive(screen == "title" || screen == "guide-title");
        hudRoot.SetActive(screen == "hud" || screen == "menu" || screen == "guide");
        menuRoot.SetActive(screen == "menu");
        SetMenuFocus(menuFocus);
        guideRoot.SetActive(screen.StartsWith("guide"));
        clearRoot.SetActive(screen == "clear");
        if (guideRoot.activeSelf) ShowPage(guidePage);
        gaugeMask.sizeDelta = new Vector2(1436f * progress, gaugeMask.sizeDelta.y);
        cmText.text = $"{Mathf.RoundToInt(progress * GameManager.Goal)} / {GameManager.Goal} CM";
        catText.text = "따라오는 고양이 6마리";
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

    void Dim(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var go = FullScreen("Dim", parent);
        var img = go.AddComponent<Image>();
        img.color = DimColor;
        if (onClick != null) go.AddComponent<Button>().onClick.AddListener(onClick);
        else img.raycastTarget = true;   // 뒤의 게임 화면 클릭 막기
    }

    void Background(Transform parent, out Image bg)
    {
        var white = FullScreen("White", parent).AddComponent<Image>();
        white.color = Color.white;
        var go = new GameObject("Background", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1920f, 1080f);
        var fit = go.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 1920f / 1080f;
        bg = go.AddComponent<Image>();
        bg.sprite = skin.Get("BG_Sunset");
        bg.raycastTarget = false;
    }

    /// <summary>Figma 좌표(왼쪽 위 x, y, 폭, 높이)로 RectTransform 배치. 부모는 frameW x frameH 크기, 가운데 기준.</summary>
    static void Place(RectTransform rt, float x, float y, float w, float h, float frameW = 1920f, float frameH = 1080f)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w * 0.5f - frameW * 0.5f, frameH * 0.5f - (y + h * 0.5f));
    }

    Image Img(string name, Transform parent, string sprite, float x, float y, float w, float h, float frameW = 1920f, float frameH = 1080f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        if (frameW > 0f) Place(rt, x, y, w, h, frameW, frameH);
        else rt.sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>();
        img.sprite = skin.Get(sprite);
        img.raycastTarget = false;
        return img;
    }

    Button Btn(string name, Transform parent, string sprite, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick)
    {
        var img = Img(name, parent, sprite, x, y, w, h);
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
        b.onClick.AddListener(onClick);
        // 클릭한 버튼이 선택된 채로 남으면 Space(점프)가 버튼을 다시 누르므로 선택 해제
        b.onClick.AddListener(() => { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); });
        img.gameObject.AddComponent<UIButtonFx>();
        return b;
    }

    static Text Label(string name, Transform parent, Font font, int size, Color color, float x, float y, float w, float h, float frameW, float frameH)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Place(rt, x, y, w, h, frameW, frameH);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
