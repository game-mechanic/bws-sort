using DG.Tweening;
using UnityEngine;

public class BubbleMultiplier : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent(out DummyBubble dummyBubble))
        {
            return;
        }

        foreach (var data in dummyBubble.datas)
        {
            Vector3 position = transform.position;
            Bubble bubble = Instantiate(GameSettings.Instance.Bubbles[0], position, Quaternion.identity);

            BubbleType category = data.name;


            //Color bubbleColor = datas[j].overrideColor ?
            //    datas[j].bubbleColor :
            //    GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            bubble.Category = category;

            //if (GameSettings.Instance.CanChangeColor)
            //    bubble.SetColor(bubbleColor);
            bubble.Rb.linearVelocityX = Random.Range(-.5f, .5f);
            bubble.SetName(new() { data.data });
            bubble.transform.DOScale(bubble.transform.localScale, .5f)
                .From(0.4f)
                //.OnStart(() => bubble.HideText())
                .SetDelay(0.2f)
                .SetEase(Ease.InCirc)
                .OnComplete(() =>
                {
                    DOVirtual.DelayedCall(.3f, () => { bubble.ScaleUptext(); });
                });
            bubble.HideText();
        }
        Destroy(collision.gameObject);
    }
}
