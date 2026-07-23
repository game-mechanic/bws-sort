using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DT.GridSystem;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class CategoryManager : GridSystem2D<Bubble>
{
    public static CategoryManager instance;
    public static CategoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<CategoryManager>();
            }
            return instance;
        }
    }

    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public bool overrideColor = false;
        [ColorUsage(false)] public Color bubbleColor = Color.white;
        public Bubble.Data data;
    }
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] HorizontalAlignment horizontalAlignment;
    [SerializeField] List<Data> datas = new();
    [SerializeField] int initialSpawns = 15;
    bool EnableRandomSize => GameSettings.Instance.EnableRandomBubbleSize;
    [SerializeField] float minMultiplier = 1f;
    [SerializeField] float maxMultiplier = 2f;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();


    IEnumerator Start()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];

        if (!spawnOnStart)
        {
            yield break;
        }

        Shuffle();

        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        for (int j = 0; j < gridArray.Length; j++)
        {

            FromIndex(j, out int x, out int y);

            Vector2Int gridPos = new(x, y);

            Vector3 pos = GetWorldPosition(x, y);
            BubbleType category = datas[j].name;
            Bubble.Data data = datas[j].data;
            Color bubbleColor = datas[j].overrideColor ?
                datas[j].bubbleColor :
                GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);

            if (EnableRandomSize)
            {
                float randomScale = Random.Range(minMultiplier, maxMultiplier);
                bubble.transform.localScale = Vector3.one * randomScale;
            }

            bubble.Category = category;

            if (GameSettings.Instance.CanChangeColor)
                bubble.SetColor(bubbleColor);
            bubble.GridPosition = gridPos;
            AddGridObject(x, y, bubble);
            bubble.SetName(new() { data });
        }

        currentIndex = gridArray.Length;
    }

    private void Shuffle()
    {
        if (datas.Count == 0) return;
        for (int i = 0; i < 10; i++)
        {
            int a = Random.Range(0, initialSpawns);
            int b = Random.Range(0, initialSpawns);
            (datas[b], datas[a]) = (datas[a], datas[b]);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeCategory();
        }
    }
    public void RegisterCategory(BubbleType category)
    {
        if (categoryCounts.ContainsKey(category))
        {
            categoryCounts[category]++;
        }
        else
        {
            categoryCounts[category] = 1;
        }
    }

    public int ReduceCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return 0;
        categoryCounts[category] -= 2;

        if (categoryCounts[category] <= 0)
        {
            categoryCounts.Remove(category);
            return 0;
        }
        else
        {
            return categoryCounts[category];
        }
    }


    public void SpawnNewCategories(Vector2Int index)
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        int newSpawnCount = GameSettings.Instance.CheckRow ? GridSize.x : GridSize.y;

        int end = Mathf.Min(currentIndex + newSpawnCount, datas.Count);
        int i = 0;
        for (int j = currentIndex; j < end; j++)
        {
            int x = GameSettings.Instance.CheckRow ? j % GridSize.x : index.x;

            int y = GameSettings.Instance.CheckRow ? index.y : j % GridSize.y;

            Vector3 pos = GetWorldPosition(x, y);

            BubbleType category = datas[j].name;
            Bubble.Data data = datas[j].data;
            Color bubbleColor = datas[j].overrideColor ?
                        datas[j].bubbleColor :
                        GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            // DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            // {
            var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);

            if (EnableRandomSize)
            {
                float randomScale = Random.Range(minMultiplier, maxMultiplier);
                bubble.transform.localScale = Vector3.one * randomScale;
            }
            bubble.transform.DOScale(bubble.transform.localScale, .1f).From(0).SetDelay(i++ * 0.1f);

            bubble.Category = category;
            bubble.GridPosition = new(x, y);
            AddGridObject(x, y, bubble);

            if (GameSettings.Instance.CanChangeColor)
                bubble.SetColor(bubbleColor);

            bubble.SetName(new() { data });
            // });
        }
        currentIndex += newSpawnCount;
    }

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

    public void CheckRow(int y)
    {
        HashSet<BubbleType> rowCategories = new();

        for (int i = 0; i < GridSize.x; i++)
        {
            if (TryGetGridObject(i, y, out Bubble b))
            {
                rowCategories.Add(b.Category);
            }
        }
        if (rowCategories.Count > 1)
        {
            return;
        }
        float delayInterval = 0.1f;
        for (int i = 0; i < GridSize.x; i++)
        {
            if (TryGetGridObject(i, y, out Bubble b))
            {
                b.transform.DOMove(b.transform.position + Vector3.up * 0.05f, .1f).SetLoops(2, LoopType.Yoyo).SetDelay(delayInterval * i);
                DOVirtual.DelayedCall(i * delayInterval, () => b.SetColor(b.Category.Color));
            }
        }

    }

    public void CheckColumn(int x)
    {
        HashSet<BubbleType> rowCategories = new();

        for (int i = 0; i < GridSize.y; i++)
        {
            if (TryGetGridObject(x, i, out Bubble b))
            {
                rowCategories.Add(b.Category);
            }
        }
        if (rowCategories.Count > 1)
        {
            return;
        }
        float delayInterval = 0.1f;


        for (int i = 0; i < GridSize.y; i++)
        {
            if (TryGetGridObject(x, i, out Bubble b))
            {
                b.transform.DOMove(b.transform.position + Vector3.up * 0.05f, .1f).SetLoops(2, LoopType.Yoyo).SetDelay(delayInterval * i);
                DOVirtual.DelayedCall(i * delayInterval, () => b.SetColor(b.Category.Color));
            }
        }

        // List<Bubble> colBubbles = new();
        // for (int i = 0; i < GridSize.y; i++)
        // {
        //     if (TryGetGridObject(x, i, out Bubble b))
        //     {
        //         colBubbles.Add(b);
        //     }
        // }
        // StartCoroutine(MergeOneByOne(colBubbles, GameSettings.GetStackedPosition(GetWorldPosition(x, 0), 5, CellSize.y)));

    }

    internal void SwapCategories(Bubble a, Bubble b)
    {
        a.transform.DOMove(GetWorldPosition(b.GridPosition), .1f)
           .OnComplete(() =>
           {
               a.EndDrag();
               CheckForMatch(a.GridPosition);
           });

        Swap(b, GetWorldPosition(a.GridPosition));

        (b.GridPosition, a.GridPosition) = (a.GridPosition, b.GridPosition);

        AddGridObject(a.GridPosition.x, a.GridPosition.y, a);
        AddGridObject(b.GridPosition.x, b.GridPosition.y, b);
    }
    private static void Swap(Bubble a, Vector3 b)
    {
        const float MoveDuraiton = .5f;

        Vector2 dir = b - a.transform.position;
        Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized * .52f;

        a.transform.DOPath(new Vector3[] { Vector3.Lerp(a.transform.position, b, 0.5f) + perp, b }, MoveDuraiton, pathType: PathType.CatmullRom)
            .OnComplete(() =>
            {
                a.EndDrag();
                CheckForMatch(a.GridPosition);
            });
    }
    static void CheckForMatch(Vector2Int index)
    {
        if (GameSettings.Instance.CheckRow)
            Instance.CheckRow(index.y);
        else
            Instance.CheckColumn(index.x);
    }



    // IEnumerator MergeOneByOne(List<Bubble> hoveredBubbles, Vector3 targetPosition)
    // {
    //     const float Duration = .8f;
    //     const float Interval = 0.2f;

    //     //yield return new WaitForSeconds(HighlightDuration);

    //     Bubble a = hoveredBubbles[3];
    //     const Ease outBack = Ease.InOutSine;

    //     a.transform.DOMove(targetPosition, Duration)
    //         .SetEase(outBack);
    //     Bubble b = hoveredBubbles[2];

    //     b.transform.DOMove(targetPosition, Duration)
    //         .SetDelay(Interval)
    //         .SetEase(outBack);

    //     hoveredBubbles[1].transform.DOMove(targetPosition, Duration)
    //        .SetDelay(Interval * 2)
    //        .SetEase(outBack);

    //     hoveredBubbles[0].transform.DOMove(targetPosition, Duration)
    //         .SetDelay(Interval * 3)
    //         .SetEase(outBack);

    //     yield return new WaitForSeconds(Duration);
    //     // IMPORTANT: TryMerge must RETURN the new bubble
    //     InputHandler.Instance.TryMerge(a, b, out Bubble current);
    //     yield return new WaitForSeconds(0.1f);
    //     a = current;
    //     b = hoveredBubbles[1];
    //     yield return new WaitForSeconds(Interval);
    //     InputHandler.Instance.TryMerge(a, b, out current);
    //     //yield return new WaitForSeconds(0.1f);
    //     a = current;
    //     b = hoveredBubbles[0];

    //     yield return new WaitForSeconds(Interval);
    //     InputHandler.Instance.TryMerge(a, b, out current);
    // }
    IEnumerator MergeOneByOne(List<Bubble> hoveredBubbles, Vector3 targetPosition)
    {
        const float Duration = 0.8f;
        const float Interval = 0.2f;
        const Ease ease = Ease.InOutSine;

        // Move all bubbles one by one
        Tween lastTween = null;

        for (int i = hoveredBubbles.Count - 1, order = 0; i >= 0; i--, order++)
        {
            lastTween = hoveredBubbles[i].transform
                .DOMove(targetPosition, Duration)
                .SetDelay(order * Interval)
                .SetEase(ease);
        }

        // Wait until the last bubble reaches the target
        yield return lastTween.WaitForCompletion();

        // Start with the last bubble
        Bubble current = hoveredBubbles[hoveredBubbles.Count - 1];

        // Merge remaining bubbles one by one
        for (int i = hoveredBubbles.Count - 2; i >= 0; i--)
        {
            InputHandler.Instance.TryMerge(current, hoveredBubbles[i], out Bubble merged);
            current = merged;

            // Small delay between merges (optional)
            if (i > 0)
                yield return new WaitForSeconds(Interval);
        }
    }
}
