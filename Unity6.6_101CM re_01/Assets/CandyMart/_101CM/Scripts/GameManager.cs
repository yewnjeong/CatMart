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
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (GameFlowUI.I && GameFlowUI.I.CloseGuide()) return;   // 게임 설명이 떠 있으면 그것부터 닫기
        if (!won && !onTitle) SetMenu(!menuOpen);
    }

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
        if (count == 0) return;
        rawCm = Mathf.Max(0, rawCm - cm);
        p.SetProgress(Progress);
    }

    public void OnSwept(CatPlayer p) => CameraFollow.Shake(0.3f);

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
