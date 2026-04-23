using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CategoryManager : Singleton<CategoryManager>
{

    [SerializeField] int initialSpawns = 15;
    [SerializeField] TextMeshPro text;
    [SerializeField] BubbleType[] categories;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new();
    public BubbleType CurrentType => categories[currentIndex % categories.Length];
    /* IEnumerator Start()
     {
         WaitForSeconds _waitForSeconds0_1 = new(0.1f);
         Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
         for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
         {
             Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
             BubbleType category = datas[j].name;
             Bubble.Data data = datas[j].data;

             DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
             {
                 var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
                 bubble.Category = category;
                 bubble.SetName(new() { data });
             });
             if (j % 4 == 0)
                 yield return _waitForSeconds0_1;
         }
         currentIndex = initialSpawns;
     }*/
    private void Start()
    {
        Redraw();
    }

    private void Redraw()
    {
        text.text = CurrentType.name;
    }

    public void HideText()
    {
        text.transform.DOScale(0, 0.3f);
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


    //public void SpawnNewCategories()
    //{
    //    Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

    //    int end = Mathf.Min(currentIndex + 4, datas.Count);
    //    for (int j = currentIndex; j < end; j++)
    //    {
    //        Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
    //        BubbleType category = datas[j].name;
    //        Bubble.Data data = datas[j].data;

    //        DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
    //        {
    //            CreateBubble(bubblePrefab, pos, category, data);
    //        });
    //    }
    //    currentIndex += 4;
    //}

    public static Bubble CreateBubble(Vector3 pos, BubbleType category, Bubble.Data data)
    {
        var bubble = Instantiate(GameSettings.Instance.Bubbles[0], pos, Quaternion.identity);
        bubble.Category = category;
        bubble.SetName(new() { data });
        return bubble;
    }

    public void ChangeCategory()
    {
        currentIndex++;
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
        text.transform.DOScale(0.1f, .3f).SetEase(Ease.OutBack);
        Redraw();

    }

    public int GetCategoryCount(BubbleType category)
    {
        if (!categoryCounts.ContainsKey(category)) return -1;
        return categoryCounts[category];
    }
}
