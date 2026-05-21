using UnityEngine;

[CreateAssetMenu]
public class BubbleType : ScriptableObject
{
    [SerializeField,ColorUsage(false)] Color color;

    public Color Color { get => color; }
}
