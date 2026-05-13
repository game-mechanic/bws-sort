using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleSpawnner : MonoBehaviour
{
    [SerializeField] HorizontalAlignment horizontalAlignment;
    [SerializeField] List<CategoryManager.Data> datas = new();
    IEnumerator Start()
    {
        WaitForSeconds _waitForSeconds0_1 = new(0.1f);
        Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];
        for (int j = 0; j < datas.Count; j++)
        {
            Vector3 pos = horizontalAlignment.GetSlotPosition(j % 4);
            BubbleType category = datas[j].name;
            Bubble.Data data = datas[j].data;
            Color bubbleColor = datas[j].overrideColor ?
                datas[j].bubbleColor :
                GameSettings.Instance.BubbleColors[j % GameSettings.Instance.BubbleColors.Length];

            DOVirtual.DelayedCall(Random.Range(0.1f, 0.2f), () =>
            {
                var bubble = Instantiate(bubblePrefab, pos, Quaternion.identity);
                bubble.Category = category;
                if (GameSettings.Instance.CanChangeColor)
                    bubble.SetColor(bubbleColor);
                bubble.SetName(new() { data });
            });
            if (j % 4 == 0)
                yield return _waitForSeconds0_1;
        }
    }
}
