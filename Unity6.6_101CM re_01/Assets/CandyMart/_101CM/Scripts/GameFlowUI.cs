using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Figma 시안(101CM Game UI · UI SunSet KO/EN)대로 만든 Canvas UI 전체.
/// 시작 화면(02-1) · 게임 방법 1~4(02-1~4 / 03-1~4) · 진행 HUD(01-x) · Esc 메뉴(03-1) · 클리어(04).
/// 이미지 위치는 FigmaUILayout(Figma 1920x1080 프레임 기준 왼쪽 위 x, y, 폭, 높이). 이미지·폰트는 Resources/FigmaUISkin.
/// 메뉴와 게임 방법 카드는 최종 Figma 원본을 사용한다. 음량과 플레이 기록은 실행 중 갱신한다.
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
    // Dim: rgba(255,196,192) — Esc 위(03): 0.6 x 0.94 / 시작 화면 위(02): 0.3 x 0.94
    static readonly Color DimPlay = new Color(1f, 0.769f, 0.753f, 0.564f);
    static readonly Color DimTitle = new Color(1f, 0.769f, 0.753f, 0.282f);
    // 시작 화면 배경은 0.75, 게임 설명(02-2~4) 배경은 0.55 불투명 → 흰색 0.27을 덮어 맞춤
    static readonly Color TitleFade = new Color(1f, 1f, 1f, 0.267f);

    GameObject titleRoot, hudRoot, menuRoot, guideRoot, clearRoot;
    GameObject titleDesign;   // 시작 화면의 로고·고양이·버튼 (배경 그림 제외)
    RectTransform gaugeMask;
    Text cmText, catText, clearTime;
    float shownProgress;
    bool endingShown;

    // 언어별 글자
    struct LocText { public Text text; public string ko, en; public bool enFredoka; }
    readonly List<LocText> locs = new List<LocText>();
    Image langToggle, clearBubble, menuArt, guideArt;
    readonly List<(Image art, string asset, float cx, float y)> finalButtons = new();

    VolumeSlider masterSlider;
    Text masterValue;
    RawImage masterArtworkPatch;
    RectTransform masterSliderDesign, settingsKoHit, settingsEnHit;

    Image guideDim, guideFade;
    RectTransform guideDesign;
    GameObject settingsRoot;
    Button prevButton, nextButton;
    const int GuidePageCount = 4;
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
        if (gm.MenuOpen && !lastMenuOpen) SetMenuFocus(0);   // Esc로 새로 열 때만 계속하기부터 (게임 방법에서 돌아올 땐 유지)
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
        // Figma 02-2~4: 시작 화면 위 게임 설명은 배경 그림만 남고 로고·고양이·버튼은 사라짐
        if (titleDesign) titleDesign.SetActive(!overTitle);
        guideFade.gameObject.SetActive(overTitle);
        guideDim.color = overTitle ? DimTitle : DimPlay;
        masterSlider.SetValue(GameAudio.MasterVolume);
        UpdateVolumeTexts();
        ShowPage(0);
        guideRoot.SetActive(true);
    }

    /// <summary>Esc·닫기 버튼·바깥 클릭. 게임 설명이나 설정이 열려 있었으면 닫고 true.</summary>
    public bool CloseGuide()
    {
        if (!guideRoot || !guideRoot.activeSelf) return false;
        guideRoot.SetActive(false);
        GameAudio.SaveVolumes();
        if (titleDesign) titleDesign.SetActive(true);
        return true;
    }

    void UpdateVolumeTexts()
    {
        masterValue.text = Mathf.RoundToInt(masterSlider.Value * 100f) + "%";
    }

    void ShowPage(int p)
    {
        page = Mathf.Clamp(p, 0, GuidePageCount - 1);
        bool settings = page == 0;
        bool english = English;
        var sprite = FinalArtwork($"Guide{page + 1}", english);
        guideArt.sprite = sprite;
        guideDesign.anchoredPosition = new Vector2(0f, settings && titleRoot.activeSelf ? -7f : 0f);
        settingsRoot.SetActive(settings);
        prevButton.interactable = page > 0;
        nextButton.interactable = page < GuidePageCount - 1;
        if (settings)
        {
            // Sample a clean pixel of the card so the runtime volume control blends into the Figma panel.
            masterArtworkPatch.texture = sprite.texture;
            masterArtworkPatch.uvRect = new Rect(0.72f, 0.52f, 0f, 0f);
            masterSliderDesign.anchoredPosition = new Vector2(english ? 5f : 0f, english ? 2f : 7f);
            Place(masterValue.rectTransform, 1131f, english ? 530f : 524f, 90f, 35f);
            float x = english ? 849f : 837f;
            float y = english ? 608f : 612f;
            Place(settingsKoHit, x, y, 120f, 72f);
            Place(settingsEnHit, x + 120f, y, 120f, 72f);
        }
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
        masterValue.font = en ? skin.enFont : skin.bodyFont;
        SetMenuFocus(menuFocus);
        foreach (var (art, asset, centerX, y) in finalButtons)
        {
            var sprite = FinalArtwork(asset, en);
            art.sprite = sprite;
            float w = sprite.rect.width * 0.5f;
            Place(art.rectTransform, centerX - w * 0.5f, y, w, sprite.rect.height * 0.5f);
        }
        // 클리어 말풍선: EN은 글자가 길어 더 넓은 풍선, 글자 중심도 오른쪽으로 25px
        clearBubble.sprite = skin.Get(en ? "C_BubbleEN" : "C_BubbleKO");
        PlaceSprite(clearBubble.rectTransform, en ? "C_BubbleEN" : "C_BubbleKO");
        float cx = en ? 1388.5f : 1363.5f;
        foreach (var l in locs)
            if (l.text.name.StartsWith("ClearLabel"))
                PlaceCenter(l.text.rectTransform, cx, en ? 536f : 536.5f, 500f, 70f);
        PlaceCenter(clearTime.rectTransform, cx, en ? 593f : 593.5f, 300f, 60f);
        ShowPage(page);
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
        BuildSettings();
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
        // Position below is relative to the center of the 1920x1080 HUD.
        gaugeMask.anchorMin = gaugeMask.anchorMax = new Vector2(0.5f, 0.5f);
        gaugeMask.pivot = new Vector2(0f, 1f);
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
        menuArt = FinalImage("Panel", d);
        Place(menuArt.rectTransform, 560f, 170f, 800f, 815f);
        MenuHit(0, d, "Resume", OnResume);
        MenuHit(1, d, "Retry", OnRetry);
        MenuHit(2, d, "HowTo", OpenGuide);
        MenuHit(3, d, "Home", OnHome);
        menuRoot.SetActive(false);
    }

    // 메뉴 버튼: 마우스가 올라간 버튼만 핑크. 패널 그림을 통째로 바꾼다 (Menu = 계속하기, Menu1~3 = 다시 하기/게임 방법/시작 화면).
    int menuFocus;
    bool lastMenuOpen;

    void MenuHit(int i, Transform d, string name, UnityEngine.Events.UnityAction onClick)
    {
        var b = HitArea(name, d, 700f, 441f + 100f * i, 520f, 72f, onClick);
        b.gameObject.AddComponent<UIButtonFx>().onHover = () => SetMenuFocus(i);
    }

    void SetMenuFocus(int focus)
    {
        menuFocus = focus;
        menuArt.sprite = FinalArtwork(focus == 0 ? "Menu" : $"Menu{focus}", English);
    }

    // ===== 02-1 시작 화면 =====
    void BuildTitle(Transform root)
    {
        titleRoot = FullScreen("Title", root);
        Background(titleRoot.transform, "BG_Title", out _);
        var d = Design("Design", titleRoot.transform);
        titleDesign = d.gameObject;
        Spr("Cat", d, "T_Cat");
        // Figma 101CM 텍스트 모션: 글자 5개, 0.2초 간격, 0.4초, cubic-bezier(0.42, 0, 1, 1), 2초 반복
        LetterTitle("Logo", d, "T_Logo", 5, 0.2f, 0.4f, new Vector4(0.42f, 0f, 1f, 1f));

        UIMotion.Add(Spr("DecoLineA", d, "T_LineA").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1f, 0.3f, 1.2f, 0.6f, 1f, 2f, 1f));
        UIMotion.Add(Spr("DecoLineB", d, "T_LineB").gameObject, UIMotion.Mode.Scale, UIMotion.Keys(0f, 1.2f, 0.3f, 1f, 2f, 1f));
        var popIn = UIMotion.Keys(0f, 0f, 0.15f, 0f, 0.6f, 1.15f, 0.95f, 0.97f, 1.35f, 1f, 2f, 1f);
        UIMotion.Add(Spr("StarA", d, "T_StarA").gameObject, UIMotion.Mode.Scale, popIn);
        UIMotion.Add(Spr("StarB", d, "T_StarB").gameObject, UIMotion.Mode.Scale, popIn);

        FinalButton("GameStart", d, "Start", 1177f, 699f, OnStart);
        FinalButton("HowToPlay", d, "HowTo", 1178f, 839f, OpenGuide);

        // 언어 토글: 그림 한 장(선택된 쪽이 크림색) + 왼쪽 반 KOR / 오른쪽 반 ENG 클릭 영역
        langToggle = Spr("LangToggle", d, "T_LangKOR");
        var lr = FigmaUILayout.Rects["T_LangKOR"];
        HitArea("KOR", d, lr.x, lr.y, lr.width * 0.5f, lr.height, () => SetLanguage(false));
        HitArea("ENG", d, lr.x + lr.width * 0.5f, lr.y, lr.width * 0.5f, lr.height, () => SetLanguage(true));
    }

    // ===== 02-1~4 / 03-1~4 게임 방법 =====
    void BuildGuide(Transform root)
    {
        guideRoot = FullScreen("Guide", root);
        guideFade = FullScreen("TitleFade", guideRoot.transform).AddComponent<Image>();
        guideFade.color = TitleFade;
        guideDim = Dim(guideRoot.transform, () => CloseGuide());
        guideDesign = Design("Design", guideRoot.transform);
        guideArt = FinalImage("Panel", guideDesign);
        Place(guideArt.rectTransform, 560f, 170f, 800f, 815f);
        var panelBlocker = FinalImage("PanelClickBlocker", guideDesign);
        panelBlocker.color = Color.clear;
        panelBlocker.raycastTarget = true;
        Place(panelBlocker.rectTransform, 620f, 265f, 680f, 630f);
        HitArea("Close", guideDesign, 1213f, 294f, 59f, 63f, () => CloseGuide());
        prevButton = HitArea("Prev", guideDesign, 825f, 801f, 64f, 58f, () => ShowPage(page - 1));
        nextButton = HitArea("Next", guideDesign, 1030f, 801f, 64f, 58f, () => ShowPage(page + 1));
        guideRoot.SetActive(false);
    }

    const float SliderX = 843f, SliderW = 278f, SliderY = 537.5f;

    void BuildSettings()
    {
        var d = Design("SettingsControls", guideDesign);
        settingsRoot = d.gameObject;
        var patch = new GameObject("VolumeArtworkPatch", typeof(RectTransform), typeof(RawImage));
        patch.transform.SetParent(d, false);
        masterArtworkPatch = patch.GetComponent<RawImage>();
        masterArtworkPatch.raycastTarget = false;
        Place(masterArtworkPatch.rectTransform, 836f, 520f, 387f, 54f);
        masterSliderDesign = Design("Slider", d);
        masterSlider = MakeSlider(masterSliderDesign);
        masterValue = Label("MasterValue", d, skin.bodyFont, 28, Brown, 0f, 0f, 90f, 35f);
        masterValue.alignment = TextAnchor.MiddleRight;
        masterSlider.onReleased = () => { GameAudio.Eat(); GameAudio.SaveVolumes(); };
        masterSlider.SetValue(GameAudio.MasterVolume);
        UpdateVolumeTexts();

        settingsKoHit = (RectTransform)HitArea("KOR", d, 0f, 0f, 120f, 72f, () => SetLanguage(false)).transform;
        settingsEnHit = (RectTransform)HitArea("ENG", d, 0f, 0f, 120f, 72f, () => SetLanguage(true)).transform;
    }

    VolumeSlider MakeSlider(RectTransform d)
    {
        Spr("MasterTrack", d, "S_Track");
        var maskGo = new GameObject("MasterFillMask", typeof(RectTransform), typeof(RectMask2D));
        var mask = (RectTransform)maskGo.transform;
        mask.SetParent(d, false);
        mask.anchorMin = mask.anchorMax = new Vector2(0.5f, 0.5f);
        mask.pivot = new Vector2(0f, 0.5f);
        mask.anchoredPosition = new Vector2(SliderX - 960f, 540f - (SliderY + 10f));
        mask.sizeDelta = new Vector2(0f, 40f);
        var fill = Spr("MasterFill", mask, "S_Fill");
        var fr = FigmaUILayout.Rects["S_Fill"];
        var frt = fill.rectTransform;
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0f, 0.5f);
        frt.anchoredPosition = new Vector2(fr.x - SliderX, (SliderY + 10f) - (fr.y + fr.height * 0.5f));

        var knob = Spr("MasterKnob", d, "S_Knob");
        var hit = FinalImage("MasterSlider", d);
        Place(hit.rectTransform, SliderX, SliderY - 14f, SliderW, 48f);
        hit.color = Color.clear;
        hit.raycastTarget = true;
        var slider = hit.gameObject.AddComponent<VolumeSlider>();
        slider.track = hit.rectTransform;
        slider.fillMask = mask;
        slider.knob = knob.rectTransform;
        slider.onChanged = value => { GameAudio.MasterVolume = value; UpdateVolumeTexts(); };
        return slider;
    }

    // ===== 04 클리어 =====
    void BuildClear(Transform root)
    {
        clearRoot = FullScreen("Clear", root);
        var white = FullScreen("White", clearRoot.transform).AddComponent<Image>();
        white.color = Color.white;
        Background(clearRoot.transform, "BG_Clear", out _);
        // Keep the clear background opaque; the looping fade caused a flash.
        var d = Design("Design", clearRoot.transform);
        Spr("Cats", d, "C_Cats");
        // Figma CLEAR! 텍스트 모션: 글자 6개, 0.1714초 간격, 0.3429초, cubic-bezier(0, 0, 1, 1.273) — 살짝 튕김, 2초 반복
        LetterTitle("Title", d, "C_Title", 6, 0.17142860f, 0.342857f, new Vector4(0f, 0f, 1f, 1.272999f));

        var starFrom = Vector2.one * 1.2f;
        foreach (string star in new[] { "StarA", "StarB", "StarC", "StarD" })
            UIMotion.AddEaseOutScale(Spr(star, d, "C_" + star).gameObject, starFrom, Vector2.one, 0.065f, 1.2f);

        // Polygon 1–4 resize along their own rotated axes. The sprites already contain
        // the static rotation, so the artwork counter-rotates inside the animated pivot.
        ClearSpark(d, "C_SparkC", new Vector2(1557.27f, 164.37f), 107.29f, new Vector2(2.333f, 1.575f), 0.256f);
        ClearSpark(d, "C_SparkD", new Vector2(1545.17f, 182.26f), 59.45f, new Vector2(1.96f, 1.466f), 0.065f);
        ClearSpark(d, "C_SparkA", new Vector2(368.63f, 356.8f), -27.51f, new Vector2(1.987f, 1.446f), 0.256f);
        ClearSpark(d, "C_SparkB", new Vector2(372.96f, 332.18f), -60.82f, new Vector2(1.808f, 1.396f), 0.065f);

        // 클리어 시간 말풍선 (KO: 클리어 시간 / EN: CLEAR TIME) — 위치는 ApplyLanguage에서
        clearBubble = Spr("Bubble", d, "C_BubbleKO");
        clearBubble.gameObject.AddComponent<ClearBubbleMotion>();
        var label = Loc(Label("ClearLabel", d, skin.bodyFont, 50, Brown, 1363.5f, 536.5f, 500f, 70f), "클리어 시간", "CLEAR TIME");
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        clearTime = Label("ClearTime", d, skin.bodyFont, 45, Brown, 1363.5f, 593.5f, 300f, 60f);
        clearTime.text = "00:00";

        FinalButton("Retry", d, "Retry", 1382.5f, 743f, OnRetry);
        FinalButton("Home", d, "Home", 1382.5f, 862f, OnHome);
        clearRoot.SetActive(false);
    }

    void ClearSpark(RectTransform design, string sprite, Vector2 center, float angle, Vector2 endScale, float delay)
    {
        var pivot = Design(sprite + "Motion", design);
        PlaceCenter(pivot, center.x, center.y, 0f, 0f);
        pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

        var image = Spr(sprite, design, sprite);
        image.rectTransform.SetParent(pivot, true);
        UIMotion.AddEaseOutScale(pivot.gameObject, Vector2.one, endScale, delay, 1.2f);
    }

#if UNITY_EDITOR
    /// <summary>에디터 미리보기용: 플레이 없이 Canvas를 만들고 화면 하나만 켠다. screen = title/hud/menu/guide/guide-title/clear</summary>
    public Canvas EditorPreview(string screen, int guidePage = 0, float progress = 0.5f, bool english = false)
    {
        if (!hudRoot)
        {
            if (!skin) skin = Resources.Load<FigmaUISkin>("FigmaUISkin");
            BuildCanvas();
        }
        bool prevLang = English;
        English = english;
        ApplyLanguage();
        titleRoot.SetActive(screen == "title" || screen == "guide-title");
        hudRoot.SetActive(screen == "hud" || screen == "menu" || screen == "guide");
        menuRoot.SetActive(screen == "menu");
        guideRoot.SetActive(false);
        if (screen.StartsWith("guide") || screen == "settings")
        {
            OpenGuide();
            if (screen != "settings") ShowPage(guidePage);
        }
        clearRoot.SetActive(screen == "clear");
        int cm = Mathf.RoundToInt(progress * GameManager.Goal);
        gaugeMask.sizeDelta = new Vector2(718f * progress, gaugeMask.sizeDelta.y);
        cmText.text = $"{cm} / {GameManager.Goal} CM";
        catText.text = english ? "Cats following: 6" : "따라오는 고양이 6마리";
        English = prevLang;
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

    /// <summary>
    /// 글자별로 움직이는 타이틀(101CM / CLEAR!). 외곽선이 옆 글자 위로 올라오지 않게 층을 나눠 쌓는다:
    /// 모든 글자의 그림자(Sh) → 외곽선(St) → 글자색(Fi). 한 글자의 세 층은 같은 모션으로 함께 움직인다.
    /// </summary>
    void LetterTitle(string name, RectTransform design, string prefix, int count, float stagger, float groupDuration, Vector4 bezier)
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rrt = (RectTransform)root.transform;
        rrt.SetParent(design, false);
        rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
        rrt.sizeDelta = new Vector2(1920f, 1080f);
        foreach (var layer in new[] { "Sh", "St", "Fi" })
            for (int i = 0; i < count; i++)
                FigmaTextMotion.Add(Spr($"{layer}{i}", rrt, $"{prefix}{i}{layer}").gameObject, i * stagger, groupDuration, bezier);
    }

    static Sprite FinalArtwork(string asset, bool english)
    {
        string locale = english ? "EN" : "KO";
        return Resources.Load<Sprite>($"FinalFigmaUI/{locale}_{asset}");
    }

    static Image FinalImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    void FinalButton(string name, Transform parent, string asset, float cx, float y, UnityEngine.Events.UnityAction onClick)
    {
        var art = FinalImage(name, parent);
        AddButton(art, onClick, true);
        finalButtons.Add((art, asset, cx, y));
    }

    static Button AddButton(Image img, UnityEngine.Events.UnityAction onClick, bool animate)
    {
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = animate ? Selectable.Transition.ColorTint : Selectable.Transition.None;
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
        if (animate) img.gameObject.AddComponent<UIButtonFx>();
        return b;
    }

    static Button HitArea(string name, Transform parent, float x, float y, float w, float h, UnityEngine.Events.UnityAction onClick)
    {
        var image = FinalImage(name, parent);
        Place(image.rectTransform, x, y, w, h);
        image.color = Color.clear;
        return AddButton(image, onClick, false);
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
