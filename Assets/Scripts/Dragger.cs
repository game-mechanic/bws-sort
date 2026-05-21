using DG.Tweening;
using UnityEngine;

public class Dragger : MonoBehaviour
{
    [SerializeField] Transform parent;
    [SerializeField] Transform shooter;
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
    public void WarmUp()
    {
        shooter.transform.DOScaleY(0.8f, 0.2f)
            .SetEase(Ease.Linear);
    }
    public void Shoot()
    {
        shooter.transform.DOScaleY(1, 0.2f)
            .SetEase(Ease.OutBack);
    }
}