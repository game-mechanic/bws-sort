using Dreamteck.Splines;
using UnityEngine;

public class BubbleContainer : MonoBehaviour
{
    Bubble bubble;
    [SerializeField] GameObject ghost;
    float moveTime;

    private bool canMove = true;
    public Bubble Bubble { get => bubble; set => bubble = value; }
    public float MoveTime { get => moveTime; set => moveTime = value; }
    public bool CanMove { get => canMove; set => canMove = value; }

    private void OnDestroy()
    {
        ParticlePool.PlayRevealFx(transform.position);
    }

    public void PickBubble(bool active)
    {
        Bubble.transform.SetParent(active ? null : transform);
    }
    public void SetGhost(bool active)
    {
        ghost.SetActive(active);
    }

    public void UpdatePosition(SplineComputer splineComputer, float moveTime)
    {
        if (!canMove) return;
        this.MoveTime = Mathf.Lerp(this.MoveTime, moveTime, Time.deltaTime * 5);
        var point = splineComputer.Evaluate(this.MoveTime);
        transform.position = point.position;
    }
    public void UpdatePositionImmediate(SplineComputer splineComputer, float moveTime)
    {
        if (!canMove) return;
        this.MoveTime = moveTime;
        var point = splineComputer.Evaluate(this.MoveTime);
        transform.position = point.position;
    }
}
