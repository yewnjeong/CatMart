using UnityEngine;

/// <summary>
/// 마트 지붕 장식(빵·과자)이 통통 튀는 모션. 웅크림 → 점프(늘어남) → 착지(찌그러짐, 출렁) → 쉼.
/// 모델 바닥 중앙을 기준으로 월드 위 방향으로 늘이고 줄인다 (FBX 프리팹 안이라 부모를 바꾸지 않고 직접 계산).
/// </summary>
public class PropBounce : MonoBehaviour
{
    public float height = 0.45f;
    public float period = 1.6f;
    [Range(0f, 1f)] public float phase;
    public float squash = 0.16f;
    public float tilt = 6f;

    Vector3 pivot, basePos, baseScale, tiltAxis;
    Quaternion baseRot;
    int upAxis;
    bool ready;

    void Start() => Init();

    /// <summary>현재 자세를 기준 자세로 기록한다.</summary>
    public void Init()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { enabled = false; return; }
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        pivot = new Vector3(b.center.x, b.min.y, b.center.z);
        basePos = transform.position;
        baseRot = transform.rotation;
        baseScale = transform.localScale;

        // 월드 위쪽과 가장 가까운 로컬 축 (블렌더 FBX는 보통 로컬 Z가 위)
        var up = Quaternion.Inverse(baseRot) * Vector3.up;
        upAxis = Mathf.Abs(up.x) > Mathf.Abs(up.y) ? (Mathf.Abs(up.x) > Mathf.Abs(up.z) ? 0 : 2) : (Mathf.Abs(up.y) > Mathf.Abs(up.z) ? 1 : 2);

        float a = (phase * 360f + 37f) * Mathf.Deg2Rad;
        tiltAxis = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        ready = true;
    }

    void Update()
    {
        if (ready) Apply(Mathf.Repeat(Time.time / period + phase, 1f));
    }

    /// <summary>한 주기 안의 위치 c(0~1)에 맞는 자세를 적용한다.</summary>
    public void Apply(float c)
    {
        float y = 0f, sy = 1f, rot = 0f;

        if (c < 0.12f)
        {
            // 웅크림
            float u = c / 0.12f;
            sy = 1f - squash * 0.6f * Mathf.Sin(u * Mathf.PI * 0.5f);
        }
        else if (c < 0.52f)
        {
            // 공중: 이륙·착지 직전엔 늘어나고 꼭대기에선 원래 모양
            float u = (c - 0.12f) / 0.40f;
            y = height * 4f * u * (1f - u);
            sy = 1f + squash * 0.5f * Mathf.Abs(1f - 2f * u);
            rot = tilt * Mathf.Sin(u * Mathf.PI * 2f);
        }
        else if (c < 0.82f)
        {
            // 착지: 찌그러졌다가 출렁이며 복귀
            float u = (c - 0.52f) / 0.30f;
            sy = 1f - squash * Mathf.Exp(-4f * u) * Mathf.Cos(u * Mathf.PI * 3f);
        }

        float sxz = 1f / Mathf.Sqrt(sy);
        var q = Quaternion.AngleAxis(rot, tiltAxis);
        var d = basePos - pivot;
        d = new Vector3(d.x * sxz, d.y * sy, d.z * sxz);
        transform.SetPositionAndRotation(pivot + Vector3.up * y + q * d, q * baseRot);

        var s = baseScale * sxz;
        s[upAxis] = baseScale[upAxis] * sy;
        transform.localScale = s;
    }
}
