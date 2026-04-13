using UnityEngine;

[CreateAssetMenu]
public class ColorType : ScriptableObject
{
    [SerializeField] Material material;

    public Material Material { get => material; }
}