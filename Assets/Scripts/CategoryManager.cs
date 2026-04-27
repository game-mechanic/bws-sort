using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class CategoryManager : Singleton<CategoryManager>
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public Bubble.Data data;
    }
    [SerializeField] HorizontalAlignment horizontalAlignment;

    [SerializeField] List<Data> datas = new();
    [SerializeField] private Transform spawnPosition;
    [SerializeField] float radius = 1f;
    [SerializeField] int initialSpawns = 15;
    int currentIndex = 0;
    Dictionary<BubbleType, int> categoryCounts = new Dictionary<BubbleType, int>();

    //IEnumerator Start()
    //{
    //    WaitForSeconds _waitForSeconds0_1 = new(0.1f);
    //    Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
    //    for (int j = 0; j < Mathf.Min(initialSpawns, datas.Count); j++)
    //    {
    //        Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
    //        BubbleType category = datas[j].name;
    //        Bubble.Data data = datas[j].data;

    //        DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
    //        {
    //            var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
    //            bubble.Category = category;
    //            bubble.SetName(new() { data });
    //        });
    //        if (j % 4 == 0)
    //            yield return _waitForSeconds0_1;
    //    }
    //    currentIndex = initialSpawns;
    //}
    private void Start()
    {
        float angleOffset = 360f / initialSpawns;
        angleOffset *= Mathf.Deg2Rad;
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        for (int i = 0; i < initialSpawns; i++)
        {
            GenerateBubble(bubblePrefab: bubblePrefab,
                pos: spawnPosition.position + new Vector3(Mathf.Sin(angleOffset * i) * radius, Mathf.Cos(angleOffset * i) * radius, 0),
                category: datas[i].name,
                data: datas[i].data);
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


    public void SpawnNewCategories(Vector3[] positions)
    {
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

        int end = Mathf.Min(currentIndex + positions.Length, datas.Count);
        for (int j = currentIndex; j < end; j++)
        {
            Vector3 pos = positions[j - currentIndex];
            BubbleType category = datas[j].name;
            Bubble.Data data = datas[j].data;

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.5f), () =>
            {
                GenerateBubble(bubblePrefab, pos, category, data);
            });
        }
        currentIndex += positions.Length;
    }

    private static Bubble GenerateBubble(Bubble bubblePrefab, Vector3 pos, BubbleType category, Bubble.Data data)
    {
        var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
        bubble.transform.DOScale(1, 0.3f).From(0).SetEase(Ease.OutBack);
        bubble.Category = category;
        bubble.SetName(new() { data });
        return bubble;
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

    public void MergeBubbles(List<Bubble> a, Vector3[] positions)
    {
        List<Bubble> hoveredBubbles = new();
        hoveredBubbles.AddRange(a);
        int i = 0;
        Sequence moveUpSeq = DOTween.Sequence();
        Sequence mergeSequence = DOTween.Sequence();

        foreach (var bubble in hoveredBubbles)
        {
            bubble.transform.DOKill();
            moveUpSeq.Join(bubble.transform.DOMove(horizontalAlignment.GetSlotPosition(i), 01f).SetEase(Ease.InBack));
            mergeSequence.Join(bubble.transform.DOMove(horizontalAlignment.transform.position, 0.5f).SetEase(Ease.InSine));

            ParticlePool.PlayRevealFx(bubble.transform.position);
            i++;
        }
        moveUpSeq.Append(mergeSequence);
        moveUpSeq.AppendCallback(() =>
        {
            List<Bubble.Data> data = new();
            foreach (var bubble in hoveredBubbles)
            {
                data.AddRange(bubble.Names);
                Destroy(bubble.gameObject);
            }
            var newBubble = Instantiate(GameSettings.Instance.Bubbles[3]);
            newBubble.transform.SetPositionAndRotation(horizontalAlignment.transform.position, Quaternion.identity);

            newBubble.Category = hoveredBubbles[0].Category;
            newBubble.SetName(data);
            newBubble.Blast();
            DOVirtual.DelayedCall(0.2f, () =>
            {
                SpawnNewCategories(positions);
            });
            ChangeCategory();
        });
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnPosition.position, radius);
    }
}
