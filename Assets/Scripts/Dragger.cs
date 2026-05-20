using DG.Tweening;
using UnityEngine;

public class Dragger : MonoBehaviour
{
    [SerializeField] Transform parent;
    void Start()
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            bool even = i % 2 == 0;

            float to = even ? 0.1f : 1;
            float from = even ? 1 : 0.1f;

            child.DOScale(from, 0.5f)
                .From(to)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear);
        }
    }
}