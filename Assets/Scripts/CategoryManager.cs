using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor.Localization.Plugins.XLIFF.V20;
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
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] HorizontalAlignment horizontalAlignment;
    [SerializeField] List<Data> datas = new();
    [SerializeField] int initialSpawns = 15;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();
    BubbleSlot[] bubbleSlots;



    IEnumerator Start()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];

        bubbleSlots = FindObjectsByType<BubbleSlot>(sortMode: FindObjectsSortMode.None);

        if (!spawnOnStart)
        {
            yield break;
        }

        Shuffle(Mathf.Min(bubbleSlots.Length, datas.Count));


        WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        int playableIndex = 0;
        for (int j = 0; j < bubbleSlots.Length; j++)
        {
            Vector3 pos = bubbleSlots[j].transform.position;
            Data categoryMangagerData = datas[bubbleSlots[j].IsPlayable ? playableIndex % datas.Count : j % datas.Count];
            BubbleType category = categoryMangagerData.name;
            Bubble.Data data = categoryMangagerData.data;
            Color bubbleColor = categoryMangagerData.overrideColor ?
                categoryMangagerData.bubbleColor :
                (bubbleSlots[j].ShouldOverrideColor ? bubbleSlots[j].Color : GameSettings.Instance.BubbleColors[currentIndex % GameSettings.Instance.BubbleColors.Length]);

            /*DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {*/
            Bubble bubble = CreateBubble(bubblePrefab, pos, category, data, bubbleColor);

            bubble.BubbleSlot = bubbleSlots[j];
            if (bubbleSlots[j].IsPlayable)
                playableIndex++;
            /*});*/
            /*if (j % 4 == 0)
                yield return _waitForSeconds0_1;*/
        }
        currentIndex = playableIndex + 1;
    }

    private static Bubble CreateBubble(Bubble bubblePrefab, Vector3 pos, BubbleType category, Bubble.Data data, Color bubbleColor)
    {
        var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
        bubble.Category = category;
        bubble.IsKinematic = RigidbodyType2D.Kinematic;
        if (GameSettings.Instance.CanChangeColor)
            bubble.SetColor(bubbleColor);
        bubble.SetName(new() { data });
        return bubble;
    }

    private void Shuffle(int length)
    {
        if (datas.Count == 0) return;
        for (int i = 0; i < 15; i++)
        {
            int a = Random.Range(0, length);
            int b = Random.Range(0, length);
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

    internal void SpawnNewCategory(BubbleSlot bubbleSlot)
    {
        // if (currentIndex >= datas.Count) return;

        Data data = datas[currentIndex % datas.Count];

        var bubble = CreateBubble(bubblePrefab: GameSettings.Instance.Bubbles[0],
            pos: bubbleSlot.transform.position,
            category: data.name,
            data: data.data,
            bubbleColor: data.overrideColor ?
            data.bubbleColor :
            (bubbleSlot.ShouldOverrideColor ? bubbleSlot.Color : GameSettings.Instance.BubbleColors[currentIndex % GameSettings.Instance.BubbleColors.Length]));
        bubble.BubbleSlot = bubbleSlot;
        currentIndex++;
        bubble.transform.DOScale(1, 0.3f)
            .From(0)
            .SetEase(Ease.InSine);
    }
}
