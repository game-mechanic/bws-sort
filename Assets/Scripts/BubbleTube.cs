using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleTube : MonoBehaviour
{
    [SerializeField] Transform startPosition;
    [SerializeField] Transform parent;
    [SerializeField] float offset;
    [SerializeField] bool spawnOnStart;
    [SerializeField] CategoryManager.Data[] data;
    [SerializeField] List<Bubble> stack = new();

    private IEnumerator Start()
    {
        if (!spawnOnStart)
        {
            yield break;
        }
        for (int i = 0; i < data.Length; i++)
        {
            Bubble bubble = Instantiate(GameSettings.Instance.Bubbles[0], GetStackedPostion(i), Quaternion.identity, parent);
            bubble.Category = data[i].name;
            bubble.SetName(new() { data[i].data });


           yield return new WaitForSeconds(0.1f);
        }
    }
    public Vector3 GetStackedPostion(int i)
    {
        return new();
    }
}
