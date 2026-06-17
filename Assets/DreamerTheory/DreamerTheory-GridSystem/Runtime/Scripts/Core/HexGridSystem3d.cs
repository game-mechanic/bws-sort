using System.Collections.Generic;
using UnityEngine;

namespace DT.GridSystem
{
    public class HexGridSystem<TGridObject> : GridSystem<TGridObject>
    {
        /// <summary>
        /// Orientation of the hex tiles: FlatTop or PointyTop.
        /// </summary>
        public enum HexOrientation
        {
            FlatTop,
            PointyTop
        }
        public enum GridPlane
        {
            XY,
            XZ
        }

        [SerializeField] protected HexOrientation hexOrientation = HexOrientation.FlatTop;
        [SerializeField] private GridPlane plane = GridPlane.XY;
        protected float sqrt3;
        protected float sqrt3Over2;


        protected static readonly Vector2Int[] evenQ = new Vector2Int[]
        {
            new(+1, 0), new(0, -1), new(-1, -1), new(-1, 0), new(-1, +1), new(0, +1)
        };
        protected static readonly Vector2Int[] oddQ = new Vector2Int[]
        {
            new(+1, 0), new(+1, -1), new(0, -1), new(-1, 0), new(0, +1), new(+1, +1)
        };
        protected static readonly Vector2Int[] evenR = new Vector2Int[]
        {
            new(+0, 1), new(-1, 0), new(-1, -1),
            new(0, -1), new(+1, -1), new(1, 0)
        };
        protected static readonly Vector2Int[] oddR = new Vector2Int[]
        {
            new(0, 1), new(-1, +1), new(-1, 0),
            new(0, -1), new(1, 0), new(+1, +1)
        };

        protected GridPlane Plane { get => plane; set => plane = value; }

        /// <summary>
        /// Initializes mathematical constants for hexagonal calculations.
        /// </summary>
        protected override void Awake()
        {
            sqrt3 = Mathf.Sqrt(3f);
            sqrt3Over2 = sqrt3 / 2f;
            base.Awake();
        }
        /// <summary>
        /// Returns a list of valid neighboring cell positions for a given hex cell in the grid,
        /// taking into account the current hex orientation (FlatTop or PointyTop) and the grid boundaries.
        /// </summary>
        /// <param name="pos">The grid position (as a Vector2Int) for which to find neighbors.</param>
        /// <returns>
        /// A list of Vector2Int positions representing the adjacent cells that are within the grid bounds.
        /// </returns>
        /// <remarks>
        /// The method automatically selects the correct neighbor offset pattern based on the grid's orientation
        /// and the parity (even/odd) of the relevant coordinate, ensuring accurate neighbor calculation for both
        /// flat-topped and pointy-topped hex layouts.
        /// </remarks>
        public override List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            Vector2Int[] directions;
            if (hexOrientation == HexOrientation.PointyTop)
            {
                if (pos.y % 2 == 0)
                {
                    directions = evenQ;
                }
                else
                {
                    directions = oddQ;
                }
            }
            else
            {
                if (pos.x % 2 == 0)
                {
                    directions = evenR;
                }
                else
                {
                    directions = oddR;
                }
            }
            List<Vector2Int> result = new();
            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                if (neighbor.x >= 0 && neighbor.y >= 0 && neighbor.x < GridSize.x && neighbor.y < GridSize.y)
                    result.Add(neighbor);
            }
            return result;
        }


        /// <summary>
        /// Converts grid coordinates to a world position for the specified orientation.
        /// </summary>
        /// <param name="x">The x-coordinate in the grid.</param>
        /// <param name="y">The y-coordinate in the grid.</param>
        /// <param name="snapToGrid">this has no effect in the hexagonal grid.</param>
        /// <returns>The calculated world position in 3D space.</returns>
        public override Vector3 GetWorldPosition(int x, int y, bool snapToGrid = false)
        {
            float size = CellSize;
            Vector3 localPosition;
            switch (hexOrientation)
            {
                case HexOrientation.PointyTop:
                    float width = size;
                    float height = sqrt3Over2 * size;
                    float offsetX = (y % 2 == 0) ? 0 : width * 0.5f;

                    float xPos = x * width + offsetX - GridSize.x * width * 0.5f;
                    float yPos = y * height - GridSize.x * height * 0.5f;

                    if (plane == GridPlane.XZ)
                        localPosition = new Vector3(xPos, 0, yPos) + new Vector3(CellSize, 0, CellSize) * 0.5f;
                    else
                        localPosition = new Vector3(xPos, yPos, 0) + new Vector3(CellSize, CellSize, 0) * 0.5f;

                    break;
                case HexOrientation.FlatTop:
                    float heightP = size;
                    float widthP = sqrt3Over2 * size;
                    float offsetY = (x % 2 == 0) ? 0 : heightP * 0.5f;

                    if (plane == GridPlane.XZ)
                        localPosition = new Vector3(x * widthP - GridSize.x * widthP * 0.5f, 0, y * heightP + offsetY - GridSize.y * heightP * 0.5f)
                        + new Vector3(CellSize, 0, CellSize) * 0.5f;
                    else
                        localPosition = new Vector3(x * widthP - GridSize.x * widthP * 0.5f, y * heightP + offsetY - GridSize.y * heightP * 0.5f, 0)
                        + new Vector3(CellSize, CellSize, 0) * 0.5f;

                    break;
                default:
                    return Vector3.zero;
            }
            return transform.TransformPoint(localPosition);
        }


        /// <summary>
        /// Converts a world position into hex grid coordinates based on the selected orientation.
        /// </summary>
        /// <param name="worldPosition">The world position to convert.</param>
        /// <param name="x">The resulting x grid index.</param>
        /// <param name="y">The resulting y grid index.</param>
        public override void GetGridPosition(Vector3 worldPosition, out int x, out int y)
        {
            Vector3 offsetCenter;

            if (plane == GridPlane.XZ)
                offsetCenter = new Vector3(CellSize, 0, CellSize) * 0.5f;
            else
                offsetCenter = new Vector3(CellSize, CellSize, 0) * 0.5f;

            Vector3 local = transform.InverseTransformPoint(worldPosition) - offsetCenter;

            float size = CellSize;

            switch (hexOrientation)
            {
                case HexOrientation.PointyTop:
                    float width = size;
                    float height = sqrt3Over2 * size;

                    float q = (local.x + GridSize.x * width * 0.5f) / width;

                    float r;
                    if (plane == GridPlane.XZ)
                        r = (local.z + GridSize.y * height * 0.5f) / height;
                    else
                        r = (local.y + GridSize.y * height * 0.5f) / height;


                    int row = Mathf.RoundToInt(r);
                    float offsetX = (row % 2 == 0) ? 0 : 0.5f;
                    int col = Mathf.RoundToInt(q - offsetX);

                    x = Mathf.Clamp(col, 0, GridSize.x - 1);
                    y = Mathf.Clamp(row, 0, GridSize.y - 1);
                    break;

                case HexOrientation.FlatTop:
                    float heightP = size;
                    float widthP = sqrt3Over2 * size;

                    float colF = (local.x + GridSize.x * widthP * 0.5f) / widthP;


                    float rowF;

                    if (plane == GridPlane.XZ)
                        rowF = (local.z + GridSize.y * heightP * 0.5f) / heightP;
                    else
                        rowF = (local.y + GridSize.y * heightP * 0.5f) / heightP;

                    int colP = Mathf.RoundToInt(colF);
                    float offsetY = (colP % 2 == 0) ? 0 : 0.5f;
                    int rowP = Mathf.RoundToInt(rowF - offsetY);

                    x = Mathf.Clamp(colP, 0, GridSize.x - 1);
                    y = Mathf.Clamp(rowP, 0, GridSize.y - 1);
                    break;

                default:
                    x = y = 0;
                    break;
            }
        }


        /// <summary>
        /// Visualizes the hex grid using Unity Gizmos, including cell centers and outlines.
        /// </summary>
        public override void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!drawGizmos) return;
            sqrt3 = Mathf.Sqrt(3f);
            sqrt3Over2 = sqrt3 / 2f;

            for (int i = 0; i < GridSize.x; i++)
            {
                for (int j = 0; j < GridSize.y; j++)
                {
                    Vector3 position = GetWorldPosition(i, j, true);
                    //Gizmos.color = Color.green;
                    //Gizmos.DrawWireSphere(position, CellSize * 0.1f);
                    Gizmos.color = Color.white;
                    DrawHexOutline(position, CellSize * 0.45f);
                    if (showGridIndex)
                        UnityEditor.Handles.Label(position, $"{i},{j}");
                }
            }
#endif
        }

        /// <summary>
        /// Draws the outline of a single hexagon for debugging in the Unity editor.
        /// </summary>
        /// <param name="center">The center of the hex cell.</param>
        /// <param name="size">The radius from the center to a corner.</param>
        protected void DrawHexOutline(Vector3 center, float size)
        {
            Vector3[] corners = new Vector3[7];

            for (int i = 0; i < 7; i++)
            {
                float angleDeg = 0;
                if (hexOrientation == HexOrientation.PointyTop)
                {
                    angleDeg = 60f * i - 30f;
                }
                else
                {
                    angleDeg = 60f * i;
                }

                float angleRad = Mathf.Deg2Rad * angleDeg;

                Vector3 offset;
                if (plane == GridPlane.XZ)
                    offset = new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
                else
                    offset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);

                Vector3 worldOffset = transform.TransformDirection(offset);
                corners[i] = center + worldOffset * size;
            }

            for (int i = 0; i < 6; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}
