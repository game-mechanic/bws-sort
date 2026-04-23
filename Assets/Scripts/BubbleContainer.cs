using Dreamteck.Splines;
using UnityEngine;

public class BubbleContainer : MonoBehaviour
{
    Bubble bubble;
    [SerializeField] GameObject ghost;
    float moveTime;

    public Bubble Bubble { get => bubble; set => bubble = value; }

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
        this.moveTime = Mathf.Lerp(this.moveTime, moveTime, Time.deltaTime * 5);
        var point = splineComputer.Evaluate(this.moveTime);
        transform.position = point.position;
    }
}
