using System.Collections;
using UnityEngine;

/// <summary>
/// 엔딩 연출 (마지막 1CM를 먹으면 GameManager가 시작).
///  1. 조작이 꺼지고, 행렬이 ♥(감정표현 1) 콩콩 뛰며 출구 앞까지 자동으로 달려간다.
///  2. 출구 바로 앞에서 고양이들이 차례로 주인공 몸속으로 합쳐진다.
///  3. 반짝 → 무지개 젤리 고양이로 변신.
///  4. 출구를 지나 다리 끝(ExitZone)까지 걸어가면 기존 클리어(축하 모션 → 클리어 화면)로 이어진다.
/// 중간에 클리어가 되면(테스트 순간이동 등) 바로 멈춘다.
/// </summary>
public static class EndingSequence
{
    public static bool Running { get; private set; }
    /// <summary>씬을 다시 불러오면 코루틴이 끊기므로 상태 초기화 (Domain Reload 꺼져 있어 static이 남음).</summary>
    public static void ResetState() => Running = false;

    const float MergeBeforeGate = 2.2f;   // 게이트 몇 m 앞에서 합쳐질지
    const float HeartInterval = 0.85f;    // 달리는 동안 ♥ 감정표현 간격

    public static IEnumerator Play(GameManager gm, CatPlayer p)
    {
        if (Running || !p) yield break;
        Running = true;
        p.InputEnabled = false;
        var cf = Object.FindAnyObjectByType<CameraFollow>();

        yield return new WaitForSeconds(0.9f);   // 마지막 1CM 먹고 빙글 + 출구 열리기 시작

        var zone = Object.FindAnyObjectByType<ExitZone>();
        if (!zone) { Stop(p); yield break; }
        var zc = zone.GetComponent<Collider>();
        Vector3 zoneP = Ground(zc ? zc.bounds.center : zone.transform.position);
        Vector3 gateP = gm.exitGate ? Ground(gm.exitGate.transform.position) : zoneP;
        Vector3 outDir = Flat(zoneP - gateP);
        if (outDir.sqrMagnitude < 0.01f) outDir = Flat(zoneP);
        outDir.Normalize();
        Vector3 mergeP = Ground(gateP - outDir * MergeBeforeGate);
        ClearObstacles(gm, p.transform.position, mergeP, zoneP);

        // 1) ♥ 콩콩 뛰며 출구 앞까지
        float nextHeart = 0f;
        yield return RunTo(gm, p, cf, mergeP, 1f, () =>
        {
            if (Time.time < nextHeart) return;
            nextHeart = Time.time + HeartInterval;
            p.Emote(0);
        });
        if (gm.IsWon) { Stop(p); yield break; }

        // 2) 출구 쪽을 보고 서서 합체
        p.AutoMove = null;
        p.visual.yaw = Mathf.Atan2(outDir.x, outDir.z) * Mathf.Rad2Deg;
        yield return new WaitForSeconds(0.35f);
        yield return p.MergeFollowers();
        if (gm.IsWon) { Stop(p); yield break; }

        // 3) 무지개 고양이로 변신
        CameraFollow.Shake(0.25f);
        GameAudio.ExitOpen();
        p.BecomeRainbow();
        yield return new WaitForSeconds(1.3f);

        // 4) 출구를 지나 다리 끝까지 (ExitZone에 닿으면 GameManager.TryExit → 클리어)
        yield return RunTo(gm, p, cf, zoneP + outDir * 0.6f, 0.65f, null);
        Stop(p);
    }

    static IEnumerator RunTo(GameManager gm, CatPlayer p, CameraFollow cf, Vector3 target, float speed01, System.Action tick)
    {
        float timeout = Time.time + 10f, checkAt = Time.time + 0.8f;
        Vector3 lastPos = p.transform.position;
        while (!gm.IsWon && Time.time < timeout)
        {
            Vector3 to = Flat(target - p.transform.position);
            if (to.magnitude < 0.35f) break;
            Vector3 dir = to.normalized;
            if (!gm.MenuOpen)
            {
                p.AutoMove = dir * speed01;
                if (cf) cf.yaw = Mathf.LerpAngle(cf.yaw, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 1f - Mathf.Exp(-3f * Time.deltaTime));
                tick?.Invoke();
            }
            // 무언가에 걸려 제자리면 살짝 밀어 줌
            if (Time.time > checkAt)
            {
                if (Flat(p.transform.position - lastPos).magnitude < 0.15f)
                {
                    var rb = p.GetComponent<Rigidbody>();
                    Vector3 step = p.transform.position + dir * Mathf.Min(0.6f, to.magnitude) + Vector3.up * 0.05f;
                    if (rb) rb.position = step; else p.transform.position = step;
                }
                lastPos = p.transform.position;
                checkAt = Time.time + 0.8f;
            }
            yield return null;
        }
        p.AutoMove = null;
    }

    const float ClearRadius = 6f;   // 달려가는 길에서 이 거리 안의 카트·빗자루는 톡 사라짐

    /// <summary>
    /// 엔딩 동안 카트·빗자루는 모두 멈추고(부딪혀도 흩어지지 않음),
    /// 달려가는 길(현재 위치 → 합체 지점 → 출구) 가까이 있는 것은 작아지며 사라져 길을 비켜 준다.
    /// </summary>
    static void ClearObstacles(GameManager gm, Vector3 a, Vector3 b, Vector3 c)
    {
        foreach (var cart in Object.FindObjectsByType<ShoppingCart>())
        {
            cart.enabled = false;
            if (NearPath(cart.transform.position, a, b, c)) gm.StartCoroutine(PopAway(cart.gameObject));
        }
        foreach (var broom in Object.FindObjectsByType<MagicBroom>())
        {
            broom.enabled = false;
            if (NearPath(broom.transform.position, a, b, c)) gm.StartCoroutine(PopAway(broom.gameObject));
        }
        // 세워 둔 장식 카트(스크립트 없음)도 길목에 있으면 행렬이 걸리므로 같이 치움
        var popped = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var col in Object.FindObjectsByType<Collider>())
        {
            if (col.isTrigger || col.GetComponentInParent<ShoppingCart>() || col.GetComponentInParent<Follower>()) continue;
            var root = CartRoot(col.transform);
            if (!root || popped.Contains(root.gameObject)) continue;
            if (!NearPath(col.bounds.center, a, b, c)) continue;
            popped.Add(root.gameObject);
            gm.StartCoroutine(PopAway(root.gameObject));
        }
    }

    /// <summary>이름에 Cart가 들어간 가장 바깥 부모 (Cart.fbx 조각들을 한 번에 치우기 위해).</summary>
    static Transform CartRoot(Transform t)
    {
        Transform found = null;
        for (var x = t; x; x = x.parent)
            if (x.name.IndexOf("Cart", System.StringComparison.OrdinalIgnoreCase) >= 0) found = x;
        return found;
    }

    static bool NearPath(Vector3 p, Vector3 a, Vector3 b, Vector3 c) =>
        SegDist(p, a, b) < ClearRadius || SegDist(p, b, c) < ClearRadius;

    static float SegDist(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = Flat(b - a), ap = Flat(p - a);
        float t = ab.sqrMagnitude > 1e-4f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude) : 0f;
        return (ap - ab * t).magnitude;
    }

    static IEnumerator PopAway(GameObject go)
    {
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
        Vector3 s0 = go.transform.localScale;
        float t = 0f;
        const float dur = 0.35f;
        while (t < dur && go)
        {
            t += Time.deltaTime;
            float n = t / dur;
            float k = n < 0.3f ? 1f + 0.15f * (n / 0.3f) : 1.15f * (1f - (n - 0.3f) / 0.7f);   // 살짝 부풀었다가 쏙
            go.transform.localScale = s0 * Mathf.Max(0f, k);
            yield return null;
        }
        if (go) go.SetActive(false);
    }

    static void Stop(CatPlayer p)
    {
        if (p) p.AutoMove = null;
        Running = false;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    static Vector3 Ground(Vector3 p)
    {
        var from = new Vector3(p.x, p.y + 4f, p.z);
        return Physics.Raycast(from, Vector3.down, out var hit, 12f, ~(1 << 2), QueryTriggerInteraction.Ignore) ? hit.point : p;
    }
}
