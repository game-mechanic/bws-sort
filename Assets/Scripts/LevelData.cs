using System.Collections.Generic;
using UnityEditor;
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

    [Header("Editor Tools")]
    [Tooltip("Drag another LevelData here and click Append Data to add its items to the end of this list")]
    [SerializeField] private LevelData levelDataToAppend;

    [EditorButton("Append Data")]
    public void AppendData()
    {
        EditorGUILayout.BeginHorizontal();
        if (levelDataToAppend == null)
        {
            Debug.LogWarning("No LevelData assigned to append from.");
            return;
        }

        foreach (var item in levelDataToAppend.datas)
        {
            // Deep copy the data to avoid reference issues
            datas.Add(new Data
            {
                name = item.name,
                overrideColor = item.overrideColor,
                bubbleColor = item.bubbleColor,
                data = new Bubble.Data
                {
                    name = item.data.name,
                    icon = item.data.icon,
                    animationClip = item.data.animationClip,
                    showBothTxtAndImg = item.data.showBothTxtAndImg
                }
            });
        }

        Debug.Log($"Appended {levelDataToAppend.datas.Count} items from {levelDataToAppend.name}. Total items now: {datas.Count}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    [EditorButton("Clear Data")]
    public void ClearData()
    {
        if (levelDataToAppend == null)
        {
            Debug.LogWarning("No LevelData assigned to append from.");
            return;
        }

        levelDataToAppend = null;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        EditorGUILayout.EndHorizontal();
    }
}
