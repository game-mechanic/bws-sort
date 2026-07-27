using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : Singleton<InputHandler>
{
    Bubble draggable;
    Bubble highlightedBubble;
    Camera mainCamera;
    bool isDragging;
    Vector3 startScale;
    Vector3 offset;
    private Vector3 screenCenter;

    [SerializeField] Transform spawnPosition;
    [SerializeField] Dragger dragger;
    RaycastHit2D hit;
    public UnityEvent OnSuccessfullMerge;

    private List<Bubble> dockedBubbles = new List<Bubble>();
    private Dictionary<Bubble, Vector3> bubbleSpawnPoints = new Dictionary<Bubble, Vector3>();

    private void Start()
    {
        mainCamera = Camera.main;
        draggerVisual.transform.position = spawnPosition.position;
        SpawnNewBubble();
        ParticlePool.Init();
        screenCenter = mainCamera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height) * 0.5f);
        screenCenter.z = 0;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Plane plane = new(Vector3.back, Vector3.zero);
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.origin + ray.direction * enter;
                
                float closestDist = float.MaxValue;
                Bubble closestBubble = null;
                
                foreach (var b in dockedBubbles)
                {
                    if (b == null) continue;
                    float dist = Vector3.Distance(point, b.transform.position);
                    if (dist < b.Radius * 3f && dist < closestDist)
                    {
                        closestDist = dist;
                        closestBubble = b;
                    }
                }
                
                if (closestBubble != null)
                {
                    activeStack = closestBubble;
                    isDragging = true;
                    if (draggerVisual != null) draggerVisual.gameObject.SetActive(false);
                    offset = activeStack.transform.position - point;
                    activeStack.transform.DOKill();
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && activeStack != null)
            {
                isDragging = false;
                DropBubble();
            }
        }

        if (isDragging && activeStack != null)
        {
            Plane plane = new(Vector3.back, Vector3.zero);
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.origin + ray.direction * enter;
                activeStack.transform.position = point + offset;
            }
        }
    }



    public bool TryRaycast2D(Ray ray, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(ray.origin, ray.direction, 100);
        return hit.collider != null;
    }
    //private void FixedUpdate()
    //{
    //    if (draggable != null && isDragging)
    //    {
    //        Plane plane = new(Vector3.back, new Vector3(0, 0, 0));
    //        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

    //        plane.Raycast(ray, out float enter);

    //        Vector3 hitPoint = ray.origin + ray.direction * enter;
    //        hitPoint += offset;

    //        draggable.transform.position = Vector3.Lerp(draggable.transform.position, hitPoint, GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);

    //        if (GetOverlap(hitPoint, draggable.Radius, out Collider2D hit))
    //        {
    //            if (hit.TryGetComponent(out Bubble d))
    //            {
    //                if (d != highlightedBubble && d != draggable)
    //                    Highlight(d);
    //            }
    //            else
    //                Highlight(null);
    //        }
    //        else
    //        {
    //            Highlight(null);
    //        }
    //    }
    //}
    Collider2D[] results = new Collider2D[10];
    [SerializeField] private Transform draggerVisual;
    private float leftLimit = -2.5f;
    private float rightLimit = 2.5f;
    private Bubble activeStack;

    private bool GetOverlap(Vector3 center, float radius, out Collider2D hit)
    {
        int count = Physics2D.OverlapCircle(center, radius, new ContactFilter2D() { layerMask = ~0 }, results);
        Collider2D overlappingBubble = null;
        float closest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (results[i].TryGetComponent(out Bubble b))
            {
                if (overlappingBubble == null)
                {
                    overlappingBubble = results[i];
                }
                if ((b.transform.position - draggable.transform.position).sqrMagnitude < closest)
                {
                    closest = (b.transform.position - draggable.transform.position).sqrMagnitude;
                    overlappingBubble = results[i];
                }
                //if (b.Category == draggable.Category && b != draggable)
                //{
                //    hit = results[i];
                //    return true;
                //}
            }
        }
        hit = overlappingBubble;
        return overlappingBubble != null;
    }

    public void ReleaseDrag()
    {
        if (draggable == null)
        {
            isDragging = false;
            return;
        }
        isDragging = false;
        //draggable.EndDrag();
        if (highlightedBubble == null)
        {
            Highlight(null);
        }
        else if (!TryMerge(draggable, highlightedBubble))
        {
            draggable.ReturnBack();
            Highlight(null);
        }
        else
        {
            draggable.EndDrag();
            draggable.BlastGhost();
        }

        //if (!TryMerge(draggable, highlightedBubble))
        //{
        //    if (highlightedBubble == null)
        //    {
        //        draggable.ReturnBack();
        //    }
        //    else
        //    {
        //        draggable.ReturnBack();
        //        Highlight(null);
        //    }
        //}
        //else
        //{
        //    draggable.EndDrag();
        //    draggable.BlastGhost();
        //}
        //Highlight(null);
    }

    public bool TryMerge(Bubble a, Bubble b)
    {
        return TryMerge(a, b, out _);
    }
    public bool TryMerge(Bubble a, Bubble b, out Bubble bubble)
    {
        if (a.Category != b.Category)
        {
            bubble = null;
            return false;
        }

        int maxIndex = Mathf.Max(a.Index, b.Index);
        int nextIndex = a.Index + b.Index + 1;
        var bigBubble = a.Index == maxIndex ? a : b;
        var newBubble = Instantiate(GameSettings.Instance.Bubbles[nextIndex]);
        newBubble.transform.SetPositionAndRotation(bigBubble.transform.position, bigBubble.transform.rotation);
        var names = a.Names;
        names.AddRange(b.Names);
        newBubble.Category = a.Category;
        newBubble.SetName(names);
        newBubble.Bounce();
        a.transform.DOKill();
        b.transform.DOKill();
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (CategoryManager.Instance.ReduceCount(a.Category) <= 0)
        {
            newBubble.Blast(/*()=> OnSuccessfullMerge?.Invoke()*/);
        }
        bubble = newBubble;
        return true;
    }

    void Highlight(Bubble newBubble)
    {
        if (highlightedBubble != null)
        {
            highlightedBubble.Highlight(false);
        }
        highlightedBubble = newBubble;
        if (highlightedBubble != null)
        {
            highlightedBubble.Highlight(true);
            highlightedBubble.Bounce(0.2f, 0.5f);
        }
    }
    private void DropBubble()
    {
        if (activeStack == null) return;

        var hexGrid = CategoryManager.Instance?.HexGrid;
        if (hexGrid == null)
        {
            ReturnActiveStackToSpawn();
            return;
        }

        Vector2Int gridPos = hexGrid.GetGridPosition(activeStack.transform.position);

        // 1. Check if the cell is already occupied
        if (hexGrid.TryGetGridObject(gridPos, out Bubble occupied) && occupied != null)
        {
            ReturnActiveStackToSpawn();
            return;
        }

        // 2. Check if it's adjacent to any existing bubble
        bool hasNeighbor = false;
        foreach (var nPos in hexGrid.GetNeighbors(gridPos))
        {
            if (hexGrid.TryGetGridObject(nPos, out Bubble nb) && nb != null)
            {
                hasNeighbor = true;
                break;
            }
        }

        if (!hasNeighbor)
        {
            ReturnActiveStackToSpawn();
            return;
        }

        // Drop is valid
        Vector3 snapPos = hexGrid.GetWorldPosition(gridPos);
        hexGrid.AddGridObject(snapPos, activeStack);
        
        dockedBubbles.Remove(activeStack);
        bubbleSpawnPoints.Remove(activeStack);
        
        Bubble droppedBubble = activeStack;
        activeStack = null;
        
        droppedBubble.transform.DOMove(snapPos, 0.1f).SetLink(droppedBubble.gameObject).OnComplete(() => 
        {
            if (droppedBubble == null) return;
            droppedBubble.Bounce();

            List<Bubble> connected = GetConnectedBubblesFrom(gridPos, droppedBubble.Category);
            
            if (connected.Count >= 4)
            {
                MergeBubbles(connected);
            }
            
            SpawnNewBubble();
        });
    }

    private void ReturnActiveStackToSpawn()
    {
        if (activeStack == null) return;
        
        Vector3 returnPos = spawnPosition.position;
        if (bubbleSpawnPoints.TryGetValue(activeStack, out Vector3 point))
        {
            returnPos = point;
        }
        
        activeStack.transform.DOMove(returnPos, 0.3f).SetEase(Ease.OutBack).SetLink(activeStack.gameObject);
        activeStack = null;
    }
    private void CascadeBounceEffect(Bubble originBubble)
    {
        if (originBubble == null)
            return;

        var hexGrid = CategoryManager.Instance?.HexGrid;

        if (hexGrid == null)
            return;

        float maxAmplitude = GameSettings.Instance.MaxBounceAmplitude;

        Vector2Int originPos = hexGrid.GetGridPosition(originBubble.transform.position);

        HashSet<Bubble> visited = new HashSet<Bubble>();

        // Start recursive propagation
        PropagateBounce(originPos, 0, maxAmplitude, visited);
    }
    int maxDepth = 5;
    private void PropagateBounce(
        Vector2Int gridPos,
        int depth,
        float amplitude,
        HashSet<Bubble> visited)
    {
        if (depth == maxDepth) return;

        var hexGrid = CategoryManager.Instance?.HexGrid;

        if (hexGrid == null)
            return;

        Bubble currentBubble = hexGrid.GetGridObject(gridPos) as Bubble;

        if (currentBubble == null)
            return;

        if (visited.Contains(currentBubble))
            return;

        visited.Add(currentBubble);

        // Apply bounce with current amplitude
        currentBubble.Bounce(amplitude);

        // Fade amplitude as depth increases
        float nextAmplitude = Mathf.Lerp(GameSettings.Instance.MaxBounceAmplitude, 0, ((float)depth) / maxDepth);
        // Get neighbours and continue recursion
        List<Vector2Int> neighbours = hexGrid.GetNeighbors(gridPos);

        foreach (var neighbourPos in neighbours)
        {
            PropagateBounce(
                neighbourPos,
                depth + 1,
                nextAmplitude,
                visited);
        }
    }

    /// <summary>
    /// Returns all connected bubbles of the same category using DFS.
    /// </summary>
    private List<Bubble> GetConnectedBubblesFrom(
        Vector2Int startPos,
        BubbleType category)
    {
        var result = new List<Bubble>();

        var hexGrid = CategoryManager.Instance?.HexGrid;
        if (hexGrid == null || !hexGrid.IsInBounds(startPos))
            return result;

        HashSet<Vector2Int> visited = new();
        Stack<Vector2Int> stack = new();

        stack.Push(startPos);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();

            if (!visited.Add(current))
                continue;

            Bubble bubble = hexGrid.GetGridObject(current);

            if (bubble == null || bubble.Category != category)
                continue;

            result.Add(bubble);

            foreach (Vector2Int neighbor in hexGrid.GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }

        return result;
    }
    [SerializeField] private float mergeMoveDuration = 0.45f;
    [SerializeField] private float mergeInterval = 0.12f;
    [SerializeField] private float mergeStartDelay = 0.5f;
    [SerializeField] private Ease mergeEase = Ease.InOutSine;
    #region BullSHit
    [Tooltip("How many seconds before the last bubble finishes converging to spawn bubbleOnPointMerge. Clamped to [0, bubbleMergeMoveDuration].")]
    [SerializeField] private float mergeSpawnEarlyOffset = 0.15f;
    [SerializeField] private float shootDuration;
    [SerializeField] private Ease shootEase;
    [SerializeField] private float staggerInterval = 0.1f;

    #endregion
    public void MergeBubbles(List<Bubble> bubbles)
    {
        if (bubbles == null || bubbles.Count <= 1)
            return;

        StartCoroutine(MergeSequentially(bubbles, screenCenter));
    }
    //IEnumerator MergeOneByOne(List<Bubble> hoveredBubbles, Vector3 position)
    //{
    //    const float Duration = 0.5f;
    //    const float Interval = 0.2f;

    //    //if (hoveredBubbles.Count < 2) yield break;

    //    yield return new WaitForSeconds(1.1f);

    //    // Animate all bubbles moving to center position with staggered delays
    //    for (int i = 0; i < hoveredBubbles.Count; i++)
    //    {
    //        hoveredBubbles[i].transform.DOMove(position, Duration)
    //            .SetDelay(i * Interval)
    //            .SetEase(Ease.InSine);
    //    }

    //    yield return new WaitForSeconds(Duration + (hoveredBubbles.Count - 1) * Interval);

    //    // Merge bubbles sequentially, starting with the first two
    //    Bubble current = hoveredBubbles[0];
    //    for (int i = 1; i < hoveredBubbles.Count; i++)
    //    {
    //        TryMerge(current, hoveredBubbles[i], out current);

    //        if (i < hoveredBubbles.Count - 1)
    //        {
    //            yield return new WaitForSeconds(Interval);
    //        }
    //    }
    //}
    //void MergeBubbles(List<Bubble> bubbles)
    //{
    //    for (int i = 0; i < bubbles.Count; i++)
    //    {
    //        Bubble cube = bubbles[i];
    //        float outDelay = i * bubbleStaggerDelay;

    //        // Disable collider — Bubble owns its own visual from here.
    //        Collider col = cube.GetComponent<Collider>();
    //        if (col != null) col.enabled = false;

    //        Bubble bubble = cube;

    //        if (bubble != null)
    //        {
    //            // Freeze distance-based text toggling for the whole flight.
    //            bubble.SetScattering(true);

    //            // Detach from HexaCube hierarchy.
    //            bubble.transform.SetParent(null);

    //            Transform bubbleTransform = bubble.transform;

    //            // Random point on sphere surface around pointOfMerge.
    //            Vector3 scatterPos = GetNonOverlappingScatterPosition(
    //                origin,
    //                bubbleScatterRadius,
    //                bubbleMinDistance,
    //                usedScatterPositions
    //            );

    //            usedScatterPositions.Add(scatterPos);

    //            // ── Phase 1: fly OUT to scatter position (staggered) ──────────
    //            bubbleTransform
    //                .DOMove(scatterPos, bubbleMoveDuration)
    //                .SetDelay(outDelay)
    //                .SetEase(Ease.OutCubic)
    //                .OnComplete(() =>
    //                {
    //                    // ── Phase 2: fly IN to pointOfMerge while scaling down to zero ──
    //                    // DOMove and DOScale run in parallel — bubble shrinks as it converges.
    //                    bubbleTransform
    //                        .DOMove(origin, bubbleMergeMoveDuration)
    //                        .SetEase(Ease.InCubic);

    //                    bubbleTransform
    //                        .DOScale(Vector3.zero, bubbleMergeMoveDuration)
    //                        .SetEase(Ease.InCubic)
    //                        .OnComplete(() =>
    //                        {
    //                            TryMerge
    //                        });
    //                });
    //        }
    //    }

    private IEnumerator MergeSequentially(List<Bubble> bubbles, Vector3 mergePosition)
    {
        yield return new WaitForSeconds(mergeStartDelay);


        foreach (var bubble in bubbles)
        {
            bubble.Bounce();
            bubble.Highlight(true);
            yield return new WaitForSeconds(staggerInterval);
        }


        yield return new WaitForSeconds(.3f);



        // Remove destroyed/null bubbles
        bubbles.RemoveAll(b => b == null);

        if (bubbles.Count <= 1)
            yield break;

        // First bubble stays in place
        Bubble current = bubbles[1];

        var hexGrid = CategoryManager.Instance.HexGrid;
        var rGP = hexGrid.GetGridPosition(bubbles[1].transform.position);
        hexGrid.RemoveGridObject(rGP.x, rGP.y);

        TryMerge(bubbles[0], bubbles[1], out current);
        current.SortingOrder = 100;
        current.transform.DOKill();
        Dictionary<Bubble, Vector3> originalPositions = new();
        for (int i = 2; i < bubbles.Count; i++)
        {
            Bubble next = bubbles[i];

            if (current == null || next == null)
                continue;

            next.transform.DOKill();

            // Move CURRENT merged bubble towards NEXT bubble
            Tween moveTween = current.transform
                .DOMove(next.transform.position, mergeMoveDuration)
                .SetEase(mergeEase)
                .SetLink(current.gameObject);

            foreach (var b in hexGrid.GetNeighbors(hexGrid.GetGridPosition(next.transform.position)))
            {
                if (hexGrid.TryGetGridObject(b, out Bubble n) && n != null)
                {
                    if (!originalPositions.ContainsKey(n))
                    {
                        originalPositions.Add(n, n.transform.position);
                    }
                    Vector3 direction = (n.transform.position - next.transform.position).normalized;
                    n.transform.DOKill();
                    n.transform.DOMove(n.transform.position + direction * 0.3f, mergeMoveDuration)
                    .SetEase(mergeEase).SetTarget(n.transform).SetLink(n.gameObject);
                }
            }

            yield return moveTween.WaitForCompletion();

            rGP = hexGrid.GetGridPosition(current.transform.position);
            hexGrid.RemoveGridObject(rGP.x, rGP.y);

            // Merge when reached
            TryMerge(current, next, out Bubble mergedResult);
            mergedResult.SortingOrder = 100;

            // Use merged bubble if created
            if (mergedResult != null)
            {
                current = mergedResult;
            }

            // Small delay between merges
            yield return new WaitForSeconds(mergeInterval);
        }
        yield return new WaitForSeconds(2);

        foreach (var item in originalPositions)
        {
            if (item.Key != null && item.Key != null)
            {
                if (HasNeighbour(item.Key))
                {
                    item.Key.transform.DOKill();
                    item.Key.transform.DOMove(item.Value, mergeMoveDuration).SetEase(Ease.OutBack).SetLink(item.Key.gameObject);
                }
                else
                {
                    item.Key.Bounce();
                    item.Key.OnBounce.AddListener(() =>
                    {
                        ParticlePool.PlayRevealFx(item.Key.transform.position);
                        Destroy(item.Key.gameObject);
                    });
                }
            }
        }
    }

    bool HasNeighbour(Bubble b)
    {
        var grid = CategoryManager.Instance.HexGrid;
        Vector2Int gridPos = grid.GetGridPosition(b.transform.position);
        foreach (var neibour in grid.GetNeighbors(gridPos))
        {
            if (grid.TryGetGridObject(neibour, out _))
            {
                return true;
            }
        }
        return false;
    }


    public void SpawnNewBubble()
    {
        // Only spawn when all bubbles have been placed
        if (dockedBubbles.Count > 0) return;

        List<Transform> points = new List<Transform>();
        if (spawnPosition.childCount > 0)
        {
            foreach (Transform child in spawnPosition)
            {
                points.Add(child);
            }
        }
        else
        {
            points.Add(spawnPosition);
        }

        foreach (var point in points)
        {
            Bubble newBubble = CategoryManager.Instance.SpawnNewBubble(point.position);
            if (newBubble != null)
            {
                newBubble.transform.DOScale(1, 0.5f).From(0).SetEase(Ease.OutBack).SetLink(newBubble.gameObject);
                dockedBubbles.Add(newBubble);
                bubbleSpawnPoints[newBubble] = point.position;
            }
            else
            {
                // Break out if no more bubbles available
                break;
            }
        }
    }

}