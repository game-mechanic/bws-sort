using System.Collections;
using UnityEngine;

/// <summary>
/// Procedural 2D granular sand simulation.
///
/// Each sand cell stores its own color.
/// This means spawning new colored sand will NEVER change
/// the color of already-existing sand.
///
/// LEFT MOUSE = Spawn sand at mouse position.
/// SPACE = Spawn sand at screen center.
/// </summary>
public class ProceduralSandBall : MonoBehaviour
{
    // ============================================================
    // WORLD
    // ============================================================

    [Header("WORLD")]
    [SerializeField] private float worldWidth = 8f;
    [SerializeField] private float worldHeight = 10f;

    [SerializeField] private float groundY = -3.5f;
    [SerializeField] private float groundThickness = 0.25f;


    // ============================================================
    // SAND
    // ============================================================

    [Header("SAND")]
    [SerializeField] private int gridWidth = 180;
    [SerializeField] private int gridHeight = 220;

    // Default color used when spawning from mouse/Space.
    [SerializeField]
    private Color sandColor =
        new Color(0.78f, 0.57f, 0.28f, 1f);

    [SerializeField]
    private Color sandVariation =
        new Color(0.12f, 0.08f, 0.04f, 1f);

    [Range(0f, 1f)]
    [SerializeField]
    private float sandFill = 0.90f;

    [Range(0f, 1f)]
    [SerializeField]
    private float sandDensity = 0.97f;

    [SerializeField, Range(0, 2)]
    private float sandRenderThickness = 1;


    // ============================================================
    // SPAWN
    // ============================================================

    [Header("MOUSE SAND SPAWN")]
    [SerializeField]
    private float spawnRadius = 0.5f;

    [SerializeField]
    private bool spawnOnLeftClick = true;


    // ============================================================
    // SAND PHYSICS
    // ============================================================

    [Header("SAND SIMULATION")]
    [SerializeField]
    private float simulationStepsPerFrame = 8;

    [SerializeField]
    private float pileSpread = 0.75f;

    [SerializeField]
    private float diagonalChance = 0.85f;

    [SerializeField]
    private float sidewaysChance = 0.65f;


    // ============================================================
    // INTERNAL GRID
    // ============================================================

    private bool[,] sand;
    private bool[,] nextSand;

    // IMPORTANT:
    // Every sand cell has its OWN color.
    private Color[] sandColors;
    private Color[] nextSandColors;

    private Color[] texturePixels;

    private Texture2D sandTexture;
    private SpriteRenderer sandRenderer;

    private float cellWidth;
    private float cellHeight;


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        CalculateGrid();

        CreateWorld();

        CreateSandTexture();

        // No sand is created at start.
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        for (int i = 0; i < simulationStepsPerFrame; i++)
        {
            SimulateSand();
        }

        UpdateSandTexture();
    }


    // ============================================================
    // MOUSE SPAWN
    // ============================================================

    private void SpawnSandAtMouse()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 mouseScreenPosition = Input.mousePosition;

        mouseScreenPosition.z =
            Mathf.Abs(cam.transform.position.z);

        Vector3 mouseWorldPosition =
            cam.ScreenToWorldPoint(mouseScreenPosition);

        mouseWorldPosition.z = 0f;

        // Use the current default sand color.
        SpawnSandAtWorldPosition(
            mouseWorldPosition,
            sandColor
        );
    }


    // ============================================================
    // SPAWN SAND
    // ============================================================

    public void SpawnSandAtWorldPosition(
        Vector3 worldPosition,
        Color spawnColor)
    {
        Vector2 gridPosition =
            WorldToGrid(worldPosition);

        int centerX =
            Mathf.RoundToInt(gridPosition.x);

        int centerY =
            Mathf.RoundToInt(gridPosition.y);

        int radiusX =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    spawnRadius / cellWidth
                )
            );

        int radiusY =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    spawnRadius / cellHeight
                )
            );


        // ========================================================
        // CREATE CIRCULAR SAND
        // ========================================================

        for (int y = -radiusY;
             y <= radiusY;
             y++)
        {
            for (int x = -radiusX;
                 x <= radiusX;
                 x++)
            {
                float nx =
                    x / (float)radiusX;

                float ny =
                    y / (float)radiusY;

                float distance =
                    nx * nx +
                    ny * ny;

                if (distance > sandFill)
                    continue;

                int gx =
                    centerX + x;

                int gy =
                    centerY + y;

                if (!InsideGrid(gx, gy))
                    continue;

                // Don't spawn inside ground.
                if (IsGroundCell(gy))
                    continue;


                // ====================================================
                // IMPORTANT
                // ====================================================
                // If this cell already contains sand,
                // DO NOT replace its color.
                //
                // This is what prevents the old sand from changing
                // when a new colored sand is spawned.
                // ====================================================

                if (sand[gx, gy])
                    continue;


                sand[gx, gy] = true;

                // Store THIS spawn's color in THIS cell.
                sandColors[
                    gy * gridWidth + gx
                ] = spawnColor;
            }
        }

        UpdateSandTexture();
    }


    // ============================================================
    // DEFAULT COLOR OVERLOAD
    // ============================================================

    public void SpawnSandAtWorldPosition(
        Vector3 worldPosition)
    {
        SpawnSandAtWorldPosition(
            worldPosition,
            sandColor
        );
    }


    // ============================================================
    // SET CURRENT DEFAULT COLOR
    // ============================================================

    public void SetSandColor(Color newColor)
    {
        // IMPORTANT:
        // This changes ONLY the color used for FUTURE sand.
        //
        // Existing sandColors are NOT touched.
        sandColor = newColor;
    }


    // ============================================================
    // CAMERA
    // ============================================================

    private void SetupCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject cameraObject =
                new GameObject("Main Camera");

            cameraObject.tag = "MainCamera";

            cam =
                cameraObject.AddComponent<Camera>();
        }

        cam.orthographic = true;

        cam.transform.position =
            new Vector3(
                0f,
                0f,
                -10f
            );

        cam.transform.rotation =
            Quaternion.identity;

        cam.orthographicSize =
            worldHeight * 0.5f;

        cam.clearFlags =
            CameraClearFlags.SolidColor;

        cam.backgroundColor =
            new Color(
                0.08f,
                0.07f,
                0.05f,
                1f
            );

        cam.cullingMask = ~0;
    }


    // ============================================================
    // GRID
    // ============================================================

    private void CalculateGrid()
    {
        cellWidth =
            worldWidth /
            gridWidth;

        cellHeight =
            worldHeight /
            gridHeight;


        sand =
            new bool[
                gridWidth,
                gridHeight
            ];

        nextSand =
            new bool[
                gridWidth,
                gridHeight
            ];


        // One color for EVERY sand cell.
        sandColors =
            new Color[
                gridWidth *
                gridHeight
            ];

        nextSandColors =
            new Color[
                gridWidth *
                gridHeight
            ];


        texturePixels =
            new Color[
                gridWidth *
                gridHeight
            ];


        // Initialize colors as transparent.
        for (int i = 0; i < sandColors.Length; i++)
        {
            sandColors[i] = Color.clear;
            nextSandColors[i] = Color.clear;
            texturePixels[i] = Color.clear;
        }
    }


    // ============================================================
    // WORLD
    // ============================================================

    private void CreateWorld()
    {
        GameObject ground =
            new GameObject("Ground");

        ground.transform.SetParent(transform);

        ground.transform.position =
            new Vector3(
                0f,
                groundY,
                0f
            );

        ground.transform.localScale =
            new Vector3(
                worldWidth,
                groundThickness,
                1f
            );

        SpriteRenderer renderer =
            ground.AddComponent<SpriteRenderer>();

        renderer.sprite =
            CreateWhiteSprite();

        renderer.color =
            new Color(
                0.30f,
                0.24f,
                0.16f,
                1f
            );

        renderer.sortingOrder = 0;

        BoxCollider2D collider =
            ground.AddComponent<BoxCollider2D>();

        collider.size =
            Vector2.one;
    }


    // ============================================================
    // SAND TEXTURE
    // ============================================================

    private void CreateSandTexture()
    {
        sandTexture =
            new Texture2D(
                gridWidth,
                gridHeight,
                TextureFormat.RGBA32,
                false
            );

        // Point keeps the sand sharp and clear.
        sandTexture.filterMode =
            FilterMode.Point;

        sandTexture.wrapMode =
            TextureWrapMode.Clamp;


        sandRenderer =
            new GameObject(
                "Sand Simulation"
            ).AddComponent<SpriteRenderer>();

        sandRenderer.transform.SetParent(transform);

        sandRenderer.transform.position =
            Vector3.zero;


        float pixelsPerUnit =
            gridWidth /
            worldWidth;


        Sprite sprite =
            Sprite.Create(
                sandTexture,
                new Rect(
                    0f,
                    0f,
                    gridWidth,
                    gridHeight
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                pixelsPerUnit
            );


        sandRenderer.sprite =
            sprite;

        sandRenderer.transform.localScale =
            Vector3.one;

        sandRenderer.sortingOrder = 5;
    }


    // ============================================================
    // SAND SIMULATION
    // ============================================================

    private void SimulateSand()
    {
        // Clear next grid.
        System.Array.Clear(
            nextSand,
            0,
            nextSand.Length
        );


        // Clear next colors.
        for (int i = 0;
             i < nextSandColors.Length;
             i++)
        {
            nextSandColors[i] =
                Color.clear;
        }


        // Bottom -> Top.
        for (int y = 0;
             y < gridHeight;
             y++)
        {
            for (int x = 0;
                 x < gridWidth;
                 x++)
            {
                if (!sand[x, y])
                    continue;


                MoveSand(
                    x,
                    y
                );
            }
        }


        // Swap sand grids.
        bool[,] tempSand =
            sand;

        sand =
            nextSand;

        nextSand =
            tempSand;


        // Swap color grids.
        Color[] tempColors =
            sandColors;

        sandColors =
            nextSandColors;

        nextSandColors =
            tempColors;
    }


    // ============================================================
    // MOVE SAND
    // ============================================================

    private void MoveSand(
        int x,
        int y)
    {
        int sourceIndex =
            y * gridWidth + x;

        Color particleColor =
            sandColors[sourceIndex];


        // ========================================================
        // GROUND
        // ========================================================

        if (IsGroundCell(y))
        {
            SetNextSand(
                x,
                y,
                particleColor
            );

            return;
        }


        // ========================================================
        // 1. STRAIGHT DOWN
        // ========================================================

        if (CanMove(x, y - 1))
        {
            SetNextSand(
                x,
                y - 1,
                particleColor
            );

            return;
        }


        // ========================================================
        // 2. DIAGONAL DOWN
        // ========================================================

        bool leftFirst =
            Random.value < 0.5f;


        if (leftFirst)
        {
            // Down-left
            if (CanMove(
                x - 1,
                y - 1))
            {
                SetNextSand(
                    x - 1,
                    y - 1,
                    particleColor
                );

                return;
            }


            // Down-right
            if (CanMove(
                x + 1,
                y - 1))
            {
                SetNextSand(
                    x + 1,
                    y - 1,
                    particleColor
                );

                return;
            }
        }
        else
        {
            // Down-right
            if (CanMove(
                x + 1,
                y - 1))
            {
                SetNextSand(
                    x + 1,
                    y - 1,
                    particleColor
                );

                return;
            }


            // Down-left
            if (CanMove(
                x - 1,
                y - 1))
            {
                SetNextSand(
                    x - 1,
                    y - 1,
                    particleColor
                );

                return;
            }
        }


        // ========================================================
        // 3. STAY
        // ========================================================

        SetNextSand(
            x,
            y,
            particleColor
        );
    }


    // ============================================================
    // SET NEXT SAND + COLOR
    // ============================================================

    private void SetNextSand(
        int x,
        int y,
        Color color)
    {
        if (!InsideGrid(x, y))
            return;


        nextSand[x, y] = true;


        int index =
            y * gridWidth + x;


        // The sand's color travels with the sand particle.
        nextSandColors[index] =
            color;
    }


    // ============================================================
    // CAN MOVE
    // ============================================================

    private bool CanMove(
        int x,
        int y)
    {
        if (!InsideGrid(
            x,
            y))
        {
            return false;
        }


        if (IsGroundCell(y))
        {
            return false;
        }


        if (sand[x, y])
        {
            return false;
        }


        if (nextSand[x, y])
        {
            return false;
        }


        return true;
    }


    // ============================================================
    // GROUND
    // ============================================================

    private bool IsGroundCell(
        int y)
    {
        float worldY =
            GridToWorldY(y);

        return
            worldY <=
            groundY +
            groundThickness * 0.5f;
    }


    // ============================================================
    // TEXTURE
    // ============================================================

    private void UpdateSandTexture()
    {
        for (int y = 0;
             y < gridHeight;
             y++)
        {
            for (int x = 0;
                 x < gridWidth;
                 x++)
            {
                int index =
                    y * gridWidth + x;


                // No sand.
                if (!sand[x, y])
                {
                    texturePixels[index] =
                        Color.clear;

                    continue;
                }


                // ==================================================
                // GET THIS PARTICLE'S OWN COLOR
                // ==================================================

                Color baseColor =
                    sandColors[index];


                // ==================================================
                // BASE SAND NOISE
                // ==================================================

                float noise =
                    Mathf.PerlinNoise(
                        x * 0.17f,
                        y * 0.17f
                    );


                // ==================================================
                // MEDIUM GRAIN
                // ==================================================

                float grain =
                    Mathf.PerlinNoise(
                        x * 0.8f + 50f,
                        y * 0.8f + 50f
                    );


                // Use THIS sand cell's color.
                Color lightSand =
                    baseColor +
                    sandVariation * 0.35f;

                Color darkSand =
                    baseColor -
                    sandVariation * 0.35f;


                Color grainColor =
                    Color.Lerp(
                        darkSand,
                        lightSand,
                        noise
                    );


                // ==================================================
                // SMALL GRAIN VARIATION
                // ==================================================

                float randomGrain =
                    Mathf.PerlinNoise(
                        x * 2.5f + 100f,
                        y * 2.5f + 100f
                    );


                grainColor *=
                    Mathf.Lerp(
                        0.82f,
                        1.12f,
                        randomGrain
                    );


                // ==================================================
                // FINE DETAIL
                // ==================================================

                float fineGrain =
                    Mathf.PerlinNoise(
                        x * 5.0f + 300f,
                        y * 5.0f + 300f
                    );


                grainColor *=
                    Mathf.Lerp(
                        0.94f,
                        1.06f,
                        fineGrain
                    );


                // Existing sand is always visible.
                grainColor.a = 1f;


                texturePixels[index] =
                    grainColor;
            }
        }


        sandTexture.SetPixels(
            texturePixels
        );


        // No mipmaps = sharper sand.
        sandTexture.Apply(false);
    }


    // ============================================================
    // WORLD -> GRID
    // ============================================================

    private Vector2 WorldToGrid(
        Vector2 world)
    {
        float gx =
            (world.x +
             worldWidth * 0.5f)
            / worldWidth *
            gridWidth;


        float gy =
            (world.y +
             worldHeight * 0.5f)
            / worldHeight *
            gridHeight;


        return new Vector2(
            gx,
            gy
        );
    }


    // ============================================================
    // GRID -> WORLD Y
    // ============================================================

    private float GridToWorldY(
        int y)
    {
        return
            -worldHeight * 0.5f +
            (y + 0.5f) *
            cellHeight;
    }


    // ============================================================
    // GRID UTILITY
    // ============================================================

    private bool InsideGrid(
        int x,
        int y)
    {
        return
            x >= 0 &&
            x < gridWidth &&
            y >= 0 &&
            y < gridHeight;
    }


    // ============================================================
    // WHITE SPRITE
    // ============================================================

    private Sprite CreateWhiteSprite()
    {
        Texture2D texture =
            new Texture2D(
                1,
                1
            );

        texture.SetPixel(
            0,
            0,
            Color.white
        );

        texture.Apply();


        return Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                1,
                1
            ),
            Vector2.one * 0.5f,
            1f
        );
    }
}