using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Tube : MonoBehaviour
{
    [SerializeField] List<CategoryManager.Data> datas;


    [SerializeField] List<Bubble> bubbles = new();
    [SerializeField] Transform startPosition;
    bool isHighlighted;


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
            Vector3 position = GameSettings.GetStackedPosition(startPosition.position, i, 1f);
            Bubble bubble = Instantiate(GameSettings.Instance.Bubbles[0], position, Quaternion.identity, transform);

            BubbleType category = datas[i].name;
            Bubble.Data data = datas[i].data;
            Color bubbleColor = datas[i].overrideColor ?
                datas[i].bubbleColor :
                GameSettings.Instance.BubbleColors[i % GameSettings.Instance.BubbleColors.Length];

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

            bubbles[i].transform.DOMove(position, 0.1f);
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
            bubble.transform.DOPath(
                new Vector3[] {
                    GameSettings.GetStackedPosition(tube.startPosition.position, GameSettings.Instance.JumpHeight, GameSettings.Instance.NormalOffset),
                    GameSettings.GetStackedPosition(startPosition.position, GameSettings.Instance.JumpHeight, GameSettings.Instance.NormalOffset),
                    targetPosition,
                },
                GameSettings.Instance.JumpDuration,
                pathType: PathType.CatmullRom).OnComplete(() => bubble.Highlight(false));

            bubbles.Add(bubble);
        }
        tube.Highlight(false);
    }
}
