using UnityEngine;
using TMPro;
using System;

/// <summary>
/// A bubble that hangs from a chain/spring and swings like a leaf on a tree.
/// Clicking moves it to the slot system.
/// </summary>
public class TreeBubble : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] SpriteRenderer bgRenderer;
    [SerializeField] SpriteRenderer iconRenderer;
    [SerializeField] TextMeshPro textLabel;
    [SerializeField] GameObject highlightEffect;
    
    [Header("Chain Visuals")]
    [SerializeField] LineRenderer chainLine;
    
    [Header("Manual Override")]
    [SerializeField] bool manuallyPositioned = false; // Check this to lock position
    
    // Called when inspector values change in Editor
    void OnValidate()
    {
        // When manually positioned is checked, store current position
        if (manuallyPositioned && !Application.isPlaying)
        {
            storedOriginalPosition = transform.position;
            Debug.Log($"{name}: Manual positioning enabled - locked position: {storedOriginalPosition}");
        }
    }
    
    [Header("Collision")]
    [SerializeField] float colliderRadius = 0.45f; // Slightly smaller than visual to prevent overlap
    
    [Header("Swing Settings - Adjust in Inspector")]
    [SerializeField][Range(0f, 30f)] float maxRotationAngle = 15f; // Max tilt in degrees
    [SerializeField][Range(0f, 1f)] float maxPositionOffsetX = 0.3f; // Max horizontal movement
    [SerializeField][Range(0f, 1f)] float maxPositionOffsetY = 0.15f; // Max vertical movement
    [SerializeField][Range(0.5f, 3f)] float swingSpeed = 1f; // Speed of swing animation
    [SerializeField][Range(0f, 1f)] float naturalVariation = 0.3f; // How much random variation (0=uniform, 1=very random)
    
    [Header("Runtime Data")]
    [SerializeField] BubbleType assignedCategory; // Serialized to persist in editor
    [SerializeField] Vector3 storedAnchorPoint; // Store anchor for Play mode initialization
    [SerializeField] Vector3 storedOriginalPosition; // Store spawn position (serialized)
    [SerializeField] float storedChainLength = 1.5f;
    [SerializeField] float storedSwingForce = 2f;
    
    CircleCollider2D col;
    Vector3 anchorPoint;
    Vector3 originalPosition; // Store spawn position
    BubbleType category;
    bool isInSlot = false;
    bool physicsInitialized = false;
    
    // Enhanced swing animation
    float swingTime;
    float swingDirection; // -1 or 1 for left/right
    float randomOffset;
    float noiseOffsetX; // For Perlin noise variation
    float noiseOffsetY;
    float baseSpeed; // Base speed for this bubble
    
    public BubbleType Category => category ?? assignedCategory;
    public event Action<TreeBubble> OnClicked;
    
    void Awake()
    {
        // Restore category from serialized field if available
        if (assignedCategory != null && category == null)
        {
            category = assignedCategory;
        }
        
        // Ensure collider exists (required for OnMouseDown)
        col = GetComponent<CircleCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = colliderRadius;
        col.isTrigger = false; // Physical collider for OnMouseDown
    }
    
    void Start()
    {
        // For manually positioned bubbles, use current position as original
        if (manuallyPositioned)
        {
            originalPosition = transform.position;
            storedOriginalPosition = transform.position;
            Debug.Log($"{name}: MANUAL - Using current position as original: {originalPosition}");
        }
        // Restore original position from stored value for auto-spawned
        else if (storedOriginalPosition != Vector3.zero)
        {
            originalPosition = storedOriginalPosition;
        }
        else if (originalPosition == Vector3.zero)
        {
            // Fallback: use current position
            originalPosition = transform.position;
            storedOriginalPosition = transform.position;
        }
        
        Debug.Log($"{name}: Start - manuallyPositioned={manuallyPositioned}, originalPosition={originalPosition}, currentPos={transform.position}");
        
        // Setup swing animation when entering Play mode
        if (Application.isPlaying && !physicsInitialized)
        {
            InitializeSwingAnimation();
        }
    }
    
    public void Initialize(Vector3 anchor, float chainLength, float swingForce, BubbleType cat, Sprite icon, string text)
    {
        // Skip initialization if manually positioned
        if (manuallyPositioned)
        {
            Debug.Log($"{name}: Manually positioned - skipping automatic initialization, keeping current position");
            
            // Only set category and visuals, don't change position
            category = cat;
            assignedCategory = cat;
            
            // Store current position as original for swinging
            originalPosition = transform.position;
            storedOriginalPosition = transform.position;
            
            // Setup visuals only
            SetupVisuals(icon, text, cat);
            
            if (highlightEffect != null)
                highlightEffect.SetActive(false);
            
            // Setup swing if in play mode
            if (Application.isPlaying && !physicsInitialized)
            {
                InitializeSwingAnimation();
            }
            
            return;
        }
        
        // Normal automatic initialization
        anchorPoint = anchor;
        storedAnchorPoint = anchor;
        storedChainLength = chainLength;
        storedSwingForce = swingForce;
        
        category = cat;
        assignedCategory = cat; // Store in serialized field to persist
        
        // Validate category
        if (category == null)
        {
            Debug.LogError($"TreeBubble '{name}': Category is NULL during initialization!");
        }
        
        // Position bubble at spawn point (formation position)
        transform.position = anchor;
        originalPosition = anchor; // Store for swinging around
        storedOriginalPosition = anchor; // Serialize to persist in Play mode
        
        // Setup swing if already in play mode
        if (Application.isPlaying && !physicsInitialized)
        {
            InitializeSwingAnimation();
        }
        
        // Setup visuals
        SetupVisuals(icon, text, cat);
        
        // Setup chain line (from slightly above bubble)
        if (chainLine != null)
        {
            chainLine.positionCount = 2;
            chainLine.startWidth = 0.05f;
            chainLine.endWidth = 0.05f;
            chainLine.SetPosition(0, anchor + Vector3.up * 0.5f); // Chain from above
            chainLine.SetPosition(1, transform.position);
        }
        
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }
    
    void SetupVisuals(Sprite icon, string text, BubbleType cat)
    {
        if (icon != null && iconRenderer != null)
        {
            iconRenderer.sprite = icon;
            iconRenderer.gameObject.SetActive(true);
            if (textLabel != null) textLabel.gameObject.SetActive(false);
        }
        else if (!string.IsNullOrEmpty(text) && textLabel != null)
        {
            textLabel.text = text;
            textLabel.gameObject.SetActive(true);
            if (iconRenderer != null) iconRenderer.gameObject.SetActive(false);
        }
        else
        {
            // Fallback: show category name if available
            if (textLabel != null && cat != null)
            {
                textLabel.text = cat.name;
                textLabel.gameObject.SetActive(true);
                if (iconRenderer != null) iconRenderer.gameObject.SetActive(false);
            }
        }
    }
    
    void InitializeSwingAnimation()
    {
        if (physicsInitialized)
            return;
        
        // Random starting time for variation
        swingTime = UnityEngine.Random.Range(0f, 100f);
        
        // Random direction (left or right)
        swingDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
        
        // Random offset so all bubbles don't sync
        randomOffset = UnityEngine.Random.Range(0f, 6.28f); // 0 to 2*PI
        
        // Random Perlin noise offsets for natural variation
        noiseOffsetX = UnityEngine.Random.Range(0f, 1000f);
        noiseOffsetY = UnityEngine.Random.Range(0f, 1000f);
        
        // Each bubble has slightly different base speed
        baseSpeed = UnityEngine.Random.Range(0.8f, 1.2f);
        
        physicsInitialized = true;
        
        Debug.Log($"{name}: Enhanced swing initialized - direction={swingDirection}, baseSpeed={baseSpeed}");
    }
    
    void Update()
    {
        // Apply enhanced pendulum/leaf swing animation if not in slot
        if (!isInSlot && physicsInitialized && Application.isPlaying)
        {
            // Update time with base speed variation
            swingTime += Time.deltaTime * swingSpeed * baseSpeed;
            
            // PRIMARY SWING: Main sine wave for smooth swinging
            float primarySwing = Mathf.Sin(swingTime + randomOffset) * swingDirection;
            
            // NATURAL VARIATION: Add Perlin noise for organic, non-uniform motion
            float noiseX = (Mathf.PerlinNoise(swingTime * 0.5f + noiseOffsetX, 0f) - 0.5f) * 2f; // -1 to 1
            float noiseY = (Mathf.PerlinNoise(swingTime * 0.3f + noiseOffsetY, 0f) - 0.5f) * 2f;
            
            // Blend primary swing with natural variation
            float blendedSwingX = Mathf.Lerp(primarySwing, noiseX, naturalVariation);
            float blendedSwingY = Mathf.Lerp(0f, noiseY, naturalVariation * 0.5f); // Less Y variation
            
            // SECONDARY MOTION: Add subtle secondary frequency (like wind gusts)
            float secondarySwing = Mathf.Sin(swingTime * 2.3f + randomOffset * 0.7f) * 0.15f;
            blendedSwingX += secondarySwing;
            
            // Apply Z-axis rotation (tilt left/right) - follows X motion
            float rotationZ = blendedSwingX * maxRotationAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            
            // POSITION: Follows rotation with natural variation
            float positionX = blendedSwingX * maxPositionOffsetX;
            
            // Y position: Creates arc motion (pendulum drops at extremes) + variation
            float arcDrop = -Mathf.Abs(blendedSwingX) * maxPositionOffsetY * 0.5f;
            float positionY = arcDrop + (blendedSwingY * maxPositionOffsetY * 0.5f);
            
            transform.position = originalPosition + new Vector3(positionX, positionY, 0f);
        }
        
        // Update chain visual
        if (!isInSlot && chainLine != null && Application.isPlaying)
        {
            chainLine.SetPosition(0, originalPosition + Vector3.up * 0.5f);
            chainLine.SetPosition(1, transform.position);
        }
        
        // Draw chain in edit mode too
        if (!Application.isPlaying && chainLine != null)
        {
            chainLine.SetPosition(0, anchorPoint + Vector3.up * 0.5f);
            chainLine.SetPosition(1, transform.position);
        }
    }
    
    void OnMouseDown()
    {
        if (!isInSlot && Application.isPlaying)
        {
            Debug.Log($"TreeBubble clicked: {name}, Category: {(category != null ? category.name : "null")}");
            OnClicked?.Invoke(this);
            if (highlightEffect != null)
                highlightEffect.SetActive(true);
        }
    }
    
    void OnMouseEnter()
    {
        if (!isInSlot && highlightEffect != null && Application.isPlaying)
            highlightEffect.SetActive(true);
    }
    
    void OnMouseExit()
    {
        if (!isInSlot && highlightEffect != null && Application.isPlaying)
            highlightEffect.SetActive(false);
    }
    
    public void DisablePhysics()
    {
        isInSlot = true;
        
        // Reset rotation and position
        transform.rotation = Quaternion.identity;
        
        if (chainLine != null)
            chainLine.enabled = false;
        
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }
    
    void OnDrawGizmosSelected()
    {
        // Show collider size in editor
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, colliderRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, colliderRadius);
    }
}
