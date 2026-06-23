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

    [Tooltip("Scale multiplier applied only to the origin/apex bubble (layer 0).")]
    [SerializeField] float originPointBubbleScaleMultiplier = 1.25f;

    [Tooltip("Amount by which each successive layer's scale is reduced. " +
             "Example: 0.1 => Layer0=1.25, Layer1=1.15, Layer2=1.05, Layer3=0.95")]
    [SerializeField] float layerScaleDecrementMultiplier = 0.1f;

    [Header("Grid Layers")]
    [SerializeField] List<GridGenerator> layers = new();

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

    [Header("Layer Scale Reveal")]
    [SerializeField] float layerScaleRevealDuration = 0.25f;
    [SerializeField] float layerScaleRevealDelay = 0.1f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    IEnumerator Start()
    {
        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];

        if (!spawnOnStart)
            yield break;

        // Shuffle();

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
        List<SpawnedBubbleInfo> spawnedBubbles = new();

        if (originPyramidPoint == null)
        {
            Debug.LogWarning("CategoryManager: originPyramidPoint is not assigned! " +
                             "Falling back to horizontal spawn.");
            yield return StartCoroutine(SpawnHorizontal());
            yield break;
        }

        
        int dataIndex = 0;
        int layerIndex = 0;

        while (dataIndex < datas.Count)
        {
            List<Vector3> layerPositions = new();

            if (layerIndex == 0)
            {
                layerPositions.Add(originPyramidPoint.position);
            }
            else
            {
                int gridIdx = layerIndex - 1;

                if (gridIdx >= layers.Count || layers[gridIdx] == null)
                    yield break;

                layerPositions.AddRange(layers[gridIdx].GetBordersPos());
            }

            int capturedLayer = layerIndex;

            for (int i = 0; i < layerPositions.Count && dataIndex < datas.Count; i++)
            {
                Bubble bubblePrefab = GameSettings.Instance.PyramidBubbles[
                Random.Range(0, GameSettings.Instance.PyramidBubbles.Length)
    ];

                Vector3 pos = ApplyPositionNoise(
                    layerPositions[i],
                    capturedLayer,
                    i
                );

                BubbleType cat = datas[dataIndex].name;
                Bubble.Data data = datas[dataIndex].data;

                Color bubbleColor = datas[dataIndex].overrideColor
                    ? datas[dataIndex].bubbleColor
                    : GameSettings.Instance.BubbleColors[
                        dataIndex % GameSettings.Instance.BubbleColors.Length];

                float delay = Random.Range(0.05f, 0.15f) + capturedLayer * 0.15f;

                float layerScale = Mathf.Max(
                    0.1f,
                    originPointBubbleScaleMultiplier -
                    (capturedLayer * layerScaleDecrementMultiplier)
                );

                var bubble = Instantiate(
                bubblePrefab,
                pos,
                Quaternion.identity
                );

                bubble.transform.localScale *= layerScale;

                var rb = bubble.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.bodyType = RigidbodyType2D.Kinematic;

                var coll = bubble.GetComponent<Collider2D>();
                if (coll != null)
                    coll.isTrigger = true;

                var sg = bubble.GetComponent<SortingGroup>();
                if (sg != null)
                {
                    sg.sortingOrder =
                        pyramidApexSortingOrder -
                        capturedLayer * sortingOrderDecrement;
                }

                bubble.Category = cat;

                if (GameSettings.Instance.CanChangeColor)
                    bubble.SetColor(bubbleColor);

                bubble.SetName(new() { data });

                spawnedBubbles.Add(new SpawnedBubbleInfo
                {
                    bubble = bubble,
                    originalScale = bubble.transform.localScale,
                    layer = capturedLayer
                    
                });

                // Cache complete. Hide all bubbles.
                bubble.transform.localScale = Vector3.zero;
                dataIndex++;
            }

            layerIndex++;

            if (layerIndex % 2 == 0)
                yield return new WaitForSeconds(0.05f);
        }

        // Cache complete. Hide all bubbles.
        /*foreach (var info in spawnedBubbles)
        {
            if (info.bubble != null)
                info.bubble.transform.localScale = Vector3.zero;
        }*/

        // Reveal from outermost layer -> innermost layer.
        int maxLayer = -1;

        foreach (var info in spawnedBubbles)
        {
            maxLayer = Mathf.Max(maxLayer, info.layer);
        }

        for (int layer = maxLayer; layer >= 0; layer--)
        {
            float startDelay =
                (maxLayer - layer) * layerScaleRevealDelay;

            foreach (var info in spawnedBubbles)
            {
                if (info.layer != layer || info.bubble == null)
                    continue;

                info.bubble.transform
                    .DOScale(info.originalScale, layerScaleRevealDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(startDelay);
            }
        }

        currentIndex = datas.Count;
    }

    // ── Pyramid geometry helpers ──────────────────────────────────────────────

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

class SpawnedBubbleInfo
{
    public Bubble bubble;
    public Vector3 originalScale;
    public int layer;
}