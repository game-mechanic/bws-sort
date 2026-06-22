using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;          // SortingGroup

public class CategoryManager : Singleton<CategoryManager>
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public bool overrideColor = false;
        [ColorUsage(false)] public Color bubbleColor = Color.white;
        public Bubble.Data data;
    }

    // ── General Settings ──────────────────────────────────────────────────────
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] HorizontalAlignment horizontalAlignment;
    [SerializeField] List<Data> datas = new();
    [SerializeField] BubbleType[] categories;
    [SerializeField] int initialSpawns = 15;

    // ── Pyramid Spawn Settings ────────────────────────────────────────────────
    [Header("Pyramid Spawn Settings")]

    [Tooltip("When true, all data bubbles are spawned as concentric rings centred on " +
             "originPyramidPoint. Depth illusion is created via SortingGroup order — " +
             "inner rings render in front of outer rings. " +
             "Rigidbody2D is set to Kinematic and Collider2D to isTrigger on each bubble.")]
    [SerializeField] bool spawnInPyramid = false;

    [Tooltip("World-space centre of the pyramid. The apex bubble spawns here.")]
    [SerializeField] Transform originPyramidPoint;

    [Tooltip("Bubbles placed in ring 2 (default 6).")]
    [SerializeField] int layer2BubbleCount = 6;

    [Tooltip("Extra bubbles added per subsequent ring. " +
             "Default 6 → ring 2 = 6, ring 3 = 12, ring 4 = 18 …")]
    [SerializeField] int layerCountIncrement = 6;

    [Tooltip("Scale multiplier applied only to the origin/apex bubble (layer 0).")]
    [SerializeField] float originPointBubbleScaleMultiplier = 1.25f;

    [Tooltip("Amount by which each successive layer's scale is reduced. " +
             "Example: 0.1 => Layer0=1.25, Layer1=1.15, Layer2=1.05, Layer3=0.95")]
    [SerializeField] float layerScaleDecrementMultiplier = 0.1f;

    [Tooltip("Radius (world units) of the second ring from the origin.")]
    [SerializeField] float pyramidBaseRadius = 1.5f;

    [Tooltip("Additional radius per ring beyond ring 2, so the cluster widens outward.")]
    [SerializeField] float pyramidRadiusIncrement = 1f;

    [Tooltip("SortingGroup.sortingOrder assigned to the apex bubble (layer 0). " +
             "Outer rings receive progressively lower values so they render behind.")]
    [SerializeField] int pyramidApexSortingOrder = 10;

    [Tooltip("How much sortingOrder decreases per ring. " +
             "E.g. apex = 10, decrement = 1 → ring 1 = 9, ring 2 = 8 …")]
    [SerializeField] int sortingOrderDecrement = 1;

    [Tooltip("Perlin-noise amplitude applied to every bubble position. " +
             "0 = perfect geometry. Higher values scatter bubbles for an organic look. " +
             "Also offsets the noise-space seed, so different values give different patterns.")]
    [SerializeField] float noiseSeed = 0.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new();
    int currentCategory;

    public BubbleType CurrentCategory => categories[currentCategory % categories.Length];

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    IEnumerator Start()
    {
        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];

        if (!spawnOnStart)
            yield break;

        Shuffle();

        if (spawnInPyramid)
            yield return StartCoroutine(SpawnPyramid());
        else
            yield return StartCoroutine(SpawnHorizontal());

        currentCategory = 0;
    }

    // ── Horizontal (original) spawn ───────────────────────────────────────────
    IEnumerator SpawnHorizontal()
    {
        WaitForSeconds wait = new(0.1f);
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
        {
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType cat = datas[j].name;
            Bubble.Data data = datas[j].data;
            Color bubbleColor = datas[j].overrideColor
                ? datas[j].bubbleColor
                : GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
                bubble.Category = cat;
                if (GameSettings.Instance.CanChangeColor) bubble.SetColor(bubbleColor);
                bubble.SetName(new() { data });
            });

            if (j % 4 == 0)
                yield return wait;
        }

        currentIndex = initialSpawns;
    }

    // ── Pyramid spawn ─────────────────────────────────────────────────────────
    IEnumerator SpawnPyramid()
    {
        if (originPyramidPoint == null)
        {
            Debug.LogWarning("CategoryManager: originPyramidPoint is not assigned! " +
                             "Falling back to horizontal spawn.");
            yield return StartCoroutine(SpawnHorizontal());
            yield break;
        }

        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        int dataIndex = 0;
        int layerIndex = 0;

        while (dataIndex < datas.Count)
        {
            int bubblesInLayer = layerIndex == 0
                ? 1
                : layer2BubbleCount + (layerIndex - 1) * layerCountIncrement;

            // Capture layerIndex NOW — the while loop increments it after this block,
            // so without a local copy all lambdas in this iteration would close over
            // the same variable and read the wrong value by the time they fire.
            int capturedLayer = layerIndex;

            for (int i = 0; i < bubblesInLayer && dataIndex < datas.Count; i++)
            {
                Vector3 pos = GetPyramidPosition(capturedLayer, i, bubblesInLayer);
                BubbleType cat = datas[dataIndex].name;
                Bubble.Data data = datas[dataIndex].data;
                Color bubbleColor = datas[dataIndex].overrideColor
                    ? datas[dataIndex].bubbleColor
                    : GameSettings.Instance.BubbleColors[dataIndex % GameSettings.Instance.BubbleColors.Length];

                // Inner rings pop in first, outer rings trail behind
                float delay = Random.Range(0.05f, 0.15f) + capturedLayer * 0.15f;

                float layerScale = Mathf.Max(
                0.1f,
                originPointBubbleScaleMultiplier - (capturedLayer * layerScaleDecrementMultiplier)
                );

                DOVirtual.DelayedCall(delay, () =>
                {
                    var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
                    bubble.transform.localScale *= layerScale;

                    // Kinematic + trigger so bubbles are purely visual decorations
                    var rb = bubble.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

                    var coll = bubble.GetComponent<Collider2D>();
                    if (coll != null) coll.isTrigger = true;

                    // Inner layers render in front; each outer ring steps one order back
                    var sg = bubble.GetComponent<SortingGroup>();
                    if (sg != null)
                        sg.sortingOrder = pyramidApexSortingOrder - capturedLayer * sortingOrderDecrement;

                    bubble.Category = cat;
                    if (GameSettings.Instance.CanChangeColor) bubble.SetColor(bubbleColor);
                    bubble.SetName(new() { data });
                });

                dataIndex++;
            }

            layerIndex++;

            // Breathe every two rings to avoid frame spikes on large data sets
            if (layerIndex % 2 == 0)
                yield return new WaitForSeconds(0.05f);
        }

        currentIndex = datas.Count;
    }

    // ── Pyramid geometry helpers ──────────────────────────────────────────────

    /// <summary>
    /// Returns the world position for one bubble in the pyramid.
    /// All rings share the same origin Z — depth is purely a sorting-order illusion.
    /// </summary>
    Vector3 GetPyramidPosition(int layer, int indexInLayer, int totalInLayer)
    {
        Vector3 origin = originPyramidPoint.position;

        // Apex sits exactly on the origin point
        if (layer == 0)
            return ApplyPositionNoise(origin, 0, 0);

        // Each successive ring is wider so the cluster fans out from the centre
        float radius = pyramidBaseRadius + (layer - 1) * pyramidRadiusIncrement;

        // Distribute bubbles evenly around the full 360° of the ring.
        // Both X and Y are offset by the angle — there is no per-layer Y shift;
        // the pyramid depth comes entirely from SortingGroup order.
        float angle = (2f * Mathf.PI / totalInLayer) * indexInLayer;

        Vector3 pos = new Vector3(
            origin.x + radius * Mathf.Cos(angle),
            origin.y + radius * Mathf.Sin(angle),
            origin.z
        );

        return ApplyPositionNoise(pos, layer, indexInLayer);
    }

    /// <summary>
    /// Applies Perlin-noise displacement to <paramref name="pos"/> scaled by
    /// <see cref="noiseSeed"/>. Returns the original position when noiseSeed ≤ 0.
    /// </summary>
    Vector3 ApplyPositionNoise(Vector3 pos, int layer, int index)
    {
        if (noiseSeed <= 0f) return pos;

        float nx = (Mathf.PerlinNoise(
            pos.x * 0.5f + noiseSeed + index * 0.37f,
            pos.y * 0.5f + layer * 0.73f) - 0.5f) * noiseSeed * 2f;

        float ny = (Mathf.PerlinNoise(
            pos.y * 0.5f + noiseSeed + layer * 0.73f,
            pos.x * 0.5f + index * 0.37f) - 0.5f) * noiseSeed * 2f;

        return pos + new Vector3(nx, ny, 0f);
    }

    // ── Shuffle ───────────────────────────────────────────────────────────────
    private void Shuffle()
    {
        if (datas.Count == 0) return;

        // Pyramid mode shuffles the full list; horizontal mode only the first batch
        int bound = spawnInPyramid
            ? datas.Count
            : Mathf.Min(initialSpawns, datas.Count);

        for (int i = 0; i < 5; i++)
        {
            int a = Random.Range(0, bound);
            int b = Random.Range(0, bound);
            (datas[b], datas[a]) = (datas[a], datas[b]);
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ChangeCategory();
    }

    // ── Category tracking ─────────────────────────────────────────────────────
    public void RegisterCategory(BubbleType category)
    {
        if (categoryCounts.ContainsKey(category))
            categoryCounts[category]++;
        else
            categoryCounts[category] = 1;
    }

    public int ReduceCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return 0;

        categoryCounts[category]--;

        if (categoryCounts[category] <= 0)
        {
            categoryCounts.Remove(category);
            currentCategory++;
            CategoryManager.Instance.SpawnNewCategories();
            return 0;
        }

        return categoryCounts[category];
    }

    public void SpawnNewCategories()
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        int end = Mathf.Min(currentIndex + 4, datas.Count);
        for (int j = currentIndex; j < end; j++)
        {
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType cat = datas[j].name;
            Bubble.Data data = datas[j].data;
            Color bubbleColor = datas[j].overrideColor
                ? datas[j].bubbleColor
                : GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
                bubble.Category = cat;
                if (GameSettings.Instance.CanChangeColor) bubble.SetColor(bubbleColor);
                bubble.SetName(new() { data });
            });
        }
        currentIndex += 4;
    }

    // ── Category change animation (preserved, unused) ─────────────────────────
    void ChangeCategory()
    {
        //currentIndex++;
        //Vector3 squishedScale = new Vector3(1.2f, 0.8f, 1);
        //Vector3 originalScale = new Vector3(.8f, 1.2f, 1);
        //Vector3 startPosition = transform.position;
        //Sequence moveOutSequence = DOTween.Sequence();

        //// Step 2: When lid reaches destination, squish the cart
        //moveOutSequence.Append(DOTween.Sequence()
        //        .Append(transform.DOScale(squishedScale, 0.2f))
        //        .Append(transform.DOScale(originalScale, 0.1f)));

        //// Step 3: Move the cart up and restore scale simultaneously
        //moveOutSequence.Append(transform.DOMove(startPosition + Vector3.up * 2, .5f).SetEase(Ease.OutExpo));

        //moveOutSequence.AppendCallback(() =>
        //{
        //    Redraw();
        //});
        //moveOutSequence.Append(transform.DOMove(startPosition, .5f).SetEase(Ease.InExpo));
        //moveOutSequence.Join(transform.DOScale(originalScale, 0.1f));
        //moveOutSequence.Append(transform.DOScale(squishedScale, 0.2f));
        //moveOutSequence.Append(transform.DOScale(Vector3.one, 0.1f));
    }

    public int GetCategoryCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return -1;
        return categoryCounts[category];
    }
}