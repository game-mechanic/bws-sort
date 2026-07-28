using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public enum EditorToolMode
{
    Pointer,
    PaintBucket,
    Move
}

/// <summary>
/// Lightweight in-memory representation of a single grid cell used
/// only inside the editor window. Serialization is handled by LevelData.
/// </summary>
public class GridCell
{
    public Vector2Int position;

    /// <summary>Reference to the ColorType ScriptableObject for this cell. Null = no color.</summary>
    public BubbleType category;

    /// <summary>Text shown on the cell (editor-only).</summary>
    public string text = "";

    /// <summary>Sprite shown on the cell (optional editor/runtime asset). If set, sprite is used instead of drawing text.</summary>
    public Sprite sprite = null;

    public AnimationClip animationClip = null;
    public bool showBothTxtAndImg = false;

    /// <summary>Text color for editor preview (NOT saved to LevelData).</summary>
    public Color textColor = Color.white;

    /// <summary>Editor-only selection state.</summary>
    public bool isSelected;

    public GridCell(Vector2Int pos)
    {
        position = pos;
        category = null;
        text = "";
        sprite = null;
        textColor = Color.white;
        isSelected = false;
    }
}

public class LevelDesignToolWindow : EditorWindow
{
    // keep old constant as default folder
    private const string COLOR_TYPE_PATH = "Assets/SCOS";
    private const string PREF_KEY_BUBBLE_FOLDER = "LevelDesignTool_LastBubbleFolder";

    // allow user-selected folder (project-relative)
    private string bubbleTypePath = COLOR_TYPE_PATH;

    // ── Grid state ─────────────────────────────────────────────────────────────

    private int gridWidth = 10;
    private int gridHeight = 10;
    private float cellSize = 50f;
    private HexOrientation hexOrientation = HexOrientation.PointyTop;
    private EditorToolMode currentToolMode = EditorToolMode.Pointer;
    private bool mirrorX = false;
    private bool mirrorY = false;

    private GridCell[,] grid;
    private List<GridCell> selectedCells = new List<GridCell>();

    // ── Move Handle state ──────────────────────────────────────────────────────
    private bool isHandleDragging = false;
    private Vector3 handleCurrentPos;
    private Vector3 handleStartPos;
    private Vector2Int? moveDragStartGrid = null;
    private GridCell[,] preDragGrid = null;

    // ── Box Select ─────────────────────────────────────────────────────────────
    private Vector2? boxSelectStartPos = null;
    private Vector2? boxSelectCurrentPos = null;
    private bool isBoxSelecting = false;

    // ── BubbleType SO data ──────────────────────────────────────────────────────

    private List<BubbleType> availableColors = new List<BubbleType>();
    private string[] colorNames = new string[0];

    // ── Inspector state ────────────────────────────────────────────────────────

    private int inspectorColorIndex = -1;
    private bool inspectorIsActive = true;
    private string inspectorIsBubble = "";
    private Color inspectorTextColor = Color.white;
    private Sprite inspectorSprite = null;
    private AnimationClip inspectorAnimationClip = null;
    private bool inspectorShowBothTxtAndImg = false;

    // ── LevelData persistence ──────────────────────────────────────────────────

    private LevelData levelDataAsset;

    // ── Scroll positions ───────────────────────────────────────────────────────

    private Vector2 leftScrollPosition;
    private Vector2 rightScrollPosition;

    // ── Icons & textures ───────────────────────────────────────────────────────

    private Texture2D bubbleIcon;
    private Texture2D darkBg;       // Solid dark background for panels
    private Texture2D headerBg;     // Accent colour for section headers
    private Texture2D separatorTex; // Thin horizontal rule

    // Palette
    private static readonly Color ColBg = new Color(0.18f, 0.18f, 0.18f);
    private static readonly Color ColPanel = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color ColHeader = new Color(0.30f, 0.30f, 0.30f);
    private static readonly Color ColAccent = new Color(0.45f, 0.45f, 0.45f);
    private static readonly Color ColSeparator = new Color(0.28f, 0.28f, 0.28f);
    private static readonly Color ColSelectionA = new Color(1f, 1f, 1, 0.7f);
    private static readonly Color ColCellEmpty = new Color(0.25f, 0.25f, 0.25f);

    // GUIStyles
    private GUIStyle styleTopBar;
    private GUIStyle styleSectionHeader;
    private GUIStyle styleSectionBody;
    private GUIStyle stylePanelBg;
    private GUIStyle styleSelectedBadge;
    private GUIStyle styleHintLabel;
    private GUIStyle styleBoldWhite;
    private GUIStyle styleToolbarBtn;
    private GUIStyle styleToolbarBtnDanger;
    private GUIStyle styleToolbarBtnAccent;
    private bool stylesInitialized;

    [MenuItem("Tools/Level Design Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelDesignToolWindow>("Level Design Tool");
        window.minSize = new Vector2(860, 520);
        window.Show();
    }

    private void OnEnable()
    {
        // restore last-used folder
        string saved = EditorPrefs.GetString(PREF_KEY_BUBBLE_FOLDER, COLOR_TYPE_PATH);
        if (!string.IsNullOrEmpty(saved)) bubbleTypePath = saved;

        LoadColorAssets();
        InitializeGrid();

        bubbleIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Gizmos/circle_soft.png");
        if (bubbleIcon == null)
            Debug.LogWarning("[LevelDesignTool] Bubble icon not found at Assets/Gizmos/circle_soft.png");
    }

    private void OnDisable()
    {
        if (darkBg) DestroyImmediate(darkBg);
        if (headerBg) DestroyImmediate(headerBg);
        if (separatorTex) DestroyImmediate(separatorTex);
        stylesInitialized = false;
    }

    private Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void EnsureStyles()
    {
        if (stylesInitialized && darkBg != null) return;

        darkBg = MakeTex(ColPanel);
        headerBg = MakeTex(ColHeader);
        separatorTex = MakeTex(ColSeparator);

        styleTopBar = new GUIStyle
        {
            normal = { background = MakeTex(new Color(0.13f, 0.13f, 0.15f)) },
            padding = new RectOffset(6, 6, 4, 4),
            fixedHeight = 26
        };

        styleSectionHeader = new GUIStyle(EditorStyles.label)
        {
            normal = { background = headerBg, textColor = Color.white },
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 2, 4, 4),
            margin = new RectOffset(0, 0, 0, 0)
        };

        styleSectionBody = new GUIStyle
        {
            normal = { background = darkBg },
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(0, 0, 0, 10)
        };

        stylePanelBg = new GUIStyle
        {
            normal = { background = MakeTex(ColBg) },
            padding = new RectOffset(0, 0, 0, 0)
        };

        styleSelectedBadge = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { background = MakeTex(new Color(0.25f, 0.25f, 0.25f)), textColor = new Color(0.85f, 0.85f, 0.85f) },
            fontStyle = FontStyle.Bold,
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = 22,
            padding = new RectOffset(8, 8, 0, 0),
            margin = new RectOffset(0, 0, 4, 8)
        };

        styleHintLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            normal = { textColor = new Color(0.6f, 0.6f, 0.65f) },
            padding = new RectOffset(4, 4, 8, 8)
        };

        styleBoldWhite = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.white }
        };

        styleToolbarBtn = new GUIStyle(EditorStyles.toolbarButton)
        {
            normal = { textColor = new Color(0.85f, 0.85f, 0.9f) },
            fontStyle = FontStyle.Normal
        };

        styleToolbarBtnDanger = new GUIStyle(styleToolbarBtn)
        {
            normal = { textColor = new Color(1f, 0.5f, 0.5f) }
        };

        styleToolbarBtnAccent = new GUIStyle(styleToolbarBtn)
        {
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }

    private void LoadColorAssets()
    {
        availableColors.Clear();

        if (string.IsNullOrEmpty(bubbleTypePath)) bubbleTypePath = COLOR_TYPE_PATH;

        if (!AssetDatabase.IsValidFolder(bubbleTypePath))
        {
            Debug.LogWarning($"[LevelDesignTool] BubbleType folder not found: {bubbleTypePath}");
            colorNames = new[] { "— folder missing —" };
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:BubbleType", new[] { bubbleTypePath });

        if (guids.Length == 0)
        {
            colorNames = new[] { "— no colors found —" };
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BubbleType asset = AssetDatabase.LoadAssetAtPath<BubbleType>(path);
            if (asset != null)
                availableColors.Add(asset);
        }

        availableColors = availableColors.OrderBy(c => c.name).ToList();

        colorNames = new string[availableColors.Count + 1];
        colorNames[0] = "None";
        for (int i = 0; i < availableColors.Count; i++)
            colorNames[i + 1] = availableColors[i].name;
    }

    private void InitializeGrid()
    {
        GridCell[,] newGrid = new GridCell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                newGrid[x, y] = (grid != null && x < grid.GetLength(0) && y < grid.GetLength(1))
                    ? grid[x, y]
                    : new GridCell(new Vector2Int(x, y));

        grid = newGrid;
        selectedCells.RemoveAll(c => c.position.x >= gridWidth || c.position.y >= gridHeight);
        UpdateInspectorFromSelection();
    }

    private void OnGUI()
    {
        EnsureStyles();

        const float TOP_BAR_H = 28f;
        const float TOOLBAR_W = 44f;
        float rightW = Mathf.Round(position.width * 0.22f);
        float centerW = position.width - rightW - TOOLBAR_W;
        float panelY = TOP_BAR_H;
        float panelH = position.height - TOP_BAR_H;

        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), ColBg);

        DrawTopBar(new Rect(0, 0, position.width, TOP_BAR_H));
        DrawToolSidebar(new Rect(0, panelY, TOOLBAR_W, panelH));
        DrawLeftPanel(new Rect(TOOLBAR_W, panelY, centerW, panelH));
        DrawRightPanel(new Rect(TOOLBAR_W + centerW, panelY, rightW, panelH));
    }

    private void DrawToolSidebar(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.17f));
        EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), ColAccent);

        GUILayout.BeginArea(new Rect(rect.x + 4, rect.y + 8, rect.width - 8, rect.height - 16));

        bool isPtr = currentToolMode == EditorToolMode.Pointer;
        bool isWand = currentToolMode == EditorToolMode.PaintBucket;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, padding = new RectOffset(0, 0, 0, 0) };

        EditorGUI.BeginChangeCheck();
        bool newPtr = GUILayout.Toggle(isPtr, new GUIContent("⬈", "Pointer Tool"), btnStyle, GUILayout.Height(36));
        if (EditorGUI.EndChangeCheck() && newPtr) currentToolMode = EditorToolMode.Pointer;

        GUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        bool newWand = GUILayout.Toggle(isWand, new GUIContent("🪣", "Paint Bucket"), btnStyle, GUILayout.Height(36));
        if (EditorGUI.EndChangeCheck() && newWand) currentToolMode = EditorToolMode.PaintBucket;

        GUILayout.Space(8);

        bool isMove = currentToolMode == EditorToolMode.Move;
        EditorGUI.BeginChangeCheck();
        bool newMove = GUILayout.Toggle(isMove, new GUIContent("✜", "Move Tool (Shift Cells)"), btnStyle, GUILayout.Height(36));
        if (EditorGUI.EndChangeCheck() && newMove) currentToolMode = EditorToolMode.Move;

        GUILayout.EndArea();
    }

    private void DrawTopBar(Rect barRect)
    {
        EditorGUI.DrawRect(barRect, new Color(0.12f, 0.12f, 0.14f));
        EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1, barRect.width, 1), ColAccent);

        GUILayout.BeginArea(new Rect(barRect.x + 6, barRect.y + 4, barRect.width - 12, 22));
        GUILayout.BeginHorizontal();

        GUILayout.Label("LEVEL DESIGN TOOL", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }, fontSize = 13 }, GUILayout.Width(160));

        GUILayout.Space(8);
        DrawVerticalSeparator();
        GUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        DrawTopBarLabel("W");
        gridWidth = EditorGUILayout.IntField(gridWidth, GUILayout.Width(34));
        GUILayout.Space(4);
        DrawTopBarLabel("H");
        gridHeight = EditorGUILayout.IntField(gridHeight, GUILayout.Width(34));
        GUILayout.Space(4);
        DrawTopBarLabel("Cell");
        cellSize = EditorGUILayout.FloatField(cellSize, GUILayout.Width(36));
        GUILayout.Space(4);
        DrawTopBarLabel("Hex");
        hexOrientation = (HexOrientation)EditorGUILayout.EnumPopup(hexOrientation, GUILayout.Width(75));

        if (EditorGUI.EndChangeCheck())
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            cellSize = Mathf.Max(10f, cellSize);
            InitializeGrid();
        }

        GUILayout.Space(8);
        DrawVerticalSeparator();
        GUILayout.Space(8);

        mirrorX = GUILayout.Toggle(mirrorX, "⇔ Mirror H", EditorStyles.toolbarButton, GUILayout.Width(70));
        mirrorY = GUILayout.Toggle(mirrorY, "⇕ Mirror V", EditorStyles.toolbarButton, GUILayout.Width(70));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📂 Folder", styleToolbarBtn, GUILayout.Width(72)))
        {
            string startPath = Application.dataPath;
            try { startPath = System.IO.Path.GetFullPath(bubbleTypePath); } catch { startPath = Application.dataPath; }

            string abs = EditorUtility.OpenFolderPanel("Select BubbleType Folder", startPath, "");
            if (!string.IsNullOrEmpty(abs))
            {
                string dataPath = Application.dataPath.Replace("\\", "/");
                string sel = abs.Replace("\\", "/");
                if (sel.StartsWith(dataPath))
                {
                    string rel = "Assets" + sel.Substring(dataPath.Length);
                    bubbleTypePath = rel;
                    EditorPrefs.SetString(PREF_KEY_BUBBLE_FOLDER, bubbleTypePath);
                    LoadColorAssets();
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside this Unity project (inside the Assets folder).", "OK");
                }
            }
        }

        if (GUILayout.Button("↺ Refresh", styleToolbarBtn, GUILayout.Width(62)))
            LoadColorAssets();

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("⊘ Clear", styleToolbarBtnDanger, GUILayout.Width(58)))
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Clear all cell data?", "Yes", "Cancel"))
                {
                    grid = null;
                    selectedCells.Clear();
                    InitializeGrid();
                    Repaint();
                }
            };
        }

        GUILayout.Space(8);
        DrawVerticalSeparator();
        GUILayout.Space(8);

        DrawTopBarLabel("Level Data");
        EditorGUI.BeginChangeCheck();
        levelDataAsset = (LevelData)EditorGUILayout.ObjectField(levelDataAsset, typeof(LevelData), false, GUILayout.Width(140));
        if (EditorGUI.EndChangeCheck() && levelDataAsset != null)
            LoadFromLevelData();

        GUILayout.Space(6);
        if (levelDataAsset == null)
        {
            if (GUILayout.Button("✚ New & Save", styleToolbarBtnAccent, GUILayout.Width(90)))
                EditorApplication.delayCall += CreateNewAndSaveLevelData;
        }
        else
        {
            if (GUILayout.Button("New", styleToolbarBtnAccent, GUILayout.Width(36)))
                EditorApplication.delayCall += CreateNewAndSaveLevelData;
            if (GUILayout.Button("💾 Save", styleToolbarBtn, GUILayout.Width(52)))
                SaveToLevelData();
            if (GUILayout.Button("⬆ Load", styleToolbarBtn, GUILayout.Width(52)))
                LoadFromLevelData();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawTopBarLabel(string text)
    {
        GUILayout.Label(text, new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.70f, 0.70f, 0.75f) },
            padding = new RectOffset(0, 3, 0, 0)
        });
    }

    private void DrawVerticalSeparator()
    {
        Rect r = GUILayoutUtility.GetRect(1, 18, GUILayout.Width(1));
        EditorGUI.DrawRect(r, ColSeparator);
    }

    private void DrawLeftPanel(Rect panelRect)
    {
        EditorGUI.DrawRect(panelRect, ColBg);

        GUILayout.BeginArea(panelRect);
        leftScrollPosition = GUILayout.BeginScrollView(leftScrollPosition, alwaysShowHorizontal: true, alwaysShowVertical: true);

        float sqrt3Over2 = Mathf.Sqrt(3f) / 2f;
        float totalGridW, totalGridH;
        if (hexOrientation == HexOrientation.PointyTop) {
            totalGridW = gridWidth * cellSize + cellSize * 0.5f;
            totalGridH = gridHeight * (sqrt3Over2 * cellSize) + cellSize * 0.5f;
        } else {
            totalGridW = gridWidth * (sqrt3Over2 * cellSize) + cellSize * 0.5f;
            totalGridH = gridHeight * cellSize + cellSize * 0.5f;
        }

        float margin = Mathf.Max(panelRect.width, panelRect.height);
        float availW = totalGridW + margin;
        float availH = totalGridH + margin;

        Rect fullRect = GUILayoutUtility.GetRect(availW, availH);

        float offsetX = fullRect.x + (availW - totalGridW) * 0.5f;
        float offsetY = fullRect.y + (availH - totalGridH) * 0.5f;

        Rect gridRect = new Rect(offsetX, offsetY, totalGridW, totalGridH);

        DrawGrid(gridRect);
        HandleGridInput(gridRect, panelRect);

        GUILayout.EndScrollView();

        Rect hintRect = new Rect(6, panelRect.height - 18, 200, 16);
        EditorGUI.LabelField(hintRect, "Scroll to zoom  |  Drag to paint-select", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.45f, 0.45f, 0.50f) } });

        GUILayout.EndArea();
    }

    private void DrawGrid(Rect gridRect)
    {
        if (Event.current.type != EventType.Repaint) return;

        Vector2 mousePos = Event.current.mousePosition;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = grid[x, y];
                Vector2 center = GetCellCenter(gridRect, x, y);
                Vector2[] corners = GetHexCorners(center, cellSize, hexOrientation);
                Vector3[] corners3D = new Vector3[6];
                for (int i = 0; i < 6; i++) corners3D[i] = corners[i];

                bool checker = (x + y) % 2 == 0;
                Color emptyColor = checker ? new Color(0.22f, 0.22f, 0.25f) : new Color(0.24f, 0.24f, 0.27f);
                Color cellColor = emptyColor;
                Color bubbleColor = Color.clear;

                if (cell.category != null)
                {
                    Color resolved = cell.category.Color;
                    if (!string.IsNullOrEmpty(cell.text))
                        bubbleColor = resolved;
                    else
                        cellColor = resolved;
                }

                float inset = Mathf.Max(1f, cellSize * 0.025f);
                Vector3[] fillCorners = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    Vector3 dir = (corners3D[i] - (Vector3)center).normalized;
                    fillCorners[i] = corners3D[i] - dir * inset;
                }

                Handles.color = cellColor;
                Handles.DrawAAConvexPolygon(fillCorners);

                // If sprite present, draw sprite only. Otherwise draw bubble/text.
                if (cell.sprite != null)
                {
                    Handles.BeginGUI();
                    Rect iconRect = new Rect(center.x - cellSize * 0.45f, center.y - cellSize * 0.45f, cellSize * 0.9f, cellSize * 0.9f);
                    Texture2D tex = cell.sprite.texture;
                    if (tex != null)
                    {
                        Rect spriteRect = cell.sprite.rect;
                        Rect uv = new Rect(spriteRect.x / tex.width, spriteRect.y / tex.height, spriteRect.width / tex.width, spriteRect.height / tex.height);
                        GUI.DrawTextureWithTexCoords(iconRect, tex, uv, true);
                    }
                    Handles.EndGUI();
                }
                else
                {
                    if (bubbleColor != Color.clear)
                    {
                        Handles.color = bubbleColor;
                        Handles.DrawSolidDisc((Vector3)center, Vector3.forward, cellSize * 0.4f);                      
                    }

                    // Draw text always when present; slightly lighter by default, brighter on hover
                    if (!string.IsNullOrEmpty(cell.text))
                    {
                        bool hovered = Vector2.Distance(mousePos, center) <= cellSize * 0.45f;
                        Color baseColor = cell.textColor;
                        float defaultAlpha = 0.85f;
                        float hoverAlpha = 1.0f;
                        float alpha = hovered ? hoverAlpha : defaultAlpha;
                        float brightness = hovered ? 1.06f : 0.92f;
                        Color drawColor = new Color(
                            Mathf.Clamp01(baseColor.r * brightness),
                            Mathf.Clamp01(baseColor.g * brightness),
                            Mathf.Clamp01(baseColor.b * brightness),
                            alpha);

                        Handles.BeginGUI();
                        var prevColor = GUI.color;
                        GUI.color = drawColor;

                        GUIStyle ts = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = drawColor }
                        };
                        ts.fontSize = Mathf.Clamp((int)(cellSize * 0.35f), 8, 40);

                        Rect labelRect = new Rect(center.x - cellSize * 0.45f, center.y - cellSize * 0.45f, cellSize * 0.9f, cellSize * 0.9f);
                        GUI.Label(labelRect, cell.text, ts);

                        GUI.color = prevColor;
                        Handles.EndGUI();
                    }
                }

                if (cell.isSelected)
                {
                    Handles.color = ColSelectionA;
                    Handles.DrawAAPolyLine(4f, corners3D[0], corners3D[1], corners3D[2], corners3D[3], corners3D[4], corners3D[5], corners3D[0]);
                    Handles.color = new Color(ColSelectionA.r, ColSelectionA.g, ColSelectionA.b, 0.5f);
                    Handles.DrawAAPolyLine(2f, fillCorners[0], fillCorners[1], fillCorners[2], fillCorners[3], fillCorners[4], fillCorners[5], fillCorners[0]);
                }
            }
        }

        if (selectedCells.Count > 0 && currentToolMode == EditorToolMode.Move)
        {
            Vector2 avgCenter = GetSelectionCenter(gridRect);
            Handles.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            Handles.DrawSolidDisc((Vector3)avgCenter, Vector3.forward, 12f);
            Handles.color = Color.black;
            Handles.DrawWireDisc((Vector3)avgCenter, Vector3.forward, 12f);
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(3f, new Vector3(avgCenter.x - 6, avgCenter.y), new Vector3(avgCenter.x + 6, avgCenter.y));
            Handles.DrawAAPolyLine(3f, new Vector3(avgCenter.x, avgCenter.y - 6), new Vector3(avgCenter.x, avgCenter.y + 6));
        }

        if (isBoxSelecting && boxSelectStartPos.HasValue && boxSelectCurrentPos.HasValue)
        {
            Rect selRect = GetScreenRect(boxSelectStartPos.Value, boxSelectCurrentPos.Value);
            Handles.color = new Color(0.2f, 0.6f, 1f, 1f);
            Handles.DrawAAPolyLine(2f, new Vector3(selRect.xMin, selRect.yMin), new Vector3(selRect.xMax, selRect.yMin), new Vector3(selRect.xMax, selRect.yMax), new Vector3(selRect.xMin, selRect.yMax), new Vector3(selRect.xMin, selRect.yMin));
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.15f);
            Handles.DrawAAConvexPolygon(new Vector3(selRect.xMin, selRect.yMin), new Vector3(selRect.xMax, selRect.yMin), new Vector3(selRect.xMax, selRect.yMax), new Vector3(selRect.xMin, selRect.yMax));
        }
    }

    private Vector2 GetSelectionCenter(Rect gridRect)
    {
        Vector2 sum = Vector2.zero;
        foreach (var c in selectedCells)
            sum += GetCellCenter(gridRect, c.position.x, c.position.y);
        return sum / selectedCells.Count;
    }

    private Rect GetScreenRect(Vector2 p1, Vector2 p2)
    {
        return new Rect(
            Mathf.Min(p1.x, p2.x),
            Mathf.Min(p1.y, p2.y),
            Mathf.Abs(p1.x - p2.x),
            Mathf.Abs(p1.y - p2.y)
        );
    }

    private Vector2 GetCellCenter(Rect gridRect, int x, int y)
    {
        float size = cellSize;
        float sqrt3Over2 = Mathf.Sqrt(3f) / 2f;

        switch (hexOrientation)
        {
            case HexOrientation.PointyTop:
                float width = size;
                float height = sqrt3Over2 * size;
                float offsetX = (y % 2 == 0) ? 0 : width * 0.5f;
                return new Vector2(gridRect.x + x * width + offsetX + size * 0.5f, gridRect.y + y * height + size * 0.5f);
            case HexOrientation.FlatTop:
                float heightP = size;
                float widthP = sqrt3Over2 * size;
                float offsetY = (x % 2 == 0) ? 0 : heightP * 0.5f;
                return new Vector2(gridRect.x + x * widthP + size * 0.5f, gridRect.y + y * heightP + offsetY + size * 0.5f);
        }
        return Vector2.zero;
    }

    private Vector2[] GetHexCorners(Vector2 center, float size, HexOrientation orientation)
    {
        Vector2[] corners = new Vector2[6];
        bool isPointy = orientation == HexOrientation.PointyTop;
        float r = size / Mathf.Sqrt(3f);
        float angleOffset = isPointy ? 30f : 0f;

        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i + angleOffset;
            float angleRad = Mathf.PI / 180f * angleDeg;
            corners[i] = new Vector2(center.x + r * Mathf.Cos(angleRad), center.y + r * Mathf.Sin(angleRad));
        }
        return corners;
    }

    private void GetGridPosition(Vector2 localPos, out int x, out int y)
    {
        float size = cellSize;
        float sqrt3Over2 = Mathf.Sqrt(3f) / 2f;

        Vector2 local = localPos - new Vector2(size * 0.5f, size * 0.5f);

        switch (hexOrientation)
        {
            case HexOrientation.PointyTop:
                float width = size;
                float height = sqrt3Over2 * size;

                float q = local.x / width;
                float r = local.y / height;

                int row = Mathf.RoundToInt(r);
                float offsetX = (row % 2 == 0) ? 0 : 0.5f;
                int col = Mathf.RoundToInt(q - offsetX);

                x = col;
                y = row;
                break;

            case HexOrientation.FlatTop:
                float heightP = size;
                float widthP = sqrt3Over2 * size;

                float colF = local.x / widthP;
                float rowF = local.y / heightP;

                int colP = Mathf.RoundToInt(colF);
                float offsetY = (colP % 2 == 0) ? 0 : 0.5f;
                int rowP = Mathf.RoundToInt(rowF - offsetY);

                x = colP;
                y = rowP;
                break;

            default:
                x = 0; y = 0;
                break;
        }
    }


    private void HandleGridInput(Rect gridRect, Rect panelRect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && currentToolMode == EditorToolMode.Move)
        {
            if (selectedCells.Count > 0)
            {
                Vector2 avgCenter = GetSelectionCenter(gridRect);
                if (Vector2.Distance(e.mousePosition, avgCenter) < 18f)
                {
                    GetGridPosition(e.mousePosition - new Vector2(gridRect.x, gridRect.y), out int cx, out int cy);
                    moveDragStartGrid = new Vector2Int(cx, cy);

                    preDragGrid = new GridCell[gridWidth, gridHeight];
                    for (int x = 0; x < gridWidth; x++)
                    {
                        for (int y = 0; y < gridHeight; y++)
                        {
                            GridCell oc = grid[x, y];
                            preDragGrid[x, y] = new GridCell(new Vector2Int(x, y))
                            {
                                category = oc.category,
                                text = oc.text,
                                sprite = oc.sprite,
                                isSelected = oc.isSelected
                            };
                        }
                    }

                    e.Use();
                    return;
                }
            }
            e.Use();
            return;
        }

        if (e.type == EventType.MouseDrag && e.button == 0 && currentToolMode == EditorToolMode.Move)
        {
            if (moveDragStartGrid.HasValue)
            {
                GetGridPosition(e.mousePosition - new Vector2(gridRect.x, gridRect.y), out int currentX, out int currentY);
                int dx = currentX - moveDragStartGrid.Value.x;
                int dy = currentY - moveDragStartGrid.Value.y;

                ApplyGridShift(preDragGrid, dx, dy);
            }
            e.Use();
            Repaint();
            return;
        }

        if (e.rawType == EventType.MouseUp && e.button == 0 && moveDragStartGrid.HasValue)
        {
            moveDragStartGrid = null;
            preDragGrid = null;
            e.Use();
            return;
        }

        // Middle-mouse drag to pan
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            leftScrollPosition -= e.delta;
            e.Use();
            Repaint();
            return;
        }

        // Middle-mouse click (consume to prevent other behaviors)
        if (e.type == EventType.MouseDown && e.button == 2)
        {
            e.Use();
            return;
        }

        // Scroll-wheel zoom
        if (e.type == EventType.ScrollWheel)
        {
            float zoomDelta = -e.delta.y;
            float oldCellSize = cellSize;
            cellSize = Mathf.Clamp(cellSize + zoomDelta * cellSize * 0.05f, 10f, 150f);

            if (oldCellSize != cellSize)
            {
                // Zoom towards the mouse cursor
                float scale = cellSize / oldCellSize;

                // Mouse position local to the grid's origin
                Vector2 mouseLocalToGrid = e.mousePosition - new Vector2(gridRect.x, gridRect.y);
                Vector2 newMouseLocalToGrid = mouseLocalToGrid * scale;

                // Calculate the grid's new size after scaling
                float sqrt3Over2 = Mathf.Sqrt(3f) / 2f;
                float newTotalW, newTotalH;
                if (hexOrientation == HexOrientation.PointyTop)
                {
                    newTotalW = gridWidth * cellSize + cellSize * 0.5f;
                    newTotalH = gridHeight * (sqrt3Over2 * cellSize) + cellSize * 0.5f;
                }
                else
                {
                    newTotalW = gridWidth * (sqrt3Over2 * cellSize) + cellSize * 0.5f;
                    newTotalH = gridHeight * cellSize + cellSize * 0.5f;
                }

                float margin = Mathf.Max(panelRect.width, panelRect.height);
                float newAvailW = newTotalW + margin;
                float newAvailH = newTotalH + margin;

                float newOffsetX = (newAvailW - newTotalW) * 0.5f;
                float newOffsetY = (newAvailH - newTotalH) * 0.5f;

                Vector2 newContentPos = new Vector2(newOffsetX, newOffsetY) + newMouseLocalToGrid;

                leftScrollPosition += (newContentPos - e.mousePosition);
            }
            e.Use();
            Repaint();
            return;
        }

        bool isClick = e.type == EventType.MouseDown && e.button == 0;
        Vector2 local = e.mousePosition - new Vector2(gridRect.x, gridRect.y);
        GetGridPosition(local, out int cellX, out int cellY);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (currentToolMode == EditorToolMode.PaintBucket)
            {
                if (cellX >= 0 && cellX < gridWidth && cellY >= 0 && cellY < gridHeight)
                {
                    FloodedSelectContiguous(cellX, cellY, e.control || e.command);
                    int mx = GetMirroredX(cellX, cellY);
                    int my = GetMirroredY(cellX, cellY);
                    if (mirrorX && mx >= 0 && mx < gridWidth) FloodedSelectContiguous(mx, cellY, true);
                    if (mirrorY && my >= 0 && my < gridHeight) FloodedSelectContiguous(cellX, my, true);
                    if (mirrorX && mirrorY && mx >= 0 && mx < gridWidth && my >= 0 && my < gridHeight) FloodedSelectContiguous(mx, my, true);
                    e.Use();
                    GUI.changed = true;
                    UpdateInspectorFromSelection();
                }
                return;
            }
            else if (currentToolMode == EditorToolMode.Pointer)
            {
                if (e.shift)
                {
                    boxSelectStartPos = e.mousePosition;
                    boxSelectCurrentPos = e.mousePosition;
                    isBoxSelecting = false;
                }
                else
                {
                    if (cellX >= 0 && cellX < gridWidth && cellY >= 0 && cellY < gridHeight)
                    {
                        DoPointerSelection(cellX, cellY, isToggle: e.control || e.command, isClear: !(e.control || e.command));
                        GUI.changed = true;
                        UpdateInspectorFromSelection();
                    }
                }
                e.Use();
                return;
            }
        }

        if (e.type == EventType.MouseDrag && e.button == 0 && currentToolMode == EditorToolMode.Pointer)
        {
            if (e.shift && boxSelectStartPos.HasValue)
            {
                boxSelectCurrentPos = e.mousePosition;
                if (Vector2.Distance(boxSelectStartPos.Value, boxSelectCurrentPos.Value) > 5f)
                {
                    isBoxSelecting = true;
                }
                e.Use();
                Repaint();
                return;
            }
            else if (!e.shift)
            {
                if (boxSelectStartPos.HasValue) {
                    boxSelectStartPos = null;
                    isBoxSelecting = false;
                }

                if (cellX >= 0 && cellX < gridWidth && cellY >= 0 && cellY < gridHeight)
                {
                    GridCell clicked = grid[cellX, cellY];
                    if (!clicked.isSelected)
                    {
                        DoPointerSelection(cellX, cellY, isToggle: false, isClear: false);
                        GUI.changed = true;
                        UpdateInspectorFromSelection();
                    }
                }
                e.Use();
                Repaint();
                return;
            }
        }

        if (e.rawType == EventType.MouseUp && e.button == 0 && boxSelectStartPos.HasValue)
        {
            if (isBoxSelecting)
            {
                Rect selRect = GetScreenRect(boxSelectStartPos.Value, boxSelectCurrentPos.Value);
                if (!(e.control || e.command)) ClearSelection();

                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        Vector2 center = GetCellCenter(gridRect, x, y);
                        if (selRect.Contains(center))
                        {
                            DoPointerSelection(x, y, isToggle: false, isClear: false);
                        }
                    }
                }
            }
            else
            {
                if (cellX >= 0 && cellX < gridWidth && cellY >= 0 && cellY < gridHeight)
                {
                    DoPointerSelection(cellX, cellY, isToggle: e.control || e.command, isClear: !(e.control || e.command));
                }
            }

            boxSelectStartPos = null;
            isBoxSelecting = false;

            UpdateInspectorFromSelection();
            GUI.changed = true;
            e.Use();
            Repaint();
            return;
        }
    }
    private int GetMirroredX(int x, int y)
    {
        if (hexOrientation == HexOrientation.PointyTop && y % 2 != 0)
            return gridWidth - 2 - x;
        return gridWidth - 1 - x;
    }

    private int GetMirroredY(int x, int y)
    {
        if (hexOrientation == HexOrientation.FlatTop && x % 2 != 0)
            return gridHeight - 2 - y;
        return gridHeight - 1 - y;
    }
    private void DoPointerSelection(int cellX, int cellY, bool isToggle, bool isClear)
    {
        if (isClear) ClearSelection();

        bool newState = true;
        if (isToggle) {
            GridCell c = grid[cellX, cellY];
            newState = !c.isSelected;
        }

        SetCellSelected(cellX, cellY, newState);
        if (mirrorX) SetCellSelected(gridWidth - 1 - cellX, cellY, newState);
        if (mirrorY) SetCellSelected(cellX, gridHeight - 1 - cellY, newState);
        if (mirrorX && mirrorY) SetCellSelected(gridWidth - 1 - cellX, gridHeight - 1 - cellY, newState);
    }

    private void SetCellSelected(int cellX, int cellY, bool select)
    {
        if (cellX < 0 || cellX >= gridWidth || cellY < 0 || cellY >= gridHeight) return;
        GridCell c = grid[cellX, cellY];
        if (select && !c.isSelected)
        {
            c.isSelected = true;
            selectedCells.Add(c);
        }
        else if (!select && c.isSelected)
        {
            c.isSelected = false;
            selectedCells.Remove(c);
        }
    }

    private List<Vector2Int> GetHexNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (hexOrientation == HexOrientation.PointyTop)
        {
            neighbors.Add(new Vector2Int(x - 1, y));
            neighbors.Add(new Vector2Int(x + 1, y));
            if (y % 2 == 0)
            {
                neighbors.Add(new Vector2Int(x - 1, y - 1));
                neighbors.Add(new Vector2Int(x, y - 1));
                neighbors.Add(new Vector2Int(x - 1, y + 1));
                neighbors.Add(new Vector2Int(x, y + 1));
            }
            else
            {
                neighbors.Add(new Vector2Int(x, y - 1));
                neighbors.Add(new Vector2Int(x + 1, y - 1));
                neighbors.Add(new Vector2Int(x, y + 1));
                neighbors.Add(new Vector2Int(x + 1, y + 1));
            }
        }
        else // FlatTop
        {
            neighbors.Add(new Vector2Int(x, y - 1));
            neighbors.Add(new Vector2Int(x, y + 1));
            if (x % 2 == 0)
            {
                neighbors.Add(new Vector2Int(x - 1, y - 1));
                neighbors.Add(new Vector2Int(x - 1, y));
                neighbors.Add(new Vector2Int(x + 1, y - 1));
                neighbors.Add(new Vector2Int(x + 1, y));
            }
            else
            {
                neighbors.Add(new Vector2Int(x - 1, y));
                neighbors.Add(new Vector2Int(x - 1, y + 1));
                neighbors.Add(new Vector2Int(x + 1, y));
                neighbors.Add(new Vector2Int(x + 1, y + 1));
            }
        }
        return neighbors.Where(p => p.x >= 0 && p.x < gridWidth && p.y >= 0 && p.y < gridHeight).ToList();
    }

    private void FloodedSelectContiguous(int startX, int startY, bool addToSelection)
    {
        GridCell startCell = grid[startX, startY];
        BubbleType targetColor = startCell.category;

        if (!addToSelection)
            ClearSelection();

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<GridCell> connectedCells = new List<GridCell>();

        Vector2Int startPos = new Vector2Int(startX, startY);
        queue.Enqueue(startPos);
        visited.Add(startPos);

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            GridCell cell = grid[curr.x, curr.y];

            connectedCells.Add(cell);

            foreach (Vector2Int neighbor in GetHexNeighbors(curr.x, curr.y))
            {
                if (!visited.Contains(neighbor))
                {
                    GridCell neighborCell = grid[neighbor.x, neighbor.y];
                    if (neighborCell.category == targetColor)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        foreach (GridCell cell in connectedCells)
        {
            if (!cell.isSelected)
            {
                cell.isSelected = true;
                selectedCells.Add(cell);
            }
        }
    }

    private void ApplyGridShift(GridCell[,] backup, int dx, int dy)
    {
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                grid[x, y].category = null;
                grid[x, y].text = "";
                grid[x, y].sprite = null;
                grid[x, y].isSelected = false;
            }
        }
        
        selectedCells.Clear();

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                if (!backup[x,y].isSelected) {
                    grid[x, y].category = backup[x, y].category;
                    grid[x, y].text = backup[x, y].text;
                    grid[x, y].sprite = backup[x, y].sprite;
                }
            }
        }
        
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                if (backup[x,y].isSelected) {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight) {
                        grid[nx, ny].category = backup[x, y].category;
                        grid[nx, ny].text = backup[x, y].text;
                        grid[nx, ny].sprite = backup[x, y].sprite;
                        grid[nx, ny].isSelected = true;
                        selectedCells.Add(grid[nx, ny]);
                    }
                }
            }
        }
    }

    private void ClearSelection()
    {
        foreach (var cell in selectedCells) cell.isSelected = false;
        selectedCells.Clear();
    }

    private void DrawRightPanel(Rect panelRect)
    {
        EditorGUI.DrawRect(panelRect, new Color(0.17f, 0.17f, 0.20f));
        EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 2, panelRect.height), ColAccent);

        GUILayout.BeginArea(new Rect(panelRect.x + 8, panelRect.y + 8, panelRect.width - 16, panelRect.height - 16));
        rightScrollPosition = GUILayout.BeginScrollView(rightScrollPosition, GUIStyle.none, GUIStyle.none);

        GUILayout.Label("INSPECTOR", new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.6f, 0.7f) }, padding = new RectOffset(0,0,0,4) });
        GUILayout.Space(4);

        if (selectedCells.Count > 0)
        {
            GUILayout.Label($"{selectedCells.Count} CELL{(selectedCells.Count == 1 ? "" : "S")} SELECTED", styleSelectedBadge);

            DrawSectionHeader("COLOR");
            GUILayout.BeginVertical(styleSectionBody);

            EditorGUI.BeginChangeCheck();
            int rawIndex = Mathf.Clamp(inspectorColorIndex + 1, 0, colorNames.Length - 1);
            rawIndex = EditorGUILayout.Popup(rawIndex, colorNames);
            inspectorColorIndex = rawIndex - 1;

            if (inspectorColorIndex >= 0 && inspectorColorIndex < availableColors.Count)
            {
                BubbleType preview = availableColors[inspectorColorIndex];
                if (preview != null)
                {
                    GUILayout.Space(3);
                    Rect swatchRow = GUILayoutUtility.GetRect(1, 24);
                    float swatchSize = 20f;
                    EditorGUI.DrawRect(new Rect(swatchRow.x, swatchRow.y + 2, swatchSize, swatchSize), preview.Color);
                    EditorGUI.DrawRect(new Rect(swatchRow.x, swatchRow.y + 2, swatchSize, swatchSize), new Color(0,0,0,0.3f));
                    EditorGUI.LabelField(new Rect(swatchRow.x + swatchSize + 6, swatchRow.y + 4, swatchRow.width - swatchSize - 6, 18), preview.name, new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f,0.8f,0.85f) } });
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                BubbleType nc = (inspectorColorIndex >= 0 && inspectorColorIndex < availableColors.Count) ? availableColors[inspectorColorIndex] : null;
                foreach (var cell in selectedCells) cell.category = nc;
                Repaint();
            }

            GUILayout.EndVertical();

            DrawSectionHeader("CELL OPTIONS");
            GUILayout.BeginVertical(styleSectionBody);

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Text", "Draws as a bubble with selected color on top."), EditorStyles.miniLabel, GUILayout.Width(90));
            inspectorIsBubble = EditorGUILayout.TextField(inspectorIsBubble);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Text Color", "Color used to render cell text."), EditorStyles.miniLabel, GUILayout.Width(90));
            inspectorTextColor = EditorGUILayout.ColorField(inspectorTextColor);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Sprite", "Optional sprite to use instead of text."), EditorStyles.miniLabel, GUILayout.Width(90));
            inspectorSprite = (Sprite)EditorGUILayout.ObjectField(inspectorSprite, typeof(Sprite), false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Anim Clip", "Optional animation clip to play."), EditorStyles.miniLabel, GUILayout.Width(90));
            inspectorAnimationClip = (AnimationClip)EditorGUILayout.ObjectField(inspectorAnimationClip, typeof(AnimationClip), false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Show Both", "Show both text and image."), EditorStyles.miniLabel, GUILayout.Width(90));
            inspectorShowBothTxtAndImg = EditorGUILayout.Toggle(inspectorShowBothTxtAndImg);
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var cell in selectedCells) { 
                    cell.text = inspectorIsBubble; 
                    cell.textColor = inspectorTextColor; 
                    cell.sprite = inspectorSprite; 
                    cell.animationClip = inspectorAnimationClip;
                    cell.showBothTxtAndImg = inspectorShowBothTxtAndImg;
                    if (inspectorSprite != null && !inspectorShowBothTxtAndImg) cell.text = ""; 
                }
                Repaint();
            }

            GUILayout.EndVertical();

            GUILayout.Space(12);

            Rect btnRect = GUILayoutUtility.GetRect(1, 28, GUILayout.ExpandWidth(true));
            btnRect = new Rect(btnRect.x + 6, btnRect.y, btnRect.width - 12, btnRect.height);
            if (GUI.Button(btnRect, "Deselect All", new GUIStyle(EditorStyles.miniButton) { normal = { textColor = new Color(0.8f,0.8f,0.85f) }, alignment = TextAnchor.MiddleCenter }))
            {
                ClearSelection();
                Repaint();
            }
        }
        else
        {
            GUILayout.Space(20);
            GUILayout.Label("No cell selected.\n\nClick → select\nCtrl+Click → multi\nDrag → paint-select", styleHintLabel);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawSectionHeader(string title)
    {
        GUILayout.Label(title, styleSectionHeader);
    }

    private void UpdateInspectorFromSelection()
    {
        if (selectedCells.Count == 0) return;
        GridCell first = selectedCells[0];
        inspectorColorIndex = availableColors.IndexOf(first.category);
        inspectorIsBubble = first.text;
        inspectorTextColor = first.textColor;
        inspectorSprite = first.sprite;
        inspectorAnimationClip = first.animationClip;
        inspectorShowBothTxtAndImg = first.showBothTxtAndImg;
    }

    private void CreateNewAndSaveLevelData()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create New Level Data", "NewLevelData", "asset", "Choose where to save the new Level Data asset.", "Assets");
        if (string.IsNullOrEmpty(path)) return;
        LevelData newAsset = CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        levelDataAsset = newAsset;
        SaveToLevelData();
        Debug.Log($"[LevelDesignTool] Created and saved new LevelData at: {path}");
    }

    private void SaveToLevelData()
    {
        if (levelDataAsset == null) return;

        levelDataAsset.gridWidth = gridWidth;
        levelDataAsset.gridHeight = gridHeight;
        levelDataAsset.hexOrientation = hexOrientation;
        levelDataAsset.cells.Clear();

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = grid[x, y];
                levelDataAsset.cells.Add(new CellData
                {
                    position = cell.position,
                    category = cell.category,
                    text = cell.text,
                    sprite = cell.sprite,
                    animationClip = cell.animationClip,
                    showBothTxtAndImg = cell.showBothTxtAndImg
                });
            }

        EditorUtility.SetDirty(levelDataAsset);
        AssetDatabase.SaveAssets();
    }

    private void LoadFromLevelData()
    {
        if (levelDataAsset == null) return;

        gridWidth = levelDataAsset.gridWidth;
        gridHeight = levelDataAsset.gridHeight;
        hexOrientation = levelDataAsset.hexOrientation;
        grid = null;
        selectedCells.Clear();
        InitializeGrid();

        foreach (CellData data in levelDataAsset.cells)
        {
            int x = data.position.x;
            int y = data.position.y;
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;

            grid[x, y].category = data.category;
            grid[x, y].text = data.text;
            grid[x, y].sprite = data.sprite;
            grid[x, y].animationClip = data.animationClip;
            grid[x, y].showBothTxtAndImg = data.showBothTxtAndImg;
            // textColor intentionally editor-only
        }

        UpdateInspectorFromSelection();
        Repaint();
    }
}
