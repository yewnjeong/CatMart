using UnityEngine;

/// <summary>
/// 음식 종류별 아트 모델. Resources/FoodArtLibrary 에 두면 Food가 자동으로 찾아 쓴다.
/// size = 모델의 가장 긴 변 길이(m). 모델은 이 크기로 맞춰지고 중심이 피벗에 오도록 정렬된다.
/// 항목이 없는 종류는 예전 기본 도형으로 만들어진다.
/// </summary>
[CreateAssetMenu(menuName = "101CM/Food Art Library")]
public class FoodArtLibrary : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public FoodType type;
        public GameObject prefab;
        public float size = 0.5f;
        public Vector3 euler;
    }

    public Entry[] entries;

    static FoodArtLibrary cached;
    static bool loaded;

    public static FoodArtLibrary Instance
    {
        get
        {
            if (!loaded || !cached)
            {
                cached = Resources.Load<FoodArtLibrary>("FoodArtLibrary");
                loaded = true;
            }
            return cached;
        }
    }

    public Entry Find(FoodType t)
    {
        if (entries == null) return null;
        foreach (var e in entries) if (e != null && e.type == t && e.prefab) return e;
        return null;
    }
}
