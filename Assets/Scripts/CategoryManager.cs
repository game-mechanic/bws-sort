using DG.Tweening;
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
        public Bubble.Data data;
    }
    [SerializeField] HorizontalAlignment horizontalAlignment;
    [SerializeField] List<Data> datas = new();
    [SerializeField] int initialSpawns = 15;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();
    [SerializeField] HexGrid hexGrid;

    public HexGrid HexGrid { get => hexGrid; }

    void Start()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];
        //Shuffle();


        //WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        //Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        //for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
        //{
        //    Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
        //    BubbleType category = datas[j].name;
        //    Bubble.Data data = datas[j].data;
        //    Color bubbleColor = datas[j].overrideColor ?
        //        datas[j].bubbleColor :
        //        GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

        //    DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
        //    {
        //        var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
        //        bubble.Category = category;
        //        if (GameSettings.Instance.CanChangeColor)
        //            bubble.SetColor(bubbleColor);
        //        bubble.SetName(new() { data });
        //    });
        //    if (j % 4 == 0)
        //        yield return _waitForSeconds0_1;
        //}
        //currentIndex = initialSpawns;
    }

    private void Shuffle()
    {
        for (int i = 0; i < 5; i++)
        {
            int a = Random.Range(0, initialSpawns);
            int b = Random.Range(0, initialSpawns);
            (datas[b], datas[a]) = (datas[a], datas[b]);
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

    internal Bubble SpawnNewBubble(Vector3 position)
    {
        Vector3 pos = position;
        int j = currentIndex++;
        BubbleType category = datas[j].name;
        Bubble.Data data = datas[j].data;
        Color bubbleColor = datas[j].overrideColor ?
                    datas[j].bubbleColor :
                    GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];


        var bubble = Instantiate(GameSettings.Instance.Bubbles[0], pos, Quaternion.identity);
        bubble.Category = category;
        if (GameSettings.Instance.CanChangeColor)
            bubble.SetColor(bubbleColor);
        bubble.SetName(new() { data });
        bubble.RestorePositions();
        return bubble;
    }
}
