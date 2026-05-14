using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class CategoryManager : Singleton<CategoryManager>
{
    [System.Serializable]
    public class CategoryNames
    {
        [SerializeField] BubbleType category;
        [SerializeField] public string[] names;
    }

    [SerializeField] BubbleType[] allCategories;
    [SerializeField] Transform ui;
    [SerializeField] TextMeshPro text;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();
    public BubbleType CurrentCategory => allCategories[currentIndex % allCategories.Length];

    [SerializeField] List<CategoryNames> bubbleNames;
    [SerializeField] private float spacing;
    [SerializeField] HorizontalAlignment horizontalAlignment;
    private void Start()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)GameSettings.Instance.SelectedLanguage];
        Redraw();
    }

    private void Redraw()
    {
        text.text = CurrentCategory.name;
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

    public void SpawnNewBubbles()
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        int i = 0;
        foreach (var name in bubbleNames[currentIndex % bubbleNames.Count].names)
        {
            Vector3 pos = horizontalAlignment.GetSlotPosition(i);
            DOVirtual.DelayedCall(Random.Range(0.01f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
                bubble.SetName(new() { name });
            });
            i++;
        }
    }
    public void ReduceCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return;
        categoryCounts[category]--;
        if (categoryCounts[category] <= 0)
        {
            categoryCounts.Remove(category);
            ChangeCategory();
        }
    }

    void ChangeCategory()
    {
        currentIndex++;
        SpawnNewBubbles();


        Vector3 squishedScale = new Vector3(1.2f, 0.8f, 1);
        Vector3 originalScale = new Vector3(.8f, 1.2f, 1);
        Vector3 startPosition = transform.position;
        Sequence moveOutSequence = DOTween.Sequence();

        // Step 2: When lid reaches destination, squish the cart
        moveOutSequence.Append(DOTween.Sequence()
                .Append(transform.DOScale(squishedScale, 0.2f))
                .Append(transform.DOScale(originalScale, 0.1f)));

        // Step 3: Move the cart up and restore scale simultaneously
        moveOutSequence.Append(transform.DOMove(startPosition + Vector3.up * GameSettings.Instance.PropSpawnPositionHieght, .5f).SetEase(Ease.OutExpo));

        moveOutSequence.AppendCallback(() =>
        {
            Redraw();
        });
        moveOutSequence.Append(transform.DOMove(startPosition, .5f).SetEase(Ease.InExpo));
        moveOutSequence.Join(transform.DOScale(originalScale, 0.1f));
        moveOutSequence.Append(transform.DOScale(squishedScale, 0.2f));
        moveOutSequence.Append(transform.DOScale(Vector3.one, 0.1f));
    }
}
