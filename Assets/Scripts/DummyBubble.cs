using System.Collections.Generic;
using UnityEngine;

public class DummyBubble : MonoBehaviour
{
    public List<CategoryManager.Data> datas;

    public void SetData(List<CategoryManager.Data> newDatas)
    {
        datas = newDatas;
    }
}
