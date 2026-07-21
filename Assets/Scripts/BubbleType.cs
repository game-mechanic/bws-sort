using UnityEngine;

[CreateAssetMenu]
public class BubbleType : ScriptableObject
{
    [SerializeField, ColorUsage(false)] Color color;
    [SerializeField] BubbleType nextCategory;
    [SerializeField] float size;
    public Color Color { get => color; }
    public BubbleType NextCategory { get => nextCategory; }
    public float Size { get => size; }
}
