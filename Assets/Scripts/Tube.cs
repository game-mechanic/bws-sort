using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube : MonoBehaviour
{
    private const float HighlightDuration = 0.1f;
    [SerializeField] List<CategoryManager.Data> datas;


    [SerializeField] List<Bubble> bubbles = new();
    [SerializeField] Transform startPosition;
    bool isHighlighted;

    static int color;

    public bool IsHighlighted { get => isHighlighted; private set => isHighlighted = value; }
    public BubbleType TopCategory
    {
        get
        {
            if (bubbles.Count == 0) return null;
            return bubbles[^1].Category;
        }
    }

    private void Start()
    {
        for (int i = 0; i < datas.Count; i++)
        {
            Vector3 position = GameSettings.GetStackedPosition(startPosition.position, i, GameSettings.Instance.NormalOffset);
            Bubble bubble = Instantiate(GameSettings.Instance.Bubbles[0], position, Quaternion.identity, transform);

            BubbleType category = datas[i].name;
            Bubble.Data data = datas[i].data;
            Color bubbleColor = datas[i].overrideColor ?
                datas[i].bubbleColor :
                GameSettings.Instance.BubbleColors[color++ % GameSettings.Instance.BubbleColors.Length];

            bubble.Category = category;
            if (GameSettings.Instance.CanChangeColor)
                bubble.SetColor(bubbleColor);
            bubble.SetName(new() { data });
            bubbles.Add(bubble);
        }
    }



    public void Highlight(bool highlight)
    {
        if (bubbles.Count == 0) return;
        BubbleType bubbleType = bubbles[^1].Category;
        int startPos = GetStartPosition(type: bubbleType);

        int p = Mathf.Max(startPos - 1, 0);


        for (int i = bubbles.Count - 1; i >= startPos; i--)
        {
            Vector3 position;
            if (highlight)
            {
                position = GameSettings.GetStackedPosition(bubbles[p].transform.position, i - startPos, GameSettings.Instance.HighlightedOffset) + Vector3.up * 1.5f;
            }
            else
            {
                position = GameSettings.GetStackedPosition(startPosition.position, i, GameSettings.Instance.NormalOffset);
            }
            bubbles[i].DOKill();
            bubbles[i].transform.DOMove(position, HighlightDuration);
            bubbles[i].Highlight(highlight);
        }
    }

    int GetStartPosition(BubbleType type)
    {
        for (int i = bubbles.Count - 1; i >= 0; i--)
        {
            if (bubbles[i].Category != type)
            {
                return i + 1;
            }
        }
        return 0;
    }

    public bool CanRecieve(BubbleType topCategory)
    {
        return IsEmpty() || (bubbles.Count < 4 && topCategory == TopCategory);
    }

    public bool IsEmpty()
    {
        return bubbles.Count == 0;
    }

    public void Recieve(Tube tube)
    {
        if (tube == null || tube.bubbles.Count == 0)
            return;

        // Calculate available space in this tube (max 4 bubbles)
        int spaceAvailable = 4 - bubbles.Count;
        if (spaceAvailable <= 0)
            return;

        // Get the top category from source tube
        BubbleType sourceCategory = tube.bubbles[^1].Category;

        // If this tube isn't empty, categories must match
        if (bubbles.Count > 0 && TopCategory != sourceCategory)
            return;

        // Count consecutive matching bubbles from the top of source tube
        int matchingCount = 0;
        for (int i = tube.bubbles.Count - 1; i >= 0 && tube.bubbles[i].Category == sourceCategory; i--)
        {
            matchingCount++;
        }

        // Determine how many bubbles to transfer (take only what fits)
        int toTransfer = Mathf.Min(spaceAvailable, matchingCount);

        // Transfer bubbles from source tube to this tube
        for (int i = 0; i < toTransfer; i++)
        {
            // Remove from top of source tube
            Bubble bubble = tube.bubbles[^1];
            tube.bubbles.RemoveAt(tube.bubbles.Count - 1);

            // Add to this tube
            bubble.transform.SetParent(transform);
            Vector3 targetPosition = GameSettings.GetStackedPosition(startPosition.position, bubbles.Count, GameSettings.Instance.NormalOffset);
            bool canCheckCompletion = i == toTransfer - 1;
            Vector3 one = GameSettings.GetStackedPosition(tube.startPosition.position, GameSettings.Instance.JumpHeight, GameSettings.Instance.NormalOffset);
            Vector3 two = GameSettings.GetStackedPosition(startPosition.position, GameSettings.Instance.JumpHeight, GameSettings.Instance.NormalOffset);

            one.y = Mathf.Max(one.y, two.y);
            two.y = one.y;

            bubble.transform.DOPath(
                new Vector3[] {
                    one,
                    two,
                    targetPosition,
                },
                GameSettings.Instance.JumpDuration,
                pathType: PathType.CatmullRom)
                .SetEase(GameSettings.Instance.JumpTween)
                .OnComplete(() =>
                {
                    bubble.Highlight(false);
                    if (canCheckCompletion)
                    {
                        TryMerge();
                    }
                });

            bubbles.Add(bubble);
        }
        tube.Highlight(false);
    }

    void TryMerge()
    {
        if (bubbles.Count < 4) return;
        BubbleType type = bubbles[^1].Category;
        foreach (var bubble in bubbles)
        {
            if (bubble.Category != type) return;
        }

        //Highlight(true);

        StartCoroutine(MergeOneByOne(bubbles, GameSettings.GetStackedPosition(startPosition.position, 4, GameSettings.Instance.NormalOffset) + Vector3.up * GameSettings.Instance.Offset)) ;
    }
    IEnumerator MergeOneByOne(List<Bubble> hoveredBubbles, Vector3 targetPosition)
    {
        const float Duration = .8f;
        const float Interval = 0.2f;

        //yield return new WaitForSeconds(HighlightDuration);

        Bubble a = hoveredBubbles[3];
        const Ease outBack = Ease.InOutSine;

        a.transform.DOMove(targetPosition, Duration)
            .SetEase(outBack);
        Bubble b = hoveredBubbles[2];

        b.transform.DOMove(targetPosition, Duration)
            .SetDelay(Interval)
            .SetEase(outBack);

        hoveredBubbles[1].transform.DOMove(targetPosition, Duration)
           .SetDelay(Interval * 2)
           .SetEase(outBack);

        hoveredBubbles[0].transform.DOMove(targetPosition, Duration)
            .SetDelay(Interval * 3)
            .SetEase(outBack);

        yield return new WaitForSeconds(Duration);
        // IMPORTANT: TryMerge must RETURN the new bubble
        InputHandler.Instance.TryMerge(a, b, out Bubble current);
        yield return new WaitForSeconds(0.1f);
        a = current;
        b = hoveredBubbles[1];
        yield return new WaitForSeconds(Interval);
        InputHandler.Instance.TryMerge(a, b, out current);
        //yield return new WaitForSeconds(0.1f);
        a = current;
        b = hoveredBubbles[0];

        yield return new WaitForSeconds(Interval);
        InputHandler.Instance.TryMerge(a, b, out current);
        bubbles.Clear();
    }
}
