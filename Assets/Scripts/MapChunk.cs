using DG.Tweening;
using UnityEngine;
public class MapChunk : MonoBehaviour
{
    [SerializeField] BubbleType bubbleType;
    private SpriteRenderer highlightedBubble;
    [SerializeField, Range(0.5f, 1)] float scaleDownAmount = .75f;
    Vector3 initialPosition;
    Vector3 initialScale;
    public BubbleType BubbleType { get => bubbleType; }
    private void Awake()
    {
        highlightedBubble = GetComponent<SpriteRenderer>();
        // Highlight(false);
    }
    public void RecordPosition()
    {
        initialPosition = transform.position;
        initialScale = transform.localScale;
        transform.DOScale(initialScale * scaleDownAmount, 0.2f).From(0);
    }

    public void Highlight(bool v)
    {
        float targetValue = v ? 1f : 0;
        highlightedBubble.DOFade(targetValue, 0.2f);
    }

    public void StartDrag()
    {
        GetComponent<Collider2D>().enabled = false;
        highlightedBubble.sortingOrder = 1999;
        transform.DOScale(initialScale * 1.1f, 0.2f);
    }
    public void EndDrag()
    {
        highlightedBubble.sortingOrder = 50;
        transform.DOScale(initialScale, 0.2f);
    }

    public void ReturnBack()
    {
        transform.DOMove(initialPosition, 0.2f);
        GetComponent<Collider2D>().enabled = true;
        transform.DOScale(initialScale * scaleDownAmount, 0.2f);
    }
}
