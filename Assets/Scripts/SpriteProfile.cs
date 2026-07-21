using UnityEngine;

[CreateAssetMenu]
public class SpriteProfile : ScriptableObject
{
    [SerializeField] Sprite[] bubbleSprites;

    public Sprite[] BubbleSprites { get => bubbleSprites; }
}
