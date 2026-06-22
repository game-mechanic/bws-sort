using DT.GridSystem;
using System.Collections.Generic;
using UnityEngine;
using DT.GridSystem;

public class GridGenerator : GridSystem3D<float>
{
    public IEnumerable<Vector2Int> GetBorders()
    {
        for(int i = 0; i < gridSize.x; i++)
        {
            yield return new Vector2Int(i, 0);
            yield return new Vector2Int(i, gridSize.y - 1);
        }

        for(int j = 1; j < gridSize.y - 1; j++)
        {
            yield return new Vector2Int(0, j);
            yield return new Vector2Int(gridSize.x - 1, j);
        }
    }

    public IEnumerable<Vector3> GetBordersPos()
    {
        for(int i = 0; i < gridSize.x; i++)
        {
            yield return GetWorldPosition(i, 0);
            yield return GetWorldPosition(i, gridSize.y - 1);
        }

        for (int j = 1; j < gridSize.y - 1; j++)
        {
            yield return GetWorldPosition(0, j);
            yield return GetWorldPosition(gridSize.x - 1, j);
        }
    }
}
