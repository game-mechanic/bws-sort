using UnityEngine;
using UnityEngine.UI;

public class FallingSandController : MonoBehaviour
{
    [Header("Sand")]
    public Color sandColor = new Color(
        0.76f,
        0.55f,
        0.30f,
        1f
    );

    [Header("Sand Stream")]
    [Range(1f, 20f)]
    public float spawnWidth = 2f;

    [Range(1f, 30f)]
    public float spawnHeight = 4f;

    [Header("Simulation")]
    
    public float simulationStepsPerSecond = 300f;

    [Range(1, 20)]
    public int maxStepsPerFrame = 8;

    [Header("References")]
    public ComputeShader fallingSandShader;

    public RenderTexture fallingSandRT;


    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private Image image;

    private Camera mainCamera;

    private float accumulator;

    private bool rightFirst;

    private Vector2Int spawnPosition;

    private bool spawnSand;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "FallingSandController: Main Camera not found."
            );
        }

        if (fallingSandShader == null)
        {
            Debug.LogError(
                "FallingSandController: Compute Shader is not assigned."
            );

            enabled = false;
            return;
        }

        CreateRenderTexture();

        SetupImage();

        Dispatch("Initialize");
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        UpdateSimulation();
    }


    // =====================================================
    // START SAND
    // Called from Bubble.cs
    // =====================================================

    public void StartSand(Vector2 screenPosition)
    {
        spawnPosition = new Vector2Int(
            Mathf.RoundToInt(screenPosition.x),
            Mathf.RoundToInt(screenPosition.y)
        );

        spawnSand = true;

        CancelInvoke(nameof(StopSand));
        Invoke(nameof(StopSand), 1.5f);
    }


    // =====================================================
    // STOP SAND
    // =====================================================

    public void StopSand()
    {
        spawnSand = false;
    }


    // =====================================================
    // SIMULATION
    // =====================================================

    private void UpdateSimulation()
    {
        accumulator += Time.deltaTime;

        float stepTime =
            1f /
            Mathf.Max(
                1f,
                simulationStepsPerSecond
            );

        int steps = 0;

        while (
            accumulator >= stepTime &&
            steps < maxStepsPerFrame
        )
        {
            Dispatch("SandFall");

            rightFirst = !rightFirst;

            accumulator -= stepTime;

            steps++;
        }

        if (steps >= maxStepsPerFrame)
        {
            accumulator = 0f;
        }
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

        fallingSandRT = new RenderTexture(
            Screen.width,
            Screen.height,
            0,
            RenderTextureFormat.ARGB32
        );

        fallingSandRT.enableRandomWrite = true;

        fallingSandRT.filterMode =
            FilterMode.Point;

        fallingSandRT.wrapMode =
            TextureWrapMode.Clamp;

        fallingSandRT.Create();
    }


    // =====================================================
    // SET UI IMAGE
    // =====================================================

    private void SetupImage()
    {
        image = GetComponent<Image>();

        if (image == null)
        {
            Debug.LogError(
                "FallingSandController must be attached to a UI Image."
            );

            return;
        }

        if (image.material == null)
        {
            Debug.LogError(
                "Falling Sand UI Image has no material."
            );

            return;
        }

        image.material.SetTexture(
            "_MainTex",
            fallingSandRT
        );
    }


    // =====================================================
    // DISPATCH COMPUTE SHADER
    // =====================================================

    private void Dispatch(string kernelName)
    {
        if (fallingSandShader == null)
        {
            return;
        }

        int kernel =
            fallingSandShader.FindKernel(
                kernelName
            );


        // -------------------------------------------------
        // Render Texture
        // -------------------------------------------------

        fallingSandShader.SetTexture(
            kernel,
            "sandRT",
            fallingSandRT
        );


        // -------------------------------------------------
        // Spawn Position
        // -------------------------------------------------

        fallingSandShader.SetFloat(
            "posX",
            spawnPosition.x
        );

        fallingSandShader.SetFloat(
            "posY",
            spawnPosition.y
        );


        // -------------------------------------------------
        // Sand Stream Size
        // -------------------------------------------------

        fallingSandShader.SetFloat(
            "spawnWidth",
            spawnWidth
        );

        fallingSandShader.SetFloat(
            "spawnHeight",
            spawnHeight
        );


        // -------------------------------------------------
        // Direction
        // -------------------------------------------------

        fallingSandShader.SetBool(
            "rightFirst",
            rightFirst
        );


        // -------------------------------------------------
        // Spawn
        // -------------------------------------------------

        fallingSandShader.SetBool(
            "spawnSand",
            spawnSand
        );


        // -------------------------------------------------
        // Sand Color
        // -------------------------------------------------

        fallingSandShader.SetVector(
            "color",
            new Vector4(
                sandColor.r,
                sandColor.g,
                sandColor.b,
                sandColor.a
            )
        );


        // -------------------------------------------------
        // Thread Groups
        // -------------------------------------------------

        int groupsX =
            Mathf.CeilToInt(
                fallingSandRT.width / 8f
            );

        int groupsY =
            Mathf.CeilToInt(
                fallingSandRT.height / 8f
            );


        fallingSandShader.Dispatch(
            kernel,
            groupsX,
            groupsY,
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