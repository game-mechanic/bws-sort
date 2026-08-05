using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class CategoryManager : Singleton<CategoryManager>
{
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] LevelData levelData;
    [SerializeField] HorizontalAlignment horizontalAlignment;
    List<LevelData.Data> datas = new();
    int initialSpawns = 15;

    bool EnableRandomSize => GameSettings.Instance.EnableRandomBubbleSize;
    float minMultiplier = 1f;
    float maxMultiplier = 2f;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();

    public LevelData LevelDataAsset => levelData;

    public int CurrentIndex { get => currentIndex; }

    IEnumerator Start()
    {
        datas = new(levelData.datas);
        initialSpawns = levelData.initialSpawns;
        minMultiplier = levelData.minMultiplier;
        maxMultiplier = levelData.maxMultiplier;

        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];

        if (!spawnOnStart)
        {
            yield break;
        }

        Shuffle();

        List<Bubble> blastableBubble = new();

        WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
        {
            Bubble bubblePrefab = GameSettings.Instance.Bubbles[datas[j].newDatas.Count - 1];
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType category = datas[j].name;
            List<Bubble.Data> data = new();
            if (datas[j].newDatas != null)
            {
                data.AddRange(datas[j].newDatas);
            }

            Color bubbleColor = datas[j].overrideColor ?
                datas[j].bubbleColor :
                GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            Sprite sprite = GameSettings.Instance.BubbleSprites[j % GameSettings.Instance.BubbleSprites.Length];

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.Euler(new Vector3(0, 0, Random.Range(-GameSettings.Instance.RotationOffset, GameSettings.Instance.RotationOffset))));

                if (EnableRandomSize)
                {
                    float randomScale = Random.Range(minMultiplier, maxMultiplier);
                    bubble.transform.localScale = Vector3.one * randomScale;
                }

                bubble.Category = category;

                if (GameSettings.Instance.CanChangeColor)
                {
                    bubble.SetColor(bubbleColor);
                }

                if (GameSettings.Instance.CanUseDifferentSprites)
                {
                    bubble.SetBubbleSprite(sprite);
                }

                bubble.transform.DOScale(bubble.transform.localScale, 0.2f).From(0);

                bubble.SetName(data);

                if (bubble.Index == GameSettings.Instance.MergeCount - 1)
                {
                    blastableBubble.Add(bubble);
                }

            });

            if (j % 4 == 0)
                yield return _waitForSeconds0_1;
        }

        currentIndex = initialSpawns;
        yield return new WaitForSeconds(2);
        for (int i = 0; i < blastableBubble.Count; i++)
        {
            if (blastableBubble[i].Index == GameSettings.Instance.MergeCount - 1)
            {
                blastableBubble[i].Blast(() => InputHandler.Instance.OnSuccessfullMerge?.Invoke());
            }
            yield return new WaitForSeconds(.2f);
        }
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
            List<Bubble.Data> data = new();
            if (datas[j].newDatas != null)
            {
                data.AddRange(datas[j].newDatas);
            }
            Color bubbleColor = datas[j].overrideColor ?
                        datas[j].bubbleColor :
                        GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            Sprite sprite = GameSettings.Instance.BubbleSprites[j % GameSettings.Instance.BubbleSprites.Length];
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

                if (GameSettings.Instance.CanUseDifferentSprites)
                    bubble.SetBubbleSprite(sprite);

                bubble.SetName(data);
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
}
