#if UNITY_EDITOR   // 에디터 자동 테스트 2 전용 (메뉴: Editor/AutoPlayTest.cs) — 빌드에는 안 들어감
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자동 테스트 2: 음식이 걸어서/점프로 닿는지, 카트·빗자루, 흩어진 고양이 복구(E로 합류 / 음식으로 되돌아감),
/// Esc 화면들, 영어 글자, 다시 시작 / 홈, 테스트 내내 콘솔 에러·경고.
/// </summary>
public class AutoPlayTest2 : MonoBehaviour
{
    readonly StringBuilder log = new StringBuilder();
    readonly List<string> consoleIssues = new List<string>();
    int fails;

    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    void Check(bool ok, string what)
    {
        if (!ok) fails++;
        string line = (ok ? "PASS  " : "FAIL  ") + what;
        log.AppendLine(line);
        if (ok) Debug.Log("[AutoTest] " + line); else Debug.LogError("[AutoTest] " + line);
    }

    void Info(string what)
    {
        log.AppendLine("      " + what);
        Debug.Log("[AutoTest] " + what);
    }

    void OnEnable() => Application.logMessageReceived += OnLog;
    void OnDisable() => Application.logMessageReceived -= OnLog;

    void OnLog(string msg, string stack, LogType type)
    {
        if (msg.StartsWith("[AutoTest]")) return;
        if (type == LogType.Log) return;
        string first = msg.Split('\n')[0];
        if (consoleIssues.Count < 30) consoleIssues.Add($"{type}: {first}");
    }

    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);   // 다시 시작(씬 재로드) 뒤에도 계속
        yield return new WaitForSeconds(0.5f);
        var gm = GameManager.I;
        var p = gm.player;
        var startBtn = GameObject.Find("GameStart");
        if (startBtn) startBtn.GetComponent<Button>().onClick.Invoke(); else gm.BeginGame();
        yield return new WaitForSeconds(0.3f);

        // ---------- 1. 음식에 닿을 수 있는지 (걸어서 / 점프로) ----------
        float g = -Physics.gravity.y;
        float apex = p.jumpVelocity * p.jumpVelocity / (2f * g);
        Info($"점프 높이 {apex:0.00}m, 먹기 거리 {p.eatRange + 0.35f:0.00}m, 높이 허용 ±{p.eatHeightTolerance:0.00}m");
        var placedTypes = new List<string>();
        foreach (var f in new List<Food>(Food.All))
        {
            if (!f || f.physical || f.type == FoodType.LastPiece) continue;
            placedTypes.Add(f.type.ToString());
            string how = Reach(f, p, apex, out bool ok);
            Check(ok, $"닿음: {f.name} {f.transform.position.ToString("F1")} — {how}");
        }

        // ---------- 2. 영어 글자에 한글이 남았는지 ----------
        var ui = GameFlowUI.I;
        bool wasEn = GameFlowUI.English;
        Call(ui, "SetLanguage", true);
        var hangul = new List<string>();
        foreach (var t in ui.GetComponentsInChildren<Text>(true))
            foreach (char c in t.text) if (c >= 0xAC00 && c <= 0xD7A3) { hangul.Add($"{t.name}=\"{t.text}\""); break; }
        Check(hangul.Count == 0, "영어 모드 한글 없음" + (hangul.Count > 0 ? ": " + string.Join(", ", hangul) : ""));
        Call(ui, "SetLanguage", false);
        var empty = new List<string>();
        foreach (var t in ui.GetComponentsInChildren<Text>(true)) if (string.IsNullOrWhiteSpace(t.text) && t.name != "ClearTime") empty.Add(t.name);
        Check(empty.Count == 0, "한국어 모드 빈 글자 없음" + (empty.Count > 0 ? ": " + string.Join(", ", empty) : ""));
        Call(ui, "SetLanguage", wasEn);

        // ---------- 3. Esc 화면들 ----------
        gm.SetMenu(true);
        yield return null; yield return null;
        Check(Active("Menu"), "Esc → 메뉴 표시");
        Press("HowTo"); yield return null; yield return null;
        Check(Active("Guide") && !Active("Menu"), "HOW TO PLAY → 설명 표시, 메뉴 숨김");
        ui.CloseGuide(); yield return null; yield return null;
        Check(!Active("Guide") && Active("Menu"), "설명 닫기 → 메뉴로 돌아옴");
        Press("Settings"); yield return null; yield return null;
        Check(SettingsActive() && !Active("Menu"), "SETTINGS → 설정 표시, 메뉴 숨김");
        ui.CloseGuide(); yield return null; yield return null;
        Check(!SettingsActive() && Active("Menu"), "설정 닫기 → 메뉴로 돌아옴");
        Press("Resume"); yield return null; yield return null;
        Check(!gm.MenuOpen && Time.timeScale == 1f && !Active("Menu"), "RESUME → 게임 재개 (timeScale 1)");

        // ---------- 4. 카트에 치임 → 흩어짐 → E로 합류 / 음식으로 되돌아감 ----------
        foreach (var c in FindObjectsByType<ShoppingCart>()) c.gameObject.SetActive(false);   // 우연히 또 치이지 않게
        foreach (var b in FindObjectsByType<MagicBroom>()) b.gameObject.SetActive(false);
        var eatList = new List<Food>();
        foreach (var f in Food.All) if (f && !f.physical && f.type != FoodType.LastPiece) eatList.Add(f);
        for (int i = 0; i < 6 && i < eatList.Count; i++)
        {
            Teleport(p, eatList[i].transform.position);
            yield return new WaitForSeconds(0.12f);
            eatList[i].Eat(p);
        }
        Teleport(p, new Vector3(0f, 0.2f, -12f));   // 넓은 광장 앞
        yield return new WaitForSeconds(0.6f);
        int cm0 = gm.CM, n0 = p.FollowerCount;
        Info($"치이기 전 {cm0}cm, 고양이 {n0}마리");
        p.HitByCart(p.transform.position + Vector3.forward * 0.6f, Vector3.back, null);
        yield return null;
        int lostCount = LostKitten.All.Count;
        Check(lostCount > 0 && p.FollowerCount == n0 - lostCount, $"카트 → 고양이 {lostCount}마리 흩어짐 (남은 {p.FollowerCount})");
        int lostCm = cm0 - gm.CM;
        Check(lostCm > 0, $"CM 감소 {cm0} → {gm.CM}");
        yield return new WaitForSeconds(0.75f);   // 착지

        // 하나는 E로 데려오기 (실제 키 처리 함수 TryEat)
        LostKitten target = null;
        foreach (var k in LostKitten.All) if (k && k.CanRejoin) { target = k; break; }
        Check(target, "착지한 고양이 있음");
        if (target)
        {
            int before = gm.CM;
            Teleport(p, target.transform.position + Vector3.right * 0.6f);
            yield return null; yield return null;
            Check(p.NearbyKitten == target, "가까이 가면 흩어진 고양이 강조됨");
            Call(p, "TryEat");
            yield return null;
            Check(gm.CM > before, $"E → 고양이 합류, CM {before} → {gm.CM}");
        }
        // 나머지는 1초 뒤 음식으로 되돌아감
        var before2 = new HashSet<Food>(Food.All);
        yield return new WaitForSeconds(LostKitten.RevertDelay + 0.6f);
        var reverted = new List<Food>();
        foreach (var f in Food.All) if (f && !before2.Contains(f)) reverted.Add(f);
        Check(LostKitten.All.Count == 0, $"남은 흩어진 고양이 {LostKitten.All.Count}마리 → 전부 음식으로 돌아감 ({reverted.Count}개)");
        int sumBack = 0;
        foreach (var f in reverted)
        {
            sumBack += f.cm;
            var pos = f.transform.position;
            bool onIsland = new Vector2(pos.x, pos.z).magnitude < 20f && pos.y > -0.5f;
            bool covered = Physics.Raycast(f.FocusPoint, Vector3.up, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            Check(onIsland && !covered, $"되돌아간 음식 {f.name} {pos.ToString("F1")} 섬 위·안 묻힘");
        }
        foreach (var f in reverted)
        {
            if (!f) continue;
            Teleport(p, f.transform.position);
            yield return new WaitForSeconds(0.12f);
            f.Eat(p);
        }
        yield return new WaitForSeconds(0.2f);
        Check(gm.CM == cm0, $"흩어진 CM 전부 복구 ({gm.CM} == {cm0})");

        // ---------- 5. 빗자루 ----------
        yield return new WaitForSeconds(2f);   // 무적 시간 지나기
        Teleport(p, new Vector3(0f, 0.2f, -12f));
        yield return new WaitForSeconds(0.3f);
        int cmB = gm.CM, nB = p.FollowerCount;
        p.SweptByBroom(p.transform.position + Vector3.left, Vector3.right);
        yield return new WaitForSeconds(3f);
        var pp = p.transform.position;
        Check(pp.y > -1f && new Vector2(pp.x, pp.z).magnitude < 20.5f, $"빗자루 → 섬 안에 착지 {pp.ToString("F1")}");
        Check(gm.CM == cmB && p.FollowerCount == nB, "빗자루는 CM·고양이 손실 없음");

        // ---------- 6. 다시 시작 (Esc RETRY) ----------
        gm.Restart(false);
        yield return new WaitForSeconds(1.5f);
        gm = GameManager.I;
        int placed = 0; var newTypes = new List<string>();
        foreach (var f in Food.All) if (f && !f.physical && f.type != FoodType.LastPiece) { placed++; newTypes.Add(f.type.ToString()); }
        Check(gm && !gm.OnTitle && gm.CM == 0 && Time.timeScale == 1f, "RETRY → 바로 플레이, 0cm");
        Check(placed == placedTypes.Count, $"RETRY → 음식 {placed}개 다시 배치");
        int changed = 0; for (int i = 0; i < Mathf.Min(placedTypes.Count, newTypes.Count); i++) if (placedTypes[i] != newTypes[i]) changed++;
        Info($"음식 종류 랜덤: {changed}/{placed}개 바뀜 ({string.Join(",", newTypes)})");
        Check(MusicName().Contains("Play"), $"RETRY → 인게임 BGM ({MusicName()})");

        // ---------- 7. 홈 ----------
        gm.Restart(true);
        yield return new WaitForSeconds(1.5f);
        gm = GameManager.I;
        Check(gm && gm.OnTitle && Active("Title"), "HOME → 시작 화면");
        Check(MusicName().Contains("Title"), $"HOME → 처음 BGM ({MusicName()})");

        // ---------- 8. 콘솔 ----------
        Check(consoleIssues.Count == 0, $"테스트 중 콘솔 에러·경고 {consoleIssues.Count}개");
        foreach (var s in consoleIssues) Info(s);
        Finish();
    }

    /// <summary>음식 둘레를 돌며, 서서 또는 점프해서 먹을 수 있는 자리가 있는지 찾는다.</summary>
    static string Reach(Food f, CatPlayer p, float apex, out bool ok)
    {
        Vector3 fp = f.FocusPoint;
        float tol = p.eatHeightTolerance, maxD = p.eatRange + 0.35f - 0.05f;
        string best = "먹을 자리 없음";
        ok = false;
        for (float r = 0.3f; r <= maxD; r += 0.25f)
            for (int a = 0; a < 16; a++)
            {
                Vector3 dir = Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward;
                Vector3 q = fp + dir * r;
                var hits = Physics.RaycastAll(new Vector3(q.x, fp.y + 2.5f, q.z), Vector3.down, 6f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.normal.y < 0.7f) continue;
                    if (h.collider.GetComponentInParent<Food>()) continue;
                    float sy = h.point.y;
                    // 고양이(반지름 약 0.35)가 설 공간이 있는지
                    if (Physics.CheckSphere(h.point + Vector3.up * 0.45f, 0.3f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                    float stand = sy + 0.35f;
                    bool standEat = Mathf.Abs(fp.y - stand) <= tol;
                    bool jumpEat = fp.y >= stand - tol && fp.y <= stand + apex + tol;
                    if (!standEat && !jumpEat) continue;
                    bool fromGround = sy <= apex + 0.05f;   // 땅에서 점프로 올라설 수 있는 높이
                    string s = $"{(standEat ? "서서" : "점프로")} 먹음 (발판 {h.collider.name} 높이 {sy:0.00}, 거리 {r:0.0})";
                    if (fromGround) { ok = true; return s; }
                    best = s + " — 발판이 높아 올라갈 길 확인 필요";
                }
            }
        return best;
    }

    static void Call(object o, string method, params object[] args)
    {
        var m = o.GetType().GetMethod(method, Any);
        m.Invoke(o, args.Length > 0 ? args : null);
    }

    static bool Active(string name)
    {
        var ui = GameFlowUI.I;
        foreach (Transform t in ui.GetComponentsInChildren<Transform>(true))
            if (t.name == name && t.parent && t.parent.name == "FlowCanvas") return t.gameObject.activeInHierarchy;
        return false;
    }

    static bool SettingsActive()
    {
        foreach (var s in FindObjectsByType<VolumeSlider>(FindObjectsInactive.Include)) return s.gameObject.activeInHierarchy;
        return false;
    }

    static void Press(string name)
    {
        var ui = GameFlowUI.I;
        foreach (var b in ui.GetComponentsInChildren<Button>(true))
            if (b.name == name && b.gameObject.activeInHierarchy) { b.onClick.Invoke(); return; }
        Debug.LogWarning("[AutoTest] 버튼 없음: " + name);
    }

    static string MusicName()
    {
        var a = GameAudio.I;
        if (!a) return "(GameAudio 없음)";
        var music = (AudioSource[])typeof(GameAudio).GetField("music", Any).GetValue(a);
        int cur = (int)typeof(GameAudio).GetField("cur", Any).GetValue(a);
        return music[cur] && music[cur].clip ? music[cur].clip.name : "(없음)";
    }

    static void Teleport(CatPlayer p, Vector3 pos)
    {
        var rb = p.GetComponent<Rigidbody>();
        var to = pos + Vector3.up * 0.1f;
        if (rb) { rb.position = to; rb.linearVelocity = Vector3.zero; }
        p.transform.position = to;
    }

    void Finish()
    {
        string head = fails == 0 ? "[AutoTest] 테스트 2 전부 통과" : $"[AutoTest] 테스트 2 실패 {fails}개";
        AutoPlayTest.LastReport = head + "\n" + log;
        if (fails == 0) Debug.Log(head + "\n" + log); else Debug.LogError(head + "\n" + log);
        Destroy(gameObject);
    }
}
#endif
