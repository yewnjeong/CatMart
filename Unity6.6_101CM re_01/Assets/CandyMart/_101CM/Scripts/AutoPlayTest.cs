#if UNITY_EDITOR   // 에디터 자동 플레이 테스트 전용 (메뉴: Editor/AutoPlayTest.cs) — 빌드에는 안 들어감
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class AutoPlayTest : MonoBehaviour
{
    /// <summary>마지막 테스트 결과 전문 (콘솔이 지워져도 읽을 수 있게).</summary>
    public static string LastReport = "";
    readonly StringBuilder log = new StringBuilder();
    int fails;

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

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        var gm = GameManager.I;
        Check(gm && gm.player, "GameManager / 플레이어 있음");
        if (!gm || !gm.player) { Finish(); yield break; }
        var p = gm.player;

        // ---- 리소스 ----
        var skin = Resources.Load<FigmaUISkin>("FigmaUISkin");
        var missing = new List<string>();
        foreach (var n in new[] { "H_GaugeBG", "H_GaugeFill", "H_GaugeBorder", "H_Ears", "T_LangKOR", "T_LangENG",
                                  "S_Track", "S_Fill", "S_Knob", "C_BubbleKO", "C_BubbleEN" })
        {
            bool found = false;
            if (skin) foreach (var s in skin.sprites) if (s && s.name == n) { found = true; break; }
            if (!found) missing.Add(n);
        }
        Check(missing.Count == 0, "UI 스프라이트" + (missing.Count > 0 ? " 없음: " + string.Join(", ", missing) : " 전부 있음"));
        var sounds = new[] { "BGM_Title_1", "BGM_Play_1", "BGM_Ending_1", "SFX_UIClick", "SFX_Pop_2", "SFX_Pop_3", "SFX_Jump", "SFX_Scatter", "SFX_BoxPop", "SFX_LastAppear" };
        var noSound = new List<string>();
        foreach (var n in sounds) if (!Resources.Load<AudioClip>("Sound/" + n)) noSound.Add(n);
        Check(noSound.Count == 0, "사운드" + (noSound.Count > 0 ? " 없음: " + string.Join(", ", noSound) : " 전부 있음"));
        Check(GameAudio.I, "GameAudio 자동 생성됨");

        // ---- 시작 화면 → 게임 시작 (실제 GAME START 버튼) ----
        Check(gm.OnTitle, "시작 화면에서 시작");
        var startBtn = GameObject.Find("GameStart");
        if (startBtn) startBtn.GetComponent<Button>().onClick.Invoke(); else gm.BeginGame();
        yield return new WaitForSeconds(0.3f);
        Check(!gm.OnTitle && gm.CanEat, "GAME START → 플레이 시작");

        // ---- Esc 메뉴 → SETTINGS ----
        gm.SetMenu(true);
        yield return null; yield return null;
        var settingsBtn = GameObject.Find("Settings");   // 메뉴 버튼 칸 이름 = key
        Check(settingsBtn && settingsBtn.GetComponent<Button>(), "Esc 메뉴에 SETTINGS 버튼");
        if (settingsBtn) settingsBtn.GetComponent<Button>().onClick.Invoke();
        yield return null;
        var sliders = FindObjectsByType<VolumeSlider>(FindObjectsInactive.Include);
        Check(sliders.Length == 2, $"설정 화면 슬라이더 {sliders.Length}개");
        if (sliders.Length == 2)
        {
            float before = GameAudio.BgmVolume;
            foreach (var s in sliders) if (s.name.StartsWith("Bgm")) s.SetValue(0.3f, true);
            Check(Mathf.Abs(GameAudio.BgmVolume - 0.3f) < 0.01f, "BGM 슬라이더 → 볼륨 반영");
            foreach (var s in sliders) if (s.name.StartsWith("Bgm")) s.SetValue(before, true);   // 원래 값으로
        }
        Check(GameFlowUI.I && GameFlowUI.I.CloseGuide(), "설정 닫기 (Esc와 같은 경로)");
        gm.SetMenu(false);
        yield return new WaitForSeconds(0.2f);

        // ---- 장애물 끄기 ----
        foreach (var c in FindObjectsByType<ShoppingCart>()) c.gameObject.SetActive(false);   // 꺼진 스크립트도 트리거는 받으므로 통째로
        foreach (var b in FindObjectsByType<MagicBroom>()) b.gameObject.SetActive(false);

        // ---- 배치된 음식 먹기 ----
        var placed = new List<Food>();
        foreach (var f in Food.All) if (f && !f.physical && f.type != FoodType.LastPiece) placed.Add(f);
        int sum = 0; foreach (var f in placed) sum += f.cm;
        Info($"배치 음식 {placed.Count}개, 합계 {sum}cm");
        int eaten = 0;
        foreach (var f in placed)
        {
            if (!f) continue;
            Teleport(p, f.transform.position);
            yield return new WaitForSeconds(0.15f);
            int cm0 = gm.CM;
            f.Eat(p);
            yield return new WaitForSeconds(0.1f);
            if (gm.CM > cm0) eaten++;
            else Info($"못 먹음: {f.name} at {f.transform.position}");
        }
        Check(eaten == placed.Count, $"배치 음식 {eaten}/{placed.Count}개 먹음 → {gm.CM}cm, 따라오는 고양이 {p.FollowerCount}");

        // ---- 박스 더미 무너뜨리기 ----
        var boxes = FindObjectsByType<SnackBoxStack>();
        int collapsed = 0;
        foreach (var box in boxes)
        {
            int phys0 = CountPhysical();
            var col = box.GetComponent<Collider>();
            Teleport(p, col ? col.bounds.center : box.transform.position);
            yield return new WaitForSeconds(0.4f);
            if (CountPhysical() > phys0) collapsed++;
            else Info($"안 무너짐: {box.name} (고양이 {p.FollowerCount}마리, 필요 {box.requiredFollowers})");
        }
        Check(collapsed == boxes.Length, $"박스 더미 {collapsed}/{boxes.Length}개 무너짐");
        yield return new WaitForSeconds(2.5f);   // 쏟아진 간식이 굴러가 멈출 때까지

        int spilled = 0, spilledEaten = 0;
        foreach (var f in new List<Food>(Food.All))
        {
            if (!f || !f.physical) continue;
            spilled++;
            Teleport(p, f.transform.position);
            yield return new WaitForSeconds(0.1f);
            int cm0 = gm.CM;
            f.Eat(p);
            yield return new WaitForSeconds(0.05f);
            if (gm.CM > cm0) spilledEaten++;
        }
        Info($"쏟아진 간식 {spilledEaten}/{spilled}개 먹음 → {gm.CM}cm");

        // ---- 마지막 1CM ----
        yield return new WaitForSeconds(0.3f);
        Food last = null;
        foreach (var f in Food.All) if (f && f.type == FoodType.LastPiece) last = f;
        Check(gm.CM >= GameManager.Goal - 1, $"100CM 도달 ({gm.CM}cm)");
        Check(last, "마지막 1CM(무지개 고양이) 등장");
        if (last)
        {
            Info($"마지막 1CM 위치 {last.transform.position}");
            // 묻힘 검사: 위로 무언가 덮여 있거나(분수 등) 몸통 높이가 콜라이더 안이면 실패
            var lp = last.transform.position;
            bool covered = Physics.Raycast(lp + Vector3.up * 0.2f, Vector3.up, out var cover, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            bool inside = Physics.CheckSphere(lp + Vector3.up * 0.5f, 0.3f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            Check(!covered && !inside, "마지막 1CM가 안 묻힘" + (covered ? $" (위에 {cover.collider.name})" : "") + (inside ? " (콜라이더 안)" : ""));
            Teleport(p, last.transform.position);
            yield return new WaitForSeconds(0.15f);
            last.Eat(p);
            yield return new WaitForSeconds(0.3f);
            Check(gm.CM == GameManager.Goal, $"101CM 달성 ({gm.CM}cm)");
            Check(gm.exitGate && gm.exitGate.IsOpen, "출구 열림");
            yield return new WaitForSeconds(1.5f);   // 문 내려가는 시간

            var zone = FindAnyObjectByType<ExitZone>();
            Check(zone, "ExitZone 있음");
            if (zone)
            {
                var zc = zone.GetComponent<Collider>();
                Teleport(p, zc ? zc.bounds.center : zone.transform.position);
                float t = 0f;
                while (!gm.IsWon && t < 2f) { t += Time.deltaTime; yield return null; }
                Check(gm.IsWon, "출구 도착 → 클리어");
                yield return new WaitForSeconds(2f);
                var clear = GameObject.Find("Clear");
                Check(clear && clear.activeInHierarchy, "클리어 화면 표시");
                Info($"기록 시간 {gm.RecordTime:0.0}s");
            }
        }
        Finish();
    }

    static int CountPhysical()
    {
        int n = 0;
        foreach (var f in Food.All) if (f && f.physical) n++;
        return n;
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
        string head = fails == 0 ? "[AutoTest] 전부 통과" : $"[AutoTest] 실패 {fails}개";
        LastReport = head + "\n" + log;
        if (fails == 0) Debug.Log(head + "\n" + log); else Debug.LogError(head + "\n" + log);
        Destroy(gameObject);
    }
}
#endif
