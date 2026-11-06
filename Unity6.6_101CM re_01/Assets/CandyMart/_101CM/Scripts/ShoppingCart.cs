using UnityEngine;

/// <summary>
/// 정해진 경로를 따라 혼자 굴러다니는 쇼핑카트 (방해 요소).
/// 주인공이나 행렬에 부딪히면 행렬 끝 고양이들이 흩어진다.
/// loop=true: 경로를 계속 순환 / false: 끝에서 잠깐 멈췄다가 되돌아감.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShoppingCart : MonoBehaviour
{
    public Vector3[] waypoints;
    public bool loop = true;
    public float speed = 3.5f;
    public float pauseAtEnds = 0.8f;
    public int startIndex;
    public Transform visual;
    [Tooltip("바닥(광장 단차 등) 높이를 따라 오르내림. 바퀴가 바닥에 박히거나 뜨지 않게")]
    public bool followGround = true;

    Rigidbody rb;
    int target, dir = 1;
    float pauseUntil, rattle;
    float wheelOffset;   // 루트 → 바퀴 바닥까지의 높이 (Start에서 측정)
    float groundY;
    bool groundInit;
    Vector2 footprint;   // 바퀴 자리 반폭 (앞뒤, 좌우)
    static readonly RaycastHit[] hitBuf = new RaycastHit[16];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void MeasureWheels()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        wheelOffset = b.min.y - transform.position.y;
        var local = transform.InverseTransformVector(b.extents);
        footprint = new Vector2(Mathf.Abs(local.z) * 0.6f, Mathf.Abs(local.x) * 0.6f);
    }

    /// <summary>바퀴 네 자리 아래 바닥 중 가장 높은 곳 (자기 자신·트리거·고양이·굴러다니는 물체는 무시).</summary>
    float SampleGround(Vector3 pos, Quaternion rot)
    {
        float best = float.NegativeInfinity;
        Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right;
        for (int k = 0; k < 5; k++)
        {
            Vector3 off = k == 0 ? Vector3.zero : fwd * (k < 3 ? footprint.x : -footprint.x) + right * (k % 2 == 1 ? footprint.y : -footprint.y);
            Vector3 from = new Vector3(pos.x, pos.y + wheelOffset + 1.2f, pos.z) + off;
            int n = Physics.RaycastNonAlloc(from, Vector3.down, hitBuf, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hitBuf[i].collider;
                if (c.transform.IsChildOf(transform)) continue;
                if (c.attachedRigidbody && !c.attachedRigidbody.isKinematic) continue;
                if (c.attachedRigidbody && c.attachedRigidbody.GetComponent<ShoppingCart>()) continue;
                if (hitBuf[i].point.y > best) best = hitBuf[i].point.y;
            }
        }
        return best;
    }

    Vector3 OnGround(Vector3 pos, Quaternion rot)
    {
        if (!followGround) return pos;
        float g = SampleGround(pos, rot);
        if (float.IsNegativeInfinity(g)) return pos;
        float want = g - wheelOffset;
        groundY = groundInit ? Mathf.MoveTowards(groundY, want, 1.5f * Time.fixedDeltaTime) : want;   // 단차를 부드럽게 오르내림
        groundInit = true;
        pos.y = groundY;
        return pos;
    }

    void Start()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        startIndex = Mathf.Clamp(startIndex, 0, waypoints.Length - 1);
        rb.position = transform.position = waypoints[startIndex];
        MeasureWheels();
        rb.position = transform.position = OnGround(waypoints[startIndex], transform.rotation);
        target = startIndex + 1;
        if (target >= waypoints.Length) { target = loop ? 0 : startIndex - 1; if (!loop) dir = -1; }
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        if (Time.time < pauseUntil) return;

        // 경로는 수평(xz)으로만 따라가고, 높이는 바닥을 따른다
        Vector3 pos = rb.position, goal = waypoints[target];
        Vector3 to = goal - pos; to.y = 0f;
        float step = speed * Time.fixedDeltaTime;
        if (to.magnitude <= step)
        {
            rb.MovePosition(OnGround(new Vector3(goal.x, pos.y, goal.z), rb.rotation));
            Advance();
            return;
        }
        Vector3 d = to.normalized;
        var rot = Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(d), 300f * Time.fixedDeltaTime);
        rb.MovePosition(OnGround(pos + d * step, rot));
        rb.MoveRotation(rot);
    }

    void Advance()
    {
        if (loop)
        {
            target = (target + 1) % waypoints.Length;
            return;
        }
        if (target + dir < 0 || target + dir >= waypoints.Length)
        {
            dir = -dir;
            pauseUntil = Time.time + pauseAtEnds;
        }
        target += dir;
    }

    void Update()
    {
        if (!visual) return;
        bool moving = Time.time >= pauseUntil;
        rattle += Time.deltaTime * (moving ? 28f : 0f);
        visual.localPosition = new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(rattle)) * 0.035f : 0f, 0f);
        visual.localRotation = Quaternion.Euler(0f, 0f, moving ? Mathf.Sin(rattle * 0.5f) * 1.5f : 0f);
    }

    void OnTriggerEnter(Collider other) => Hit(other);
    void OnTriggerStay(Collider other) => Hit(other);

    void Hit(Collider other)
    {
        var gm = GameManager.I;
        if (!gm || !gm.player || gm.IsWon || EndingSequence.Running) return;
        if (other.GetComponentInParent<LostKitten>()) return;

        if (other.GetComponentInParent<CatPlayer>())
        {
            gm.player.HitByCart(transform.position, transform.forward, null);
            return;
        }
        var f = other.GetComponentInParent<Follower>();
        if (f && f.enabled) gm.player.HitByCart(transform.position, transform.forward, f);
    }
}
