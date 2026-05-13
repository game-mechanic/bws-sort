using DG.Tweening;
using System.Collections;
using UnityEngine;

public class MoveOverTime : MonoBehaviour
{
    [SerializeField] Vector3 direction;
    [SerializeField] float moveUpdistance = 1;
    [SerializeField] float moveUpDuration = 0.5f;
    [SerializeField] float speed;
    [SerializeField] float startDelay;
    bool canPan = false;

    IEnumerator Start()
    {
        if (startDelay > 0)
        {
            canPan = false;
        }
        else
        {
            canPan = true;
        }
        yield return new WaitForSeconds(startDelay);
        canPan = true;
    }
    private void OnEnable()
    {
        InputHandler.Instance.OnSuccessfullMerge.AddListener(MoveUp);
    }
    void MoveUp()
    {
        canPan = false;
        transform.DOMove(transform.position - direction.normalized * moveUpdistance, moveUpDuration)
            .SetEase(Ease.OutExpo)
            .OnComplete(() => canPan = true);
    }
    void Update()
    {
        if (!canPan) return;

        transform.position = Vector3.Lerp(transform.position, transform.position + direction.normalized, speed * Time.deltaTime);
    }
}
