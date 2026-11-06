using System.Collections;
using UnityEngine;

/// <summary>
/// 시작 연출 (GAME START를 누르면 GameManager가 시작).
/// 고양이가 출발 다리 바깥쪽에서 ♥ 콩콩 신나게 뛰어 들어와 원래 시작 자리에 도착 → 빙글 → 조작 시작.
/// 카메라가 시작 화면 시점에서 내려오는 1.2초와 겹쳐서 진행된다.
/// 도중에 순간이동(테스트 등)이 감지되면 바로 멈추고 조작을 돌려준다.
/// </summary>
public static class StartSequence
{
    public static bool Running { get; private set; }
    public static void ResetState() => Running = false;

    const float RunInDistance = 5f;   // 시작 자리에서 몇 m 바깥에서 뛰어 들어올지
    const float HeartInterval = 0.6f;

    public static IEnumerator Play(GameManager gm, CatPlayer p)
    {
        if (Running || !p) yield break;
        Running = true;
        p.InputEnabled = false;

        Vector3 spawn = p.transform.position;
        Vector3 inDir = new Vector3(-spawn.x, 0f, -spawn.z);   // 섬 중심 쪽
        inDir = inDir.sqrMagnitude > 0.01f ? inDir.normalized : Vector3.forward;
        Vector3 start = FindStart(spawn, inDir);

        var rb = p.GetComponent<Rigidbody>();
        if (rb) { rb.position = start; rb.linearVelocity = Vector3.zero; }
        p.transform.position = start;
        float yaw = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
        p.visual.yaw = yaw;

        yield return new WaitForSeconds(0.25f);
        p.Emote(1);   // ! 깜짝 → 신나서 출발
        yield return new WaitForSeconds(0.35f);

        float nextHeart = 0f, timeout = Time.time + 6f;
        Vector3 last = p.transform.position;
        while (Time.time < timeout)
        {
            Vector3 now = p.transform.position;
            if ((now - last).sqrMagnitude > 2f * 2f) break;   // 순간이동됨 → 연출 중단
            last = now;
            Vector3 to = spawn - now; to.y = 0f;
            if (to.magnitude < 0.3f) break;
            if (!gm.MenuOpen)
            {
                p.AutoMove = to.normalized;
                if (Time.time >= nextHeart) { nextHeart = Time.time + HeartInterval; p.Emote(0); }
            }
            yield return null;
        }
        p.AutoMove = null;
        if (rb) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        p.Emote(3);   // 도착하면 빙글
        yield return new WaitForSeconds(0.5f);
        p.InputEnabled = true;
        Running = false;
    }

    /// <summary>시작 자리에서 바깥쪽으로 가능한 한 멀리(최대 RunInDistance) 땅이 이어진 지점.</summary>
    static Vector3 FindStart(Vector3 spawn, Vector3 inDir)
    {
        for (float d = RunInDistance; d >= 1.5f; d -= 0.5f)
        {
            Vector3 c = spawn - inDir * d;
            if (!Physics.Raycast(c + Vector3.up * 3f, Vector3.down, out var hit, 6f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(hit.point.y - spawn.y) > 0.8f) continue;
            // 사이에 막힌 게 없어야 함 (울타리·게이트 기둥)
            Vector3 from = hit.point + Vector3.up * 0.5f, to = spawn + Vector3.up * 0.5f;
            if (Physics.SphereCast(from, 0.3f, (to - from).normalized, out _, (to - from).magnitude, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
            return hit.point + Vector3.up * 0.05f;
        }
        return spawn;
    }
}
