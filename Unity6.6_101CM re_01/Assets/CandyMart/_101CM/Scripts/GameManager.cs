using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// CM 집계, 100CM → 마지막 1CM 등장, 101CM → 출구 개방, 클리어.
/// Esc 메뉴 열기/닫기(일시정지). 화면(시작·HUD·Esc 메뉴·게임 설명·클리어)은 모두 GameFlowUI(Figma 시안)가 그린다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public const int Goal = 101;

    public CatPlayer player;
    public ExitGate exitGate;
    public Transform lastPieceSpawn;
    public Transform foodRoot;

    /// <summary>다시 시작 시 시작 화면을 건너뛰고 바로 플레이 (씬 다시 불러와도 유지되도록 static).</summary>
    static bool skipTitle;

    int rawCm, lastStage;
    bool lastSpawned, lastCollected, won, menuOpen, onTitle;
    float startTime, endTime;

    public int CM => lastCollected ? Goal : Mathf.Min(rawCm, Goal - 1);
    public float Progress => CM / (float)Goal;
    public bool CanEat => !won && !menuOpen && !onTitle;
    public bool IsWon => won;
    public bool IsPaused => menuOpen || onTitle;
    public bool OnTitle => onTitle;
    public bool MenuOpen => menuOpen;
    /// <summary>플레이 기록 시간(초). Esc 메뉴로 멈춘 시간은 제외.</summary>
    public float RecordTime => (won ? endTime : Time.time) - startTime;

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        EndingSequence.ResetState();
        StartSequence.ResetState();
        if (!GetComponent<ShopWindowFlicker>()) gameObject.AddComponent<ShopWindowFlicker>();
        onTitle = !skipTitle;
        skipTitle = false;
        // 씬에 GameFlowUI가 없으면 자동으로 붙임 (이미지 지정하려면 씬의 GameManager에 미리 붙여두기)
        if (!FindAnyObjectByType<GameFlowUI>()) gameObject.AddComponent<GameFlowUI>();
    }

    void OnDestroy()
    {
        if (I == this) Time.timeScale = 1f;
    }

    void Start()
    {
        startTime = Time.time;
        if (player) player.SetProgress(0f, true);
        CameraFollow.SetCursorLocked(!onTitle);
    }

    /// <summary>시작 화면에서 [시작하기].</summary>
    public void BeginGame()
    {
        if (!onTitle) return;
        onTitle = false;
        startTime = Time.time;
        CameraFollow.SetCursorLocked(true);
        if (player) StartCoroutine(StartSequence.Play(this, player));   // 신나서 뛰어 들어오는 시작 연출
    }

    void Update()
    {
        var kb = Keyboard.current;
#if UNITY_EDITOR
        if (kb != null && kb.f9Key.wasPressedThisFrame) DebugSkipToEnding();
#endif
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (GameFlowUI.I && GameFlowUI.I.CloseGuide()) return;   // 게임 설명이 떠 있으면 그것부터 닫기
        if (!won && !onTitle) SetMenu(!menuOpen);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 테스트용 F9: 엔딩 직전으로 건너뛰기. 남은 음식을 전부 먹어(행렬 생김) 마지막 1CM를 나오게 하고,
    /// 고양이를 그 옆에 세운다. E로 마지막 1CM를 먹으면 엔딩 연출이 시작된다. Unity 에디터에서만 동작한다.
    /// </summary>
    void DebugSkipToEnding()
    {
        if (won || lastCollected || !player) return;
        if (menuOpen) SetMenu(false);
        if (onTitle) BeginGame();

        foreach (var f in new System.Collections.Generic.List<Food>(Food.All))
            if (f && f.CanBeEaten && f.type != FoodType.LastPiece) f.Eat(player);
        if (rawCm < Goal - 1) { rawCm = Goal - 1; player.SetProgress(Progress); }
        CheckLastPiece();

        Food last = null;
        foreach (var f in Food.All) if (f && f.type == FoodType.LastPiece) last = f;
        Vector3 lp = last ? last.transform.position : lastPieceSpawn ? lastPieceSpawn.position : Vector3.zero;
        Vector3 back = new Vector3(lp.x, 0f, lp.z).normalized;   // 섬 중심 쪽으로 1.6m 물러선 자리
        if (back.sqrMagnitude < 0.01f) back = Vector3.back;
        Vector3 pos = lp - back * 1.6f + Vector3.up * 0.2f;
        var rb = player.GetComponent<Rigidbody>();
        if (rb) { rb.position = pos; rb.linearVelocity = Vector3.zero; }
        player.transform.position = pos;
        player.visual.yaw = Mathf.Atan2(back.x, back.z) * Mathf.Rad2Deg;
        var cf = FindAnyObjectByType<CameraFollow>();
        if (cf) cf.yaw = player.visual.yaw;
        Debug.Log("[F9] 엔딩 직전 — E로 마지막 1CM(무지개 고양이)를 먹으면 엔딩 연출 시작");
    }
#endif

    public void SetMenu(bool open)
    {
        menuOpen = open;
        Time.timeScale = open ? 0f : 1f;
        CameraFollow.SetCursorLocked(!open);
    }

    // ---------------- Progress ----------------
    public void OnFoodEaten(Food f, CatPlayer p)
    {
        if (f.type == FoodType.LastPiece)
        {
            lastCollected = true;
            if (exitGate) exitGate.Open();
            p.SetProgress(1f);
            p.Emote(3);
            StartCoroutine(EndingSequence.Play(this, p));   // 출구까지 자동으로 달려가 합체 → 무지개 고양이 → 클리어
            return;
        }

        rawCm += f.cm;
        p.AddFollower(f.cm, f.type);
        p.SetProgress(Progress);

        int stage = Mathf.FloorToInt(Progress * 4f);
        if (stage > lastStage)
        {
            lastStage = stage;
            p.Emote(3);   // 색이 한 단계 돌아올 때 빙글
        }
        CheckLastPiece();
    }

    void CheckLastPiece()
    {
        if (lastSpawned || rawCm < Goal - 1) return;
        lastSpawned = true;
        Vector3 pos = lastPieceSpawn ? lastPieceSpawn.position : Vector3.zero;
        Food.Create(FoodType.LastPiece, pos, foodRoot, false);
        CameraFollow.Shake(0.2f);
    }

    // ---------------- Obstacles ----------------
    public void OnKittensLost(int cm, int count, CatPlayer p)
    {
        CameraFollow.Shake(0.35f);
        GameAudio.Scatter();
        if (count == 0) return;
        rawCm = Mathf.Max(0, rawCm - cm);
        p.SetProgress(Progress);
    }

    public void OnSwept(CatPlayer p)
    {
        CameraFollow.Shake(0.3f);
        GameAudio.Scatter();
    }

    public void OnKittenRejoined(Follower f, CatPlayer p)
    {
        rawCm += f.cm;
        p.SetProgress(Progress);
        CheckLastPiece();
    }

    public void TryExit()
    {
        if (won || !lastCollected) return;
        won = true;
        endTime = Time.time;
        player.InputEnabled = false;
        player.StartCelebration();
        CameraFollow.SetCursorLocked(false);
    }

    /// <summary>다시 시작. toTitle = true면 시작 화면부터, 아니면 바로 플레이.</summary>
    public void Restart() => Restart(false);

    public void Restart(bool toTitle)
    {
        skipTitle = !toTitle;
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.buildIndex);
#endif
    }
}
