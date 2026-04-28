using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Recorder.Encoder;
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
        currentIndex = initialSpawns;
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

    public void MergeBubbles(List<Bubble> list, Vector3[] positions)
    {
        List<Bubble> hoveredBubbles = new();
        hoveredBubbles.AddRange(list);
        int i = 0;
        Sequence moveUpSeq = DOTween.Sequence();

        foreach (var bubble in hoveredBubbles)
        {
            bubble.transform.DOKill();
            moveUpSeq.Join(bubble.transform.DOMove(horizontalAlignment.GetSlotPosition(i), .8f).SetEase(Ease.OutCirc));

            //ParticlePool.PlayRevealFx(bubble.transform.position);
            i++;
        }

        StartCoroutine(MergeOneByOne(hoveredBubbles));


        moveUpSeq.AppendInterval(01f);
        moveUpSeq.AppendCallback(() =>
        {
            DOVirtual.DelayedCall(0.2f, () =>
            {
                SpawnNewCategories(positions);
            });
            ChangeCategory();
        });
    }

    IEnumerator MergeOneByOne(List<Bubble> hoveredBubbles)
    {
        const float Duration = 0.5f;
        const float Interval = 0.2f;

        yield return new WaitForSeconds(1.1f);

        Bubble a = hoveredBubbles[1];
        const Ease outBack = Ease.InSine;
        a.transform.DOMove(horizontalAlignment.transform.position, Duration)
            .SetEase(outBack);
        Bubble b = hoveredBubbles[2];
        b.transform.DOMove(horizontalAlignment.transform.position, Duration)
            .SetEase(outBack);
        hoveredBubbles[3].transform.DOMove(horizontalAlignment.transform.position, Duration)
           .SetDelay(Interval)
           .SetEase(outBack);
        hoveredBubbles[0].transform.DOMove(horizontalAlignment.transform.position, Duration)
            .SetDelay(Interval * 2)
            .SetEase(outBack);

        yield return new WaitForSeconds(Duration);
        // IMPORTANT: TryMerge must RETURN the new bubble
        InputHandler.Instance.TryMerge(a, b, out Bubble current);
        //yield return new WaitForSeconds(0.1f);
        a = current;
        b = hoveredBubbles[3];
        yield return new WaitForSeconds(Interval);
        InputHandler.Instance.TryMerge(a, b, out current);
        //yield return new WaitForSeconds(0.1f);
        a = current;
        b = hoveredBubbles[0];

        yield return new WaitForSeconds(Interval);
        InputHandler.Instance.TryMerge(a, b, out current);
    }
    //IEnumerator MergeOneByOne(List<Bubble> hoveredBubbles)
    //{
    //    const float duration = 0.25f;

    //    yield return new WaitForSeconds(1.1f);

    //    Vector3 targetPos = horizontalAlignment.transform.position;

    //    Bubble current = hoveredBubbles[1];

    //    for (int i = 2; i < hoveredBubbles.Count + 1; i++)
    //    {
    //        Bubble next = hoveredBubbles[i % hoveredBubbles.Count];

    //        // Small random offset for natural feel
    //        Vector3 offset = Random.insideUnitSphere * 0.15f;
    //        offset.z = 0;

    //        Sequence seq = DOTween.Sequence();

    //        // 🔹 Anticipation (shrink slightly before move)
    //        seq.Join(current.transform.DOScale(0.9f, 0.1f));
    //        seq.Join(next.transform.DOScale(0.9f, 0.1f));

    //        // 🔹 Move with slight delay overlap
    //        seq.Append(current.transform.DOMove(targetPos + offset, duration)
    //            .SetEase(Ease.OutQuad));

    //        seq.Join(next.transform.DOMove(targetPos, duration)
    //            .SetEase(Ease.OutBack));

    //        yield return seq.WaitForCompletion();

    //        // 🔹 Merge
    //        InputHandler.Instance.TryMerge(current, next, out Bubble merged);

    //        // 🔹 Impact feedback
    //        merged.transform.localScale = Vector3.one * 0.8f;
    //        merged.transform.DOScale(1.2f, 0.15f)
    //            .SetEase(Ease.OutBack)
    //            .OnComplete(() =>
    //            {
    //                merged.transform.DOScale(1f, 0.1f);
    //            });

    //        current = merged;

    //        // Small breathing gap (feels better than instant chaining)
    //        yield return new WaitForSeconds(0.05f);
    //    }
    //}


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnPosition.position, radius);
    }
}
