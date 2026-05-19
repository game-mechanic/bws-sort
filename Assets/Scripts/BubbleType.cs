using UnityEngine;

[CreateAssetMenu(menuName = "GameData/Bubble Type", fileName = "NewBubbleType")]
public class BubbleType : ScriptableObject
{
    [SerializeField] private Color color = Color.white;

    public Color Color => color;
}
