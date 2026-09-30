using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Spawns category bubbles hanging from a tree with swinging physics.
/// Bubbles move to slots sequentially when clicked, and 4 adjacent matching bubbles pop.
/// </summary>
public class TreeBubbleManager : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField] SpawnAreaShape spawnShape = SpawnAreaShape.Circle;
    [SerializeField] float radius = 3f;
    [SerializeField] Vector2 squareSize = new Vector2(5f, 4f);
    [SerializeField] float minDistanceBetweenBubbles = 0.8f;
    [SerializeField] int maxSpawnAttempts = 30;
    
    public enum SpawnAreaShape { Circle, Square }
    
    [Header("Tree Bubbles")]
    [SerializeField] TreeBubble treeBubblePrefab;
    [SerializeField] int bubbleCount = 20;
    [SerializeField] List<CategoryBubbleData> categoryBubbles = new List<CategoryBubbleData>();
    
    [Header("Slots")]
    [SerializeField] Transform[] slots = new Transform[5];
    [SerializeField] float slotBubbleScale = 0.8f;
    
    [Header("Chain Settings")]
    [SerializeField] float chainLength = 1.5f;
    [SerializeField] float swingForce = 2f;
    [SerializeField][Range(0f, 5f)] float swingStrength = 2f; // Adjustable swing intensity
    
    List<TreeBubble> spawnedBubbles = new List<TreeBubble>();
    List<TreeBubble> slottedBubbles = new List<TreeBubble>();
    int currentSlotIndex = 0;
    
    [System.Serializable]
    public class CategoryBubbleData
    {
        public BubbleType category;
        public Sprite icon;
        public string text;
    }
    
    void Awake()
    {
        // Initialize particle pool if needed
        ParticlePool.Init();
    }
    
    void Start()
    {
        // Don't auto-spawn - only spawn manually via editor button
        // Bubbles spawned in editor mode will carry over to play mode
        
        // Re-subscribe to click events for bubbles spawned in editor
        if (Application.isPlaying)
        {
            TreeBubble[] existingBubbles = GetComponentsInChildren<TreeBubble>();
            foreach (var bubble in existingBubbles)
            {
                // Unsubscribe first to avoid duplicate subscriptions
                bubble.OnClicked -= HandleBubbleClicked;
                // Subscribe
                bubble.OnClicked += HandleBubbleClicked;
            }
            
            if (existingBubbles.Length > 0)
            {
                Debug.Log($"<color=cyan>Subscribed to {existingBubbles.Length} bubbles' click events</color>");
            }
        }
    }
    
    [EditorButton]
    public void SpawnBubblesInEditor()
    {
        ClearBubbles();
        SpawnBubbles();
        Debug.Log($"Spawned {spawnedBubbles.Count} out of {bubbleCount} bubbles");
    }
    
    [EditorButton]
    public void ValidateSetup()
    {
        Debug.Log("=== TreeBubbleManager Validation ===");
        
        if (treeBubblePrefab == null)
            Debug.LogError("✗ TreeBubble prefab NOT assigned!");
        else
            Debug.Log($"✓ TreeBubble prefab: {treeBubblePrefab.name}");
        
        if (categoryBubbles == null || categoryBubbles.Count == 0)
            Debug.LogError("✗ No category bubbles defined!");
        else
        {
            Debug.Log($"✓ {categoryBubbles.Count} category entries");
            for (int i = 0; i < categoryBubbles.Count; i++)
            {
                var cat = categoryBubbles[i];
                if (cat.category == null)
                    Debug.LogWarning($"  ✗ Entry [{i}]: Category is NULL!");
                else
                {
                    string display = cat.icon != null ? "Icon" : (!string.IsNullOrEmpty(cat.text) ? "Text" : "Category Name");
                    Debug.Log($"  ✓ Entry [{i}]: {cat.category.name} ({display})");
                }
            }
        }
        
        if (slots == null || slots.Length == 0)
            Debug.LogError("✗ No slots assigned!");
        else
        {
            int validSlots = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) validSlots++;
                else Debug.LogWarning($"  ✗ Slot[{i}] is NULL!");
            }
            Debug.Log($"✓ {validSlots}/{slots.Length} slots assigned");
        }
        
        // Check for collider on prefab
        if (treeBubblePrefab != null)
        {
            var col = treeBubblePrefab.GetComponent<CircleCollider2D>();
            if (col == null)
                Debug.LogWarning("⚠ TreeBubble prefab missing CircleCollider2D (will be auto-added)");
            else
                Debug.Log($"✓ TreeBubble has CircleCollider2D (radius: {col.radius})");
        }
        
        // Validate current spawn
        int currentBubbles = GetComponentsInChildren<TreeBubble>().Length;
        Debug.Log($"Currently spawned: {currentBubbles} bubbles");
        
        // Distance validation
        float estimatedMinDistance = minDistanceBetweenBubbles;
        float area = Mathf.PI * radius * radius;
        float density = bubbleCount / area;
        Debug.Log($"Spawn density: {density:F2} bubbles per unit² (Radius: {radius}, Count: {bubbleCount})");
        
        if (density > 1.5f)
            Debug.LogWarning($"⚠ High density! Bubbles may overlap. Consider increasing radius or decreasing count.");
        
        Debug.Log("=== Validation Complete ===");
    }
    
    [EditorButton]
    public void ClearBubbles()
    {
        // Clear in edit mode
        if (!Application.isPlaying)
        {
            // Clear bubbles
            TreeBubble[] existingBubbles = GetComponentsInChildren<TreeBubble>();
            for (int i = existingBubbles.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(existingBubbles[i].gameObject);
            }
            
            // Clear any anchor objects
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Anchor"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        else
        {
            foreach (var bubble in spawnedBubbles)
            {
                if (bubble != null)
                    Destroy(bubble.gameObject);
            }
            
            // Clear anchors
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Anchor"))
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        spawnedBubbles.Clear();
        slottedBubbles.Clear();
        currentSlotIndex = 0;
        
        Debug.Log("<color=yellow>Cleared all bubbles and anchors</color>");
    }
    
    void SpawnBubbles()
    {
        if (treeBubblePrefab == null)
        {
            Debug.LogError("TreeBubbleManager: TreeBubble prefab is not assigned!");
            return;
        }
        
        if (categoryBubbles == null || categoryBubbles.Count == 0)
        {
            Debug.LogError("TreeBubbleManager: No category bubbles defined!");
            return;
        }
        
        // Validate category data
        List<CategoryBubbleData> validCategories = new List<CategoryBubbleData>();
        for (int i = 0; i < categoryBubbles.Count; i++)
        {
            if (categoryBubbles[i].category != null)
                validCategories.Add(categoryBubbles[i]);
            else
                Debug.LogWarning($"TreeBubbleManager: CategoryBubbleData[{i}] has null category!");
        }
        
        if (validCategories.Count == 0)
        {
            Debug.LogError("TreeBubbleManager: No valid categories! Ensure BubbleType is assigned.");
            return;
        }
        
        List<Vector3> spawnPositions = GenerateUniformSpawnPositions(bubbleCount);
        
        // Create balanced category distribution (no adjacent repeats)
        List<CategoryBubbleData> categorySequence = GenerateCategorySequence(bubbleCount, validCategories);
        
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            CategoryBubbleData data = categorySequence[i];
            
            TreeBubble bubble = Instantiate(treeBubblePrefab, transform);
            bubble.name = $"TreeBubble_{i}_{data.category.name}";
            
            Debug.Log($"Spawning bubble {i}: Category='{data.category.name}', Icon={data.icon != null}, Text='{data.text}'");
            
            bubble.Initialize(spawnPositions[i], chainLength, swingStrength, data.category, data.icon, data.text);
            
            // Always subscribe to click events (will work when Play mode starts)
            bubble.OnClicked += HandleBubbleClicked;
            
            spawnedBubbles.Add(bubble);
        }
        
        Debug.Log($"<color=green>✓ Spawned {spawnedBubbles.Count}/{bubbleCount} bubbles (Ready for Play mode)</color>");
    }
    
    void DisableBubbleCollisions()
    {
        // Instead of disabling individual collision pairs, we'll use Rigidbody2D settings
        // Set all bubbles to not collide with each other by using collision detection = None
        // This is more reliable than Physics2D.IgnoreCollision
        
        TreeBubble[] allBubbles = GetComponentsInChildren<TreeBubble>();
        
        foreach (TreeBubble bubble in allBubbles)
        {
            Rigidbody2D rb = bubble.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Set collision detection to None - bubbles won't physically interact with each other
                // But OnMouseDown will still work because it uses raycasts, not collision
                rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            }
            
            // Additionally, disable all collision pairs as backup
            Collider2D col = bubble.GetComponent<Collider2D>();
            if (col != null)
            {
                foreach (TreeBubble other in allBubbles)
                {
                    if (other != bubble)
                    {
                        Collider2D otherCol = other.GetComponent<Collider2D>();
                        if (otherCol != null)
                        {
                            Physics2D.IgnoreCollision(col, otherCol, true);
                        }
                    }
                }
            }
        }
        
        Debug.Log($"<color=cyan>Configured {allBubbles.Length} bubbles to ignore each other's collisions</color>");
    }
    
    List<CategoryBubbleData> GenerateCategorySequence(int count, List<CategoryBubbleData> validCategories)
    {
        List<CategoryBubbleData> sequence = new List<CategoryBubbleData>();
        
        // Calculate how many of each category we need
        int categoriesPerType = Mathf.CeilToInt((float)count / validCategories.Count);
        
        // Create pool with balanced distribution
        List<CategoryBubbleData> pool = new List<CategoryBubbleData>();
        for (int i = 0; i < validCategories.Count; i++)
        {
            for (int j = 0; j < categoriesPerType; j++)
            {
                pool.Add(validCategories[i]);
            }
        }
        
        // Shuffle the pool
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = pool[i];
            pool[i] = pool[j];
            pool[j] = temp;
        }
        
        // Pick from pool, avoiding adjacent repeats
        CategoryBubbleData lastPicked = null;
        int poolIndex = 0;
        
        for (int i = 0; i < count; i++)
        {
            CategoryBubbleData picked = null;
            int attempts = 0;
            
            // Try to find a different category than last
            while (attempts < pool.Count)
            {
                int checkIndex = (poolIndex + attempts) % pool.Count;
                if (pool[checkIndex].category != lastPicked?.category)
                {
                    picked = pool[checkIndex];
                    pool.RemoveAt(checkIndex);
                    break;
                }
                attempts++;
            }
            
            // Fallback: just take next available
            if (picked == null && pool.Count > 0)
            {
                picked = pool[0];
                pool.RemoveAt(0);
            }
            
            // If still null, refill pool
            if (picked == null)
            {
                picked = validCategories[Random.Range(0, validCategories.Count)];
            }
            
            sequence.Add(picked);
            lastPicked = picked;
        }
        
        return sequence;
    }
    
    List<Vector3> GenerateUniformSpawnPositions(int count)
    {
        if (spawnShape == SpawnAreaShape.Circle)
        {
            return GenerateCirclePositions(count);
        }
        else
        {
            return GenerateSquarePositions(count);
        }
    }
    
    List<Vector3> GenerateCirclePositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float currentMinDistance = minDistanceBetweenBubbles;
        
        // Use Poisson disk sampling for uniform distribution
        for (int i = 0; i < count; i++)
        {
            Vector3 candidatePos = Vector3.zero;
            bool validPosition = false;
            
            // Try with current minimum distance
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                // Generate random position in circle
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * radius; // sqrt for uniform distribution
                
                candidatePos = transform.position + new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0f);
                
                // Check distance from existing positions
                validPosition = true;
                foreach (Vector3 existingPos in positions)
                {
                    if (Vector3.Distance(candidatePos, existingPos) < currentMinDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }
                
                if (validPosition)
                    break;
            }
            
            // If failed to find position with current min distance, relax the constraint
            if (!validPosition)
            {
                float relaxedDistance = currentMinDistance * 0.7f; // Reduce by 30%
                
                for (int attempt = 0; attempt < maxSpawnAttempts * 2; attempt++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
                    
                    candidatePos = transform.position + new Vector3(
                        Mathf.Cos(angle) * distance,
                        Mathf.Sin(angle) * distance,
                        0f);
                    
                    validPosition = true;
                    foreach (Vector3 existingPos in positions)
                    {
                        if (Vector3.Distance(candidatePos, existingPos) < relaxedDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    if (validPosition)
                        break;
                }
            }
            
            // Last resort: just place it anywhere in the circle
            if (!validPosition)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
                candidatePos = transform.position + new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0f);
            }
            
            positions.Add(candidatePos);
        }
        
        return positions;
    }
    
    List<Vector3> GenerateSquarePositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float currentMinDistance = minDistanceBetweenBubbles;
        
        // Grid-based with minimum distance enforcement
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);
        
        // Calculate cell size based on min distance
        float minCellSize = currentMinDistance * 1.5f; // Add padding
        float actualWidth = Mathf.Max(squareSize.x, cols * minCellSize);
        float actualHeight = Mathf.Max(squareSize.y, rows * minCellSize);
        
        float cellWidth = actualWidth / cols;
        float cellHeight = actualHeight / rows;
        float offsetVariation = Mathf.Min(cellWidth, cellHeight) * 0.15f; // Smaller variation
        
        int spawned = 0;
        for (int row = 0; row < rows && spawned < count; row++)
        {
            for (int col = 0; col < cols && spawned < count; col++)
            {
                Vector3 candidatePos = Vector3.zero;
                bool validPosition = false;
                
                // Try to find valid position with min distance
                for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
                {
                    float x = -actualWidth * 0.5f + cellWidth * (col + 0.5f);
                    float y = actualHeight * 0.5f - cellHeight * (row + 0.5f);
                    
                    // Add random offset
                    x += UnityEngine.Random.Range(-offsetVariation, offsetVariation);
                    y += UnityEngine.Random.Range(-offsetVariation, offsetVariation);
                    
                    candidatePos = transform.position + new Vector3(x, y, 0f);
                    
                    // Check distance from existing positions
                    validPosition = true;
                    foreach (Vector3 existingPos in positions)
                    {
                        if (Vector3.Distance(candidatePos, existingPos) < currentMinDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    if (validPosition)
                        break;
                }
                
                // Fallback: place at grid center without offset
                if (!validPosition)
                {
                    float x = -actualWidth * 0.5f + cellWidth * (col + 0.5f);
                    float y = actualHeight * 0.5f - cellHeight * (row + 0.5f);
                    candidatePos = transform.position + new Vector3(x, y, 0f);
                }
                
                positions.Add(candidatePos);
                spawned++;
            }
        }
        
        return positions;
    }
    
    void HandleBubbleClicked(TreeBubble bubble)
    {
        if (bubble == null)
        {
            Debug.LogWarning("HandleBubbleClicked: Null bubble!");
            return;
        }
        
        if (currentSlotIndex >= slots.Length)
        {
            Debug.LogWarning($"HandleBubbleClicked: No more slots available ({currentSlotIndex}/{slots.Length})");
            return;
        }
        
        if (slottedBubbles.Contains(bubble))
        {
            Debug.LogWarning($"HandleBubbleClicked: Bubble '{bubble.name}' already in slot!");
            return;
        }
        
        Debug.Log($"<color=cyan>Moving bubble '{bubble.name}' (Category: {bubble.Category.name}) to slot {currentSlotIndex}</color>");
        
        MoveToSlot(bubble, currentSlotIndex);
        slottedBubbles.Add(bubble);
        currentSlotIndex++;
        
        // Check for matches AFTER the bubble has moved to the slot
        if (currentSlotIndex >= 4)
        {
            // Wait for movement animation to complete (0.5s) then check
            DOVirtual.DelayedCall(0.6f, () => CheckForMatches());
        }
    }
    
    void MoveToSlot(TreeBubble bubble, int slotIndex)
    {
        if (slotIndex >= slots.Length || slots[slotIndex] == null)
        {
            Debug.LogError($"MoveToSlot: Invalid slot index {slotIndex}");
            return;
        }
        
        bubble.DisablePhysics();
        
        Transform targetSlot = slots[slotIndex];
        bubble.transform.DOMove(targetSlot.position, 0.5f).SetEase(Ease.OutBack);
        bubble.transform.DOScale(Vector3.one * slotBubbleScale, 0.3f);
    }
    
    void CheckForMatches()
    {
        // Check if ANY 4 bubbles in slots have the same category (not necessarily adjacent)
        if (slottedBubbles.Count < 4) return;
        
        // Count occurrences of each category
        Dictionary<BubbleType, List<int>> categoryIndices = new Dictionary<BubbleType, List<int>>();
        
        for (int i = 0; i < slottedBubbles.Count; i++)
        {
            BubbleType cat = slottedBubbles[i].Category;
            if (cat == null)
            {
                Debug.LogWarning($"CheckForMatches: Bubble at index {i} has null category!");
                continue;
            }
            
            if (!categoryIndices.ContainsKey(cat))
            {
                categoryIndices[cat] = new List<int>();
            }
            categoryIndices[cat].Add(i);
        }
        
        // Check if any category has 4 or more bubbles
        foreach (var kvp in categoryIndices)
        {
            if (kvp.Value.Count >= 4)
            {
                Debug.Log($"<color=yellow>✨ MATCH! Found {kvp.Value.Count} bubbles of category '{kvp.Key.name}' - Popping 4!</color>");
                
                // Pop the first 4 of this category
                List<int> indicesToPop = kvp.Value.GetRange(0, 4);
                PopBubbles(indicesToPop);
                return; // Only pop one match at a time
            }
        }
        
        // No match found
        Debug.Log($"No match. Current slots: {GetSlotCategorySummary()}");
    }
    
    string GetSlotCategorySummary()
    {
        if (slottedBubbles.Count == 0) return "Empty";
        
        string result = "";
        for (int i = 0; i < slottedBubbles.Count; i++)
        {
            result += slottedBubbles[i].Category != null ? slottedBubbles[i].Category.name : "null";
            if (i < slottedBubbles.Count - 1) result += ", ";
        }
        return result;
    }
    
    void PopBubbles(List<int> indicesToPop)
    {
        Debug.Log($"<color=magenta>Popping {indicesToPop.Count} bubbles at indices: {string.Join(", ", indicesToPop)}</color>");
        
        List<TreeBubble> bubblesBeingPopped = new List<TreeBubble>();
        
        // Collect bubbles to pop (in reverse order to avoid index shifting during removal)
        indicesToPop.Sort();
        indicesToPop.Reverse();
        
        foreach (int index in indicesToPop)
        {
            if (index >= 0 && index < slottedBubbles.Count)
            {
                bubblesBeingPopped.Add(slottedBubbles[index]);
                slottedBubbles.RemoveAt(index);
            }
        }
        
        // Update current slot index
        currentSlotIndex = slottedBubbles.Count;
        
        // Animate all bubbles popping SIMULTANEOUSLY (no stagger)
        Sequence popSequence = DOTween.Sequence();
        
        for (int i = 0; i < bubblesBeingPopped.Count; i++)
        {
            TreeBubble bubble = bubblesBeingPopped[i];
            if (bubble == null) continue;
            
            // NO DELAY - all pop at the same time
            
            // Scale up animation
            Sequence bubblePopSeq = DOTween.Sequence();
            
            // Quick scale up (anticipation)
            bubblePopSeq.Append(
                bubble.transform.DOScale(bubble.transform.localScale * 1.3f, 0.15f)
                .SetEase(Ease.OutQuad)
            );
            
            // Pop! (scale up big then destroy)
            bubblePopSeq.Append(
                bubble.transform.DOScale(bubble.transform.localScale * 2f, 0.2f)
                .SetEase(Ease.InBack)
            );
            
            // Play particle at peak of animation
            bubblePopSeq.AppendCallback(() =>
            {
                try
                {
                    ParticlePool.PlayRevealFx(bubble.transform.position);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not play particle effect: {e.Message}");
                }
                
                // Destroy bubble
                Destroy(bubble.gameObject);
            });
            
            // Join all animations (they play at the same time)
            popSequence.Join(bubblePopSeq);
        }
        
        // After all bubbles have popped, shift remaining bubbles
        popSequence.AppendCallback(() =>
        {
            Debug.Log($"<color=green>Popped {bubblesBeingPopped.Count} bubbles. {slottedBubbles.Count} remaining in slots.</color>");
            
            // Shift remaining bubbles to fill gaps
            for (int i = 0; i < slottedBubbles.Count; i++)
            {
                slottedBubbles[i].transform.DOMove(slots[i].position, 0.3f).SetEase(Ease.OutQuad);
            }
        });
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        
        if (spawnShape == SpawnAreaShape.Circle)
        {
            DrawCircleGizmo(transform.position, radius);
        }
        else
        {
            DrawSquareGizmo(transform.position, squareSize);
        }
        
        // Draw slots
        if (slots != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform slot in slots)
            {
                if (slot != null)
                    Gizmos.DrawWireSphere(slot.position, 0.2f);
            }
        }
    }
    
    void DrawCircleGizmo(Vector3 center, float rad)
    {
        int segments = 32;
        float angleStep = 360f / segments * Mathf.Deg2Rad;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;
            
            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * rad, Mathf.Sin(angle1) * rad, 0f);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * rad, Mathf.Sin(angle2) * rad, 0f);
            
            Gizmos.DrawLine(p1, p2);
        }
    }
    
    void DrawSquareGizmo(Vector3 center, Vector2 size)
    {
        Vector3 halfSize = new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
        
        Vector3 topLeft = center + new Vector3(-halfSize.x, halfSize.y, 0f);
        Vector3 topRight = center + new Vector3(halfSize.x, halfSize.y, 0f);
        Vector3 bottomLeft = center + new Vector3(-halfSize.x, -halfSize.y, 0f);
        Vector3 bottomRight = center + new Vector3(halfSize.x, -halfSize.y, 0f);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
