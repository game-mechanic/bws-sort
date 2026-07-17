using UnityEngine;

// [SelectionBase]
public class BubbleSlot : MonoBehaviour
{
    [SerializeField] bool shouldOverrideColor;
    [SerializeField] Color color = Color.white;
    [SerializeField] bool isPlayable;
    public bool ShouldOverrideColor => shouldOverrideColor;
    public Color Color => color;

    public bool IsPlayable { get => isPlayable; }

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
