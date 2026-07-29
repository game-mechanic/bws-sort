using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class CategoryManager : Singleton<CategoryManager>
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public Bubble.Data data;
    }

    [SerializeField] List<Data> datas = new();
    [SerializeField] private Transform spawnPosition;
    [SerializeField] float radius = 1f;
    [SerializeField] int initialSpawns = 15;

    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();

    // ── lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        float angleOffset = 360f / initialSpawns * Mathf.Deg2Rad;
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        for (int i = 0; i < Mathf.Min(initialSpawns, datas.Count); i++)
        {
            Vector3 pos = spawnPosition.position + new Vector3(
                Mathf.Sin(angleOffset * i) * radius,
                Mathf.Cos(angleOffset * i) * radius, 0);

            GenerateBubble(bubblePrefab, pos, datas[i].name, datas[i].data);
        }

        currentIndex = Mathf.Min(initialSpawns, datas.Count);

        // Wire up the center circle once all bubbles (and their registrations) are done.
        // Use a small delay so all Bubble.Start() callbacks have run first.
        DOVirtual.DelayedCall(0.1f, InitCenterCircle);
    }

    // ── category tracking ──────────────────────────────────────────────────

    public void RegisterCategory(BubbleType category)
    {
        if (categoryCounts.ContainsKey(category))
            categoryCounts[category]++;
        else
            categoryCounts[category] = 1;
    }

    /// <summary>
    /// Decrements the category count when a bubble is accepted by the center circle.
    /// Returns the new count (0 means the category is cleared).
    /// </summary>
    public int ReduceCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return 0;

        categoryCounts[category]--;

        if (categoryCounts[category] <= 0)
        {
            categoryCounts.Remove(category);
            return 0;
        }

        return categoryCounts[category];
    }

    public int GetCategoryCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return 0;
        return categoryCounts[category];
    }

    // ── center circle init ─────────────────────────────────────────────────

    void InitCenterCircle()
    {
        if (CenterCircle.Instance == null)
        {
            Debug.LogWarning("CategoryManager: No CenterCircle found in scene.");
            return;
        }

        // Build an ordered, deduplicated list of categories from the datas list
        // so the center circle shows them in spawn order.
        List<BubbleType> ordered = new List<BubbleType>();
        HashSet<BubbleType> seen = new HashSet<BubbleType>();
        foreach (var d in datas)
        {
            if (d.name != null && seen.Add(d.name))
                ordered.Add(d.name);
        }

        CenterCircle.Instance.SetCategorySequence(ordered);
    }

    // ── bubble spawning ────────────────────────────────────────────────────

    /// <summary>
    /// Spawn the next bubble from the data queue at a specific world position
    /// (i.e. exactly where the removed bubble was).
    /// </summary>
    public void SpawnNewBubbleAt(Vector3 worldPos)
    {
        if (currentIndex >= datas.Count) return;

        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        BubbleType category = datas[currentIndex].name;
        Bubble.Data data    = datas[currentIndex].data;
        currentIndex++;

        DOVirtual.DelayedCall(Random.Range(0.05f, 0.2f), () =>
            GenerateBubble(bubblePrefab, worldPos, category, data));
    }

    public void SpawnNewBubbles(int count)
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        int end = Mathf.Min(currentIndex + count, datas.Count);

        float angleOffset = 360f / Mathf.Max(count, 1) * Mathf.Deg2Rad;

        for (int j = currentIndex; j < end; j++)
        {
            int localIdx = j - currentIndex;
            Vector3 pos = spawnPosition.position + new Vector3(
                Mathf.Sin(angleOffset * localIdx) * radius,
                Mathf.Cos(angleOffset * localIdx) * radius, 0);

            BubbleType category = datas[j].name;
            Bubble.Data data    = datas[j].data;

            DOVirtual.DelayedCall(Random.Range(0.05f, 0.3f), () =>
                GenerateBubble(bubblePrefab, pos, category, data));
        }

        currentIndex = end;
    }

    private static Bubble GenerateBubble(Bubble bubblePrefab, Vector3 pos,
                                          BubbleType category, Bubble.Data data)
    {
        var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
        bubble.transform.DOScale(1, 0.3f).From(0).SetEase(Ease.OutBack);
        bubble.Category = category;
        bubble.SetName(new List<Bubble.Data> { data });
        return bubble;
    }

    // ── gizmos ─────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (spawnPosition != null)
            Gizmos.DrawWireSphere(spawnPosition.position, radius);
    }
}
