using UnityEngine;
using UnityEngine.UI;

public class FallingSandController : MonoBehaviour
{
    [Header("Sand Settings")]
    [Min(1f)]
    public float range = 5f;

    [Tooltip("Fixed color for all sand")]
    public Color sandColor = new Color(
        0.851f,
        0.651f,
        0.29f,
        1f
    );


    [Header("References")]
    public ComputeShader fallingSandShader;
    public RenderTexture fallingSandRT;


    [Header("Sand Colliders")]
    [Tooltip("Drag Collider2D objects that sand should collide with")]
    public Collider2D[] sandColliders;

    [Range(1, 50)]
    public int maxColliders = 20;


    [Header("Natural Sand Movement")]
    [Range(1, 10)]
    public int lateralSpread = 1;


    [Header("Simulation Speed")]
    [Range(60, 600)]
    public float simulationStepsPerSecond = 300f;

    [Range(1, 20)]
    public int maxStepsPerFrame = 8;


    [Header("Spawn Settings")]
    [Range(1f, 50f)]
    public float minRange = 1f;

    [Range(1f, 100f)]
    public float maxRange = 50f;


    // =====================================================
    // SHADER KERNELS
    // =====================================================

    private const string fallingSandKernel = "SandFall";
    private const string initializeKernel = "Initialize";


    // =====================================================
    // SHADER PROPERTIES
    // =====================================================

    private const string sandRTProperty = "sandRT";
    private const string posXProperty = "posX";
    private const string posYProperty = "posY";
    private const string rangeProperty = "range";
    private const string rightFirstProperty = "rightFirst";
    private const string colorProperty = "color";
    private const string spawnSandProperty = "spawnSand";


    // =====================================================
    // VARIABLES
    // =====================================================

    public Vector2Int position;

    private bool rightFirst;
    private bool spawnSand;

    private Image image;

    private Camera mainCamera;

    private float simulationAccumulator;
    private float simulationStepTime;

    private Vector4[] colliderBounds;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        // Check Compute Shader
        if (fallingSandShader == null)
        {
            Debug.LogError(
                "FallingSandController: " +
                "Falling Sand Shader is NOT assigned!"
            );

            enabled = false;
            return;
        }


        // Main Camera
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "FallingSandController: " +
                "Main Camera not found. " +
                "Make sure your camera has the MainCamera tag."
            );
        }


        // Simulation
        simulationStepTime =
            1f /
            Mathf.Max(
                1f,
                simulationStepsPerSecond
            );


        // Collider array
        colliderBounds =
            new Vector4[maxColliders];


        // Create RT
        CreateRenderTexture();


        // Connect RT to UI Image
        SetMaterialTexture();


        // Clear simulation
        Dispatch(initializeKernel);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        UpdateMousePosition();

        UpdateInput();

        UpdateSimulation();
    }


    // =====================================================
    // MOUSE POSITION
    // OLD UNITY INPUT SYSTEM
    // =====================================================

    private void UpdateMousePosition()
    {
        Vector3 mousePosition =
            Input.mousePosition;


        position =
            new Vector2Int(
                Mathf.RoundToInt(
                    mousePosition.x
                ),

                Mathf.RoundToInt(
                    mousePosition.y
                )
            );
    }


    // =====================================================
    // INPUT
    // =====================================================

    private void UpdateInput()
    {
        // Left mouse button
        spawnSand =
            Input.GetMouseButton(0);


        // Mouse wheel
        float scroll =
            Input.GetAxis(
                "Mouse ScrollWheel"
            );


        if (scroll > 0f)
        {
            range += 1f;
        }
        else if (scroll < 0f)
        {
            range -= 1f;
        }


        range =
            Mathf.Clamp(
                range,
                minRange,
                maxRange
            );
    }


    // =====================================================
    // SAND SIMULATION
    // =====================================================

    private void UpdateSimulation()
    {
        simulationStepTime =
            1f /
            Mathf.Max(
                1f,
                simulationStepsPerSecond
            );


        simulationAccumulator +=
            Time.deltaTime;


        int steps = 0;


        while (
            simulationAccumulator >=
            simulationStepTime &&

            steps <
            maxStepsPerFrame
        )
        {
            Dispatch(
                fallingSandKernel
            );


            // Alternate left/right
            rightFirst =
                !rightFirst;


            simulationAccumulator -=
                simulationStepTime;


            steps++;
        }


        // Prevent huge simulation backlog
        if (
            steps >=
            maxStepsPerFrame
        )
        {
            simulationAccumulator = 0f;
        }
    }


    // =====================================================
    // SEND COLLIDERS
    // =====================================================

    private void SendCollidersToShader()
    {
        if (mainCamera == null)
            return;


        if (
            colliderBounds == null ||

            colliderBounds.Length !=
            maxColliders
        )
        {
            colliderBounds =
                new Vector4[maxColliders];
        }


        // Clear old collider data
        for (
            int i = 0;
            i < colliderBounds.Length;
            i++
        )
        {
            colliderBounds[i] =
                Vector4.zero;
        }


        int colliderCount = 0;


        if (sandColliders != null)
        {
            foreach (
                Collider2D collider
                in sandColliders
            )
            {
                if (collider == null)
                    continue;


                if (!collider.enabled)
                    continue;


                if (
                    !collider.gameObject
                    .activeInHierarchy
                )
                    continue;


                if (
                    colliderCount >=
                    maxColliders
                )
                {
                    break;
                }


                Bounds bounds =
                    collider.bounds;


                // World → Screen
                Vector3 minScreen =
                    mainCamera.WorldToScreenPoint(
                        bounds.min
                    );


                Vector3 maxScreen =
                    mainCamera.WorldToScreenPoint(
                        bounds.max
                    );


                float minX =
                    Mathf.Min(
                        minScreen.x,
                        maxScreen.x
                    );


                float maxX =
                    Mathf.Max(
                        minScreen.x,
                        maxScreen.x
                    );


                float minY =
                    Mathf.Min(
                        minScreen.y,
                        maxScreen.y
                    );


                float maxY =
                    Mathf.Max(
                        minScreen.y,
                        maxScreen.y
                    );


                colliderBounds[
                    colliderCount
                ] =
                    new Vector4(
                        minX,
                        minY,
                        maxX,
                        maxY
                    );


                colliderCount++;
            }
        }


        // Send collider count
        fallingSandShader.SetInt(
            "colliderCount",
            colliderCount
        );


        // Send collider bounds
        fallingSandShader.SetVectorArray(
            "colliderBounds",
            colliderBounds
        );
    }


    // =====================================================
    // CREATE RENDER TEXTURE
    // =====================================================

    private void CreateRenderTexture()
    {
        if (fallingSandRT != null)
        {
            fallingSandRT.Release();
        }


        fallingSandRT =
            new RenderTexture(
                Screen.width,
                Screen.height,
                0,
                RenderTextureFormat.ARGB32
            );


        fallingSandRT.enableRandomWrite =
            true;


        fallingSandRT.filterMode =
            FilterMode.Point;


        fallingSandRT.wrapMode =
            TextureWrapMode.Clamp;


        fallingSandRT.Create();
    }


    // =====================================================
    // SET MATERIAL
    // =====================================================

    private void SetMaterialTexture()
    {
        image =
            GetComponent<Image>();


        if (image == null)
        {
            Debug.LogError(
                "FallingSandController must " +
                "be attached to a UI Image."
            );

            return;
        }


        if (image.material == null)
        {
            Debug.LogError(
                "Falling Sand Image has no Material."
            );

            return;
        }


        image.material.SetTexture(
            "_MainTex",
            fallingSandRT
        );
    }


    // =====================================================
    // COMPUTE SHADER DISPATCH
    // =====================================================

    private void Dispatch(
        string kernel
    )
    {
        if (fallingSandShader == null)
            return;


        int kernelHandle;


        // Find kernel
        try
        {
            kernelHandle =
                fallingSandShader.FindKernel(
                    kernel
                );
        }
        catch
        {
            Debug.LogError(
                "FallingSandController: " +
                "Compute Shader does not contain kernel: "
                + kernel
            );

            enabled = false;
            return;
        }


        // RenderTexture
        fallingSandShader.SetTexture(
            kernelHandle,
            sandRTProperty,
            fallingSandRT
        );


        // Collider data
        SendCollidersToShader();


        // Mouse position
        fallingSandShader.SetFloat(
            posXProperty,
            position.x
        );


        fallingSandShader.SetFloat(
            posYProperty,
            position.y
        );


        // Brush range
        fallingSandShader.SetFloat(
            rangeProperty,
            range
        );


        // Direction
        fallingSandShader.SetBool(
            rightFirstProperty,
            rightFirst
        );


        // Spawn
        fallingSandShader.SetBool(
            spawnSandProperty,
            spawnSand
        );


        // Sand color
        fallingSandShader.SetVector(
            colorProperty,
            new Vector4(
                sandColor.r,
                sandColor.g,
                sandColor.b,
                sandColor.a
            )
        );


        // Lateral movement
        fallingSandShader.SetInt(
            "lateralSpread",
            lateralSpread
        );


        // Thread groups
        int threadGroupsX =
            Mathf.CeilToInt(
                fallingSandRT.width /
                8f
            );


        int threadGroupsY =
            Mathf.CeilToInt(
                fallingSandRT.height /
                8f
            );


        fallingSandShader.Dispatch(
            kernelHandle,
            threadGroupsX,
            threadGroupsY,
            1
        );
    }


    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {
        if (fallingSandRT != null)
        {
            fallingSandRT.Release();

            fallingSandRT = null;
        }
    }
}