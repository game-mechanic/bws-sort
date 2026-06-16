using DG.Tweening;
using DT.GridSystem;
using System.Collections;
using System.Collections.Generic;
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
        for (int i = 0; i < 5; i++)
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


    public void SpawnNewCategories()
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        int end = Mathf.Min(currentIndex + 4, datas.Count);
        for (int j = currentIndex; j < end; j++)
        {
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType category = datas[j].name;
            Bubble.Data data = datas[j].data;
            Color bubbleColor = datas[j].overrideColor ?
                        datas[j].bubbleColor :
                        GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);

                if (EnableRandomSize)
                {
                    float randomScale = Random.Range(minMultiplier, maxMultiplier);
                    bubble.transform.localScale = Vector3.one * randomScale;
                }

                bubble.Category = category;

                if (GameSettings.Instance.CanChangeColor)
                    bubble.SetColor(bubbleColor);

                bubble.SetName(new() { data });
            });
        }
        currentIndex += 4;
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

    internal void SwapCategories(Bubble a, Bubble b)
    {
        a.transform.DOMove(GetWorldPosition(b.GridPosition), .1f)
           .OnComplete(() =>
           {
               a.EndDrag();
               Instance.CheckRow(a.GridPosition.y);
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
                Instance.CheckRow(a.GridPosition.y);
            });
    }
}
