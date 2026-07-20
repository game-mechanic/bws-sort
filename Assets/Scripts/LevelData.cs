using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BWS/Level Data")]
public class LevelData : ScriptableObject
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public bool overrideColor = false;
        [ColorUsage(false)] public Color bubbleColor = Color.white;
        public Bubble.Data data;
    }

    public List<Data> datas = new();
    public int initialSpawns = 15;
    public float minMultiplier = 1f;
    public float maxMultiplier = 2f;
}
