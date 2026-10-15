using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class CategoryManager : Singleton<CategoryManager>
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public bool overrideColor = false;
        [ColorUsage(false)] public Color bubbleColor = Color.white;
        public int nextMergeBubbleCount = 0;
        public bool isDummy = false;
        public Bubble.Data data;

        public int NextMergeBubbleCount { get => nextMergeBubbleCount; set => nextMergeBubbleCount = value; }
        public int nextMergeCategories { get => nextMergeBubbleCount; set => nextMergeBubbleCount = value; }
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

        //Shuffle();


        WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
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
            if (j % 4 == 0)
                yield return _waitForSeconds0_1;
        }
        currentIndex = initialSpawns;
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

    internal Bubble SpawnNewBubble(Vector3 pos)
    {
        if (datas == null || datas.Count == 0) return null;
        if (currentIndex >= datas.Count)
        {
            currentIndex = 0;
        }

        int j = currentIndex;

        BubbleType category = datas[j].name;
        Color bubbleColor = datas[j].overrideColor ?
                    datas[j].bubbleColor :
                    GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

        List<Bubble.Data> bubbleDataList = new() { datas[j].data };
        if (!datas[j].isDummy)
        {
            int mergeCount = datas[j].nextMergeBubbleCount;

            for (int i = 1; i <= mergeCount; i++)
            {
                if (j + i < datas.Count)
                {
                    bubbleDataList.Add(datas[j + i].data);
                }
            }
        }

        int totalMergedCount = bubbleDataList.Count;
        int bubbleIndex = Mathf.Clamp(totalMergedCount - 1, 0, GameSettings.Instance.Bubbles.Length - 1);

        Bubble original = !datas[j].isDummy ? GameSettings.Instance.Bubbles[bubbleIndex] : GameSettings.Instance.DummyFruit;
        var bubble = Instantiate(original, pos, Quaternion.identity);

        if (EnableRandomSize)
        {
            float randomScale = Random.Range(minMultiplier, maxMultiplier);
            bubble.transform.localScale = Vector3.one * randomScale;
        }

        bubble.Category = category;

        if (GameSettings.Instance.CanChangeColor)
            bubble.SetColor(bubbleColor);

        bubble.SetName(bubbleDataList);

        currentIndex += totalMergedCount;

        return bubble;
    }
}
