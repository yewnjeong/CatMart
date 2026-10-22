using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기본: 씬에 직접 배치한 음식을 그대로 쓴다. randomizeTypes를 켜면 자리는 그대로 두고
/// 같은 계열(빵/과자/과일) 안에서 종류만 매 판 무작위로 바꾼다.
/// useSpawnPoints를 켜면 예전 방식: 배치한 음식을 지우고 FoodSpawnPoint 후보 중에서 무작위로 골라 놓는다.
/// - 구역마다 '쉬움' 자리를 최소 1개 보장(초반 진입이 막히지 않게)
/// - 얻기 어려운 자리일수록 cm가 높다 (쉬움 2 / 높은 곳 3 / 숨은 곳 3 / 위험 5)
/// </summary>
public class FoodSpawner : MonoBehaviour
{
    [Header("배치 방식")]
    [Tooltip("켜면 배치한 음식의 자리는 그대로, 종류만 같은 계열 안에서 매 판 랜덤")]
    public bool randomizeTypes = false;
    [Tooltip("켜면 예전 방식(후보 자리 FoodSpawnPoint에서 무작위 배치)")]
    public bool useSpawnPoints = false;

    [Header("구역별 음식 개수")]
    public int breadCount = 4;
    public int snackCount = 4;
    public int fruitCount = 4;
    public int plazaCount = 3;

    [Header("자리 종류별 cm")]
    public int easyCm = 2;
    public int highCm = 3;
    public int hiddenCm = 3;
    public int dangerCm = 5;

    [Header("규칙")]
    [Tooltip("구역마다 쉬운 자리를 최소 1개 포함")]
    public bool guaranteeEasyPerZone = true;
    [Tooltip("0이면 매 판 랜덤. 숫자를 넣으면 항상 같은 배치(테스트용)")]
    public int fixedSeed = 0;

    public Transform foodRoot;

    void Start()
    {
        if (fixedSeed != 0) Random.InitState(fixedSeed);
        if (!useSpawnPoints)
        {
            if (!randomizeTypes) return;
            foreach (var f in new List<Food>(Food.All))
            {
                if (!f || f.physical) continue;
                var g = Food.GroupOf(f.type);
                if (g != FoodGroup.None) f.SetType(Food.RandomOf(g));
            }
            return;
        }

        // 배치해 둔 고정 음식 정리
        foreach (var f in new List<Food>(Food.All))
            if (f && !f.physical && f.type != FoodType.LastPiece) Destroy(f.gameObject);

        if (!foodRoot && GameManager.I) foodRoot = GameManager.I.foodRoot;

        var points = GetComponentsInChildren<FoodSpawnPoint>();
        int total = 0, totalCm = 0;
        foreach (FoodZone z in System.Enum.GetValues(typeof(FoodZone)))
        {
            var list = new List<FoodSpawnPoint>();
            foreach (var p in points) if (p.zone == z) list.Add(p);
            Shuffle(list);

            int want = Mathf.Min(CountFor(z), list.Count);
            var picked = new List<FoodSpawnPoint>();
            if (guaranteeEasyPerZone && want > 0)
            {
                var easy = list.Find(p => p.kind == SpotKind.Easy);
                if (easy) { picked.Add(easy); list.Remove(easy); }
            }
            for (int i = 0; picked.Count < want && i < list.Count; i++) picked.Add(list[i]);

            foreach (var p in picked)
            {
                var f = Food.Create(PickType(z), p.transform.position, foodRoot, false);
                f.cm = CmFor(p.kind);
                total++; totalCm += f.cm;
            }
        }
        Debug.Log($"[FoodSpawner] 음식 {total}개 배치, 합계 {totalCm}cm");
    }

    int CountFor(FoodZone z)
    {
        switch (z)
        {
            case FoodZone.Bread: return breadCount;
            case FoodZone.Snack: return snackCount;
            case FoodZone.Fruit: return fruitCount;
            default: return plazaCount;
        }
    }

    int CmFor(SpotKind k)
    {
        switch (k)
        {
            case SpotKind.High: return highCm;
            case SpotKind.Hidden: return hiddenCm;
            case SpotKind.Danger: return dangerCm;
            default: return easyCm;
        }
    }

    static FoodType PickType(FoodZone z)
    {
        switch (z)
        {
            case FoodZone.Bread: return Food.RandomOf(FoodGroup.Bread);
            case FoodZone.Snack: return Food.RandomOf(FoodGroup.Snack);
            case FoodZone.Fruit: return Food.RandomOf(FoodGroup.Fruit);
            default: return Food.RandomOf((FoodGroup)Random.Range(1, 4));
        }
    }

    static void Shuffle<T>(List<T> l)
    {
        for (int i = l.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (l[i], l[j]) = (l[j], l[i]);
        }
    }
}
