using System;
using System.Collections.Generic;
using UnityEngine;


public enum HexOrientation
{
    PointyTop,
    FlatTop
}
/// <summary>
/// Serializable data for a single grid cell, stored inside LevelData.
/// </summary>
[Serializable]
public class CellData
{
    /// <summary>Grid position of the cell.</summary>
    public Vector2Int position;

    /// <summary>The ColorType asset assigned to this cell. Null = no color.</summary>
    public BubbleType category;

    /// <summary>Optional sprite assigned to this cell. If set, sprite is used instead of text.</summary>
    public Sprite sprite;

    /// <summary>Tile state for this cell (None, CROSS, WRONG, Queen).</summary>
    public string name;
    public string text;
}

/// <summary>
/// ScriptableObject that persists the full grid layout of a level.
/// Create via: right-click in Project ▶ Create ▶ GameData ▶ Level Data
/// </summary>
[CreateAssetMenu(menuName = "GameData/Level Data", fileName = "NewLevelData")]
public class LevelData : ScriptableObject
{
    [Header("Grid Dimensions")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public HexOrientation hexOrientation = HexOrientation.PointyTop;
    [Header("Cell Data")]
    public List<CellData> cells = new List<CellData>();
}
