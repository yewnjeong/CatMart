using UnityEngine;

/// <summary>
/// BGM·효과음. 실행 시 자동으로 만들어지고 씬을 다시 불러와도 유지된다 (Resources/Sound).
/// BGM은 GameManager 상태를 보고 바꾼다: 시작 화면 = Title, 플레이 = Play, 클리어(축하 애니메이션 ~ 클리어 화면) = Ending.
/// 구간마다 곡이 2개씩 있어 하나가 끝나면 다른 곡으로 넘어간다. 구간이 바뀌면 크로스페이드.
/// 효과음: UI 버튼 = SFX_Pop_1, 음식 먹기 = SFX_Pop_2/3 중 랜덤 + 음높이 살짝 랜덤.
/// </summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio I { get; private set; }

    public float bgmVolume = 0.5f;
    public float sfxVolume = 0.8f;
    public float fadeTime = 0.8f;

    enum Phase { None, Title, Play, Ending }

    AudioClip[][] bgm;          // Phase 순서대로 (Title, Play, Ending)
    AudioClip uiClick;
    AudioClip[] eat;

    AudioSource[] music = new AudioSource[2];   // 크로스페이드용 두 개
    AudioSource uiSfx, eatSfx;  // 음높이를 따로 쓰므로 분리
    int cur;                    // 지금 들리는 music 인덱스
    Phase phase = Phase.None;
    int track;
    float nextAt;               // 지금 곡이 끝나는 시각 (unscaled)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I) return;
        var go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        go.AddComponent<GameAudio>();
    }

    void Awake()
    {
        I = this;
        bgm = new[]
        {
            Load("BGM_Title_1", "BGM_Title_2"),
            Load("BGM_Play_1", "BGM_Play_2"),
            Load("BGM_Ending_1", "BGM_Ending_2"),
        };
        uiClick = Resources.Load<AudioClip>("Sound/SFX_Pop_1");
        eat = Load("SFX_Pop_2", "SFX_Pop_3");

        for (int i = 0; i < 2; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].playOnAwake = false;
            music[i].volume = 0f;
        }
        uiSfx = gameObject.AddComponent<AudioSource>();
        uiSfx.playOnAwake = false;
        eatSfx = gameObject.AddComponent<AudioSource>();
        eatSfx.playOnAwake = false;
    }

    static AudioClip[] Load(params string[] names)
    {
        var clips = new AudioClip[names.Length];
        for (int i = 0; i < names.Length; i++) clips[i] = Resources.Load<AudioClip>("Sound/" + names[i]);
        return clips;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm)
        {
            var want = gm.OnTitle ? Phase.Title : gm.IsWon ? Phase.Ending : Phase.Play;
            if (want != phase) SetPhase(want);
        }

        // 크로스페이드 (Esc 메뉴는 timeScale 0이라 unscaled)
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime);
        for (int i = 0; i < 2; i++)
        {
            float target = i == cur ? bgmVolume : 0f;
            music[i].volume = Mathf.MoveTowards(music[i].volume, target, step * bgmVolume);
            if (i != cur && music[i].isPlaying && music[i].volume <= 0f) music[i].Stop();
        }

        // 곡이 끝나면 같은 구간의 다음 곡
        // (스트리밍 곡은 불러오는 동안 isPlaying이 잠깐 false일 수 있어 곡 길이만큼 지난 뒤에만 확인)
        if (phase != Phase.None && Time.unscaledTime >= nextAt && !music[cur].isPlaying) PlayTrack(track + 1, false);
    }

    void SetPhase(Phase p)
    {
        phase = p;
        PlayTrack(Random.Range(0, 2), true);
    }

    void PlayTrack(int t, bool crossfade)
    {
        var clips = bgm[(int)phase - 1];
        track = t % clips.Length;
        var clip = clips[track];
        if (!clip) return;
        if (crossfade) cur = 1 - cur;
        var src = music[cur];
        src.clip = clip;
        src.loop = false;
        if (!crossfade) src.volume = bgmVolume;
        src.Play();
        nextAt = Time.unscaledTime + clip.length;
    }

    // ---------------- 효과음 ----------------
    public static void UIClick()
    {
        if (!I || !I.uiClick) return;
        I.uiSfx.PlayOneShot(I.uiClick, I.sfxVolume);
    }

    public static void Eat()
    {
        if (!I || I.eat.Length == 0) return;
        var clip = I.eat[Random.Range(0, I.eat.Length)];
        if (!clip) return;
        I.eatSfx.pitch = Random.Range(0.95f, 1.12f);
        I.eatSfx.PlayOneShot(clip, I.sfxVolume);
    }
}
