using UnityEngine;

[CreateAssetMenu]
public class BubbleProfile : ScriptableObject
{
    [SerializeField] Bubble[] bubbles;

    public Bubble[] Bubbles { get => bubbles; set => bubbles = value; }
}