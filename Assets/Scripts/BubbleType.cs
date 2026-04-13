using UnityEngine;

[CreateAssetMenu]
public class BubbleType : ScriptableObject
{
    [SerializeField] Material material;

    public Material Material { get => material; }
}