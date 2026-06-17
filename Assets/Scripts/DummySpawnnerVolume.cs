using DT.GridSystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DummySpawnnerVolume : HexGridSystem<DummySpawnnerVolume>
{
    [SerializeField] Vector2 size = Vector2.one;
    [SerializeField] List<CategoryManager.Data> datas = new();
    [SerializeField] List<CategoryManager.Data> combineList = new();
    [EditorButton]
    public void Generate()
    {
        int index = 0;

        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                Vector3 pos = GetWorldPosition(i, j);

                BubbleType category = datas[index % datas.Count].name;
                Bubble.Data data = datas[index % datas.Count].data;
                Color bubbleColor = datas[index % datas.Count].overrideColor ?
                    datas[index: datas.Count].bubbleColor :
                    GameSettings.Instance.BubbleColors[index % GameSettings.Instance.BubbleColors.Length];


                var bubble = PrefabUtility.InstantiatePrefab(GameSettings.Instance.Bubbles[0], transform) as Bubble;
                bubble.transform.SetPositionAndRotation(pos, Quaternion.identity);

                index++;

                bubble.Category = category;
                if (GameSettings.Instance.CanChangeColor)
                    bubble.SetColor(bubbleColor);
                bubble.SetName(new() { data });
                bubble.IsKinematic = RigidbodyType2D.Kinematic;
                EditorUtility.SetDirty(bubble);
                EditorUtility.SetDirty(bubble.Rb);
            }
        }
    }
    [EditorButton]
    public void Sufffle()
    {
        for (int i = 0; i < 10; i++)
        {
            int x = Random.Range(0, gridArray.Length);
            int y = Random.Range(0, gridArray.Length);

            (datas[x], datas[y]) = (datas[y], datas[x]);
        }
        EditorUtility.SetDirty(this);
    }

    [EditorButton]
    public void Combine()
    {
        datas.AddRange(combineList);
        EditorUtility.SetDirty(this);
    }


    [EditorButton]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
