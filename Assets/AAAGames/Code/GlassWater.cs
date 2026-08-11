using System.Collections.Generic;
using UnityEngine;

public class GlassWater : MonoBehaviour
{
    [Header("Water")]
    [SerializeField] private float maxWater = 10f;

    [Header("Water Visual")]
    [SerializeField] private SpriteRenderer waterRenderer;

    [Header("Water Fill Trigger")]
    [SerializeField] private Transform waterFillTrigger;

    [SerializeField] private float bottomY = -2f;
    [SerializeField] private float topY = 2f;

    [Header("Queue Fill Speed")]
    [SerializeField] private float fillSpeed = 0.25f;

    [Header("Water FX")]
    [SerializeField] private GameObject WaterFx;

    private Material waterMaterial;

    private float visibleWater;
    private float currentFill;

    private readonly Queue<float> waterQueue =
        new Queue<float>();

    private float activeAmount;
    private float activeProgress;

    private void Awake()
    {
        waterMaterial = waterRenderer.material;

        visibleWater = 0f;
        currentFill = 0f;

        ApplyWaterLevel();
    }

    private void Update()
    {
        ProcessWaterQueue();

        ApplyWaterLevel();
    }

    private void ProcessWaterQueue()
    {
        // Get next drop.
        if (activeAmount <= 0f)
        {
            if (waterQueue.Count == 0)
                return;

            activeAmount = waterQueue.Dequeue();
            activeProgress = 0f;
        }

        // -----------------------------------------
        // CONSTANT WATER MOVEMENT
        // -----------------------------------------

        float amount =
            fillSpeed * Time.deltaTime;

        activeProgress += amount;

        float actualAdded =
            Mathf.Min(
                amount,
                activeAmount
            );

        activeAmount -= actualAdded;

        visibleWater += actualAdded;

        visibleWater =
            Mathf.Clamp(
                visibleWater,
                0f,
                maxWater
            );

        currentFill =
            visibleWater / maxWater;

        // Current drop finished.
        if (activeAmount <= 0.00001f)
        {
            activeAmount = 0f;
            activeProgress = 0f;
        }
    }

    public void AddWater(
        float amount,
        Vector2 hitPosition)
    {
        if (amount <= 0f)
            return;

        // Calculate water already waiting.
        float queuedWater = 0f;

        foreach (float queuedAmount in waterQueue)
        {
            queuedWater += queuedAmount;
        }

        float totalReserved =
            visibleWater +
            activeAmount +
            queuedWater;

        // Prevent overflow.
        float available =
            maxWater - totalReserved;

        amount =
            Mathf.Min(
                amount,
                available
            );

        if (amount <= 0f)
            return;

        // -----------------------------------------
        // QUEUE THE DROP
        // -----------------------------------------

        waterQueue.Enqueue(amount);

        // -----------------------------------------
        // SPLASH AT ACTUAL HIT POSITION
        // -----------------------------------------

        SpawnWaterFX(hitPosition);
    }

    private void ApplyWaterLevel()
    {
        if (waterMaterial != null)
        {
            waterMaterial.SetFloat(
                "_Fill",
                currentFill
            );
        }

        if (waterFillTrigger != null && waterRenderer != null)
        {
            Bounds worldBounds = waterRenderer.bounds;

            float waterY = Mathf.Lerp(
                worldBounds.min.y,
                worldBounds.max.y,
                currentFill
            );

            Vector3 worldPosition =
                waterFillTrigger.position;

            worldPosition.y = waterY;

            waterFillTrigger.position =
                worldPosition;
        }
    }
    private float GetWaterSurfaceY()
    {
        if (waterRenderer == null)
            return transform.localPosition.y;

        Bounds bounds = waterRenderer.sprite.bounds;

        float bottom = bounds.min.y;
        float top = bounds.max.y;

        return Mathf.Lerp(
            bottom,
            top,
            currentFill
        );
    }
    private void SpawnWaterFX(Vector2 hitPosition)
    {
        if (WaterFx == null)
            return;

        Instantiate(
            WaterFx,
            hitPosition,
            Quaternion.Euler(
                -90f,
                0f,
                0f
            )
        );
    }

    public float GetWaterLevel()
    {
        return currentFill;
    }

    public bool IsFull()
    {
        return currentFill >= 0.999f;
    }
}