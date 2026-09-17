using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent(typeof(SortingLayer))]
public class MapChunk : MonoBehaviour
{
    [SerializeField] BubbleType bubbleType;
    private SpriteRenderer highlightedBubble;
    [SerializeField] SpriteRenderer textSprite;
    [SerializeField] TextMeshPro text;
    [SerializeField, Range(0.5f, 2)] float scaleDownAmount = .75f;
    Vector3 initialPosition;
    Vector3 initialScale;
    public BubbleType BubbleType { get => bubbleType; }
    SortingGroup sortingLayer;
    private void Awake()
    {
        highlightedBubble = GetComponent<SpriteRenderer>();
        sortingLayer = GetComponent<SortingGroup>();
        // Highlight(false);
        if (transform.childCount > 0)
        {
            textSprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        }
        if (textSprite)
        {
            sortingLayer.sortingOrder = 5;

            textSprite.gameObject.SetActive(true);
            highlightedBubble.enabled = false;
            GetComponent<PolygonCollider2D>().enabled = false;
            if (textSprite.transform.childCount > 0)
            {
                text = textSprite.transform.GetChild(0).GetComponent<TextMeshPro>();
                text.text = name;
            }
        }

    }
    public void RecordPosition()
    {
        initialPosition = transform.position;
        initialScale = transform.localScale;
        transform.DOScale(initialScale * scaleDownAmount, 0.2f).From(0);
    }

    public void Highlight(bool v, Color color)
    {
        highlightedBubble.color = color;
        Highlight(v);
    }
    public void Highlight(bool v)
    {
        float targetValue = v ? .75f : 0;
        highlightedBubble.DOFade(targetValue, 0.2f);
    }

    public void StartDrag()
    {
        GetComponent<Collider2D>().enabled = false;
        sortingLayer.sortingOrder = 1999;
        transform.DOScale(initialScale * 1.1f, 0.2f);
    }
    public void EndDrag()
    {
        sortingLayer.sortingOrder = 5;

        if (textSprite)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(0f, 0.2f))
                    .AppendCallback(() =>
                    {
                        textSprite.gameObject.SetActive(false);
                        highlightedBubble.enabled = true;
                    })
                    .Append(transform.DOScale(initialScale, 0.2f));
            GetComponent<BoxCollider2D>().enabled = false;
            highlightedBubble.color = Color.white;
        }
        else
        {
            transform.DOScale(initialScale, 0.2f);
        }
    }

    public void ReturnBack()
    {
        sortingLayer.sortingOrder = 5;

        if (textSprite)
        {
            textSprite.transform.DOComplete();
            textSprite.transform.DOShakePosition(0.5f, Vector3.right * 1);
        }
        transform.DOMove(initialPosition, .8f).SetEase(Ease.InSine);
        GetComponent<Collider2D>().enabled = true;
        transform.DOScale(initialScale * scaleDownAmount, 0.2f);
    }
}
