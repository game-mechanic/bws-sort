using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "BWS/Level Data")]
public class LevelData : ScriptableObject
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public bool overrideColor = false;
        [ColorUsage(false)] public Color bubbleColor = Color.white;
        public List<Bubble.Data> newDatas = new();
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
            // Deep copy the data list to avoid reference issues
            List<Bubble.Data> copiedBubbleData = new List<Bubble.Data>();
            if (item.newDatas != null)
            {
                foreach (var bData in item.newDatas)
                {
                    copiedBubbleData.Add(new Bubble.Data
                    {
                        name = bData.name,
                        icon = bData.icon,
                        animationClip = bData.animationClip,
                        showBothTxtAndImg = bData.showBothTxtAndImg
                    });
                }
            }

            datas.Add(new Data
            {
                name = item.name,
                overrideColor = item.overrideColor,
                bubbleColor = item.bubbleColor,
                newDatas = copiedBubbleData
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
