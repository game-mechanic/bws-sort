using DG.Tweening;
using UnityEngine;

public class Portal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {

        Transform transform1 = collision.transform;
        if (transform1.TryGetComponent(out Bubble b))
        {
            b.Container.CanMove = false;
        }
        transform1.DOMove(transform.position, 0.4f);
        transform1.DOScale(0, 0.4f);
    }
}