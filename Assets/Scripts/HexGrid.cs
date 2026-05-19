using DT.GridSystem;
using UnityEngine;

public class HexGrid : HexGridSystem3D<Bubble>
{
    [SerializeField] LevelData levelData;

    private void Start()
    {
        SetUpGrid(new(levelData.gridWidth, levelData.gridHeight), 1);
        hexOrientation = levelData.hexOrientation == global::HexOrientation.PointyTop ? HexOrientation.PointyTop : HexOrientation.FlatTop;
        for (int i = 0; i < levelData.cells.Count; i++)
        {
            Vector2Int gridPos = levelData.cells[i].position;
            Bubble bubblePrefab = GameSettings.Instance.Bubbles[0];

            if (bubblePrefab != null && levelData.cells[i].category != null)
            {
                Vector3 worldPos = GetWorldPosition(gridPos);                
                Bubble.Data data = new()
                {
                    name = levelData.cells[i].text,
                    icon = levelData.cells[i].sprite,
                };


                var bubble = Instantiate(bubblePrefab, worldPos, Quaternion.identity);
                bubble.Category = levelData.cells[i].category;
                bubble.SetName(new() { data });
                bubble.RestorePositions();

                AddGridObject(worldPos, bubble);
            }
        }
    }
    ///// <summary>
    ///// Converts a world position into hex grid coordinates based on the selected orientation.
    ///// </summary>
    ///// <param name="worldPosition">The world position to convert.</param>
    ///// <param name="x">The resulting x grid index.</param>
    ///// <param name="y">The resulting y grid index.</param>
    //public override void GetGridPosition(Vector3 worldPosition, out int x, out int y)
    //{
    //    Vector3 offsetCenter = new Vector3(CellSize, 0, CellSize) * 0.5f;
    //    Vector3 local = worldPosition - transform.position - offsetCenter;

    //    float size = CellSize;

    //    switch (hexOrientation)
    //    {
    //        case HexOrientation.PointyTop:
    //            float width = size;
    //            float height = sqrt3Over2 * size;

    //            float q = (local.x + GridSize.x * width * 0.5f) / width;
    //            float r = (local.y + GridSize.y * height * 0.5f) / height;

    //            int row = Mathf.RoundToInt(r);
    //            float offsetX = (row % 2 == 0) ? 0 : 0.5f;
    //            int col = Mathf.RoundToInt(q - offsetX);

    //            x = Mathf.Clamp(col, 0, GridSize.x - 1);
    //            y = Mathf.Clamp(row, 0, GridSize.y - 1);
    //            break;

    //        case HexOrientation.FlatTop:
    //            float heightP = size;
    //            float widthP = sqrt3Over2 * size;

    //            float colF = (local.x + GridSize.x * widthP * 0.5f) / widthP;
    //            float rowF = (local.y + GridSize.y * heightP * 0.5f) / heightP;

    //            int colP = Mathf.RoundToInt(colF);
    //            float offsetY = (colP % 2 == 0) ? 0 : 0.5f;
    //            int rowP = Mathf.RoundToInt(rowF - offsetY);

    //            x = Mathf.Clamp(colP, 0, GridSize.x - 1);
    //            y = Mathf.Clamp(rowP, 0, GridSize.y - 1);
    //            break;

    //        default:
    //            x = y = 0;
    //            break;
    //    }
    //}/// <summary>
    // /// Converts grid coordinates to a world position for the specified orientation.
    // /// </summary>
    // /// <param name="x">The x-coordinate in the grid.</param>
    // /// <param name="y">The y-coordinate in the grid.</param>
    // /// <param name="snapToGrid">If true, returns the center of the hex; otherwise, the corner position.</param>
    // /// <returns>The calculated world position in 3D space.</returns>
    //public override Vector3 GetWorldPosition(int x, int y, bool snapToGrid = false)
    //{
    //    float size = CellSize;

    //    switch (hexOrientation)
    //    {
    //        case HexOrientation.PointyTop:
    //            float width = size;
    //            float height = sqrt3Over2 * size;
    //            float offsetX = (y % 2 == 0) ? 0 : width * 0.5f;
    //            float xPos = x * width + offsetX - GridSize.x * width * 0.5f;
    //            float yPos = y * height - GridSize.y * height * 0.5f;
    //            return new Vector3(xPos, yPos, 0) + transform.position + new Vector3(CellSize, CellSize, 0) * 0.5f;

    //        case HexOrientation.FlatTop:
    //            float heightP = size;
    //            float widthP = sqrt3Over2 * size;
    //            float offsetY = (x % 2 == 0) ? 0 : heightP * 0.5f;
    //            return new Vector3(x * widthP - GridSize.x * widthP * 0.5f, y * heightP + offsetY - GridSize.y * heightP * 0.5f, 0) + transform.position + new Vector3(CellSize, CellSize, 0) * 0.5f;

    //        default:
    //            return Vector3.zero;
    //    }
    //}
}
