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

    public UnityEvent OnSuccessfullMerge;

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
        if (!activeStack) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Check if we clicked on the stack
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;

            //if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == activeStack.gameObject)
            //{
            //}
            isDragging = true;
            draggerVisual.gameObject.SetActive(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            draggerVisual.gameObject.SetActive(false);
            Vector3 position = highlightedBubble.transform.position + (activeStack.transform.position - highlightedBubble.transform.position).normalized * activeStack.Radius * 2;
            ReleaseStack(activeStack, position);
        }

        if (isDragging)
        {
            // Update position
            Vector3 newPosition = activeStack.transform.position;

            //Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo,
            //    Mathf.Infinity, LayerMask.GetMask("Ground"));

            Plane plane = new(Vector3.back, Vector3.zero);
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            plane.Raycast(ray, out float enter);

            Vector3 point = ray.origin + ray.direction * enter;

            newPosition.x = Mathf.Clamp(point.x, leftLimit, rightLimit);
            //newPosition.y = 2;
            //activeStack.transform.position = newPosition;

            Vector3 direction = (new Vector3(point.x, point.y) - activeStack.transform.position).normalized;
            direction.z = 0;
            draggerVisual.up = direction;

            // Get the base
            var bubble = Physics2D.Raycast(activeStack.transform.position + direction, direction);

            if (bubble.collider && bubble.collider.TryGetComponent(out Bubble b))
            {
                if (b != highlightedBubble)
                {
                    Highlight(b);
                }
            }
            else
            {
                Highlight(null);
            }
            //// Find the last base with null hexaBundleStack
            //targetBase = null;
            //int yIndex = -1;
            //for (int i = hexaBases.Length - 1; i >= 0; i--)
            //{
            //    var hBase = hexaBases[i].collider.GetComponent<HexaBase>();
            //    if (hBase != null && hBase.Bundle == null && hBase.GridPosition.y > yIndex)
            //    {
            //        targetBase = hBase;
            //        yIndex = hBase.GridPosition.y;
            //    }
            //}

            //// Update highlighting only if we found a valid target base
            //if (targetBase != null)
            //{
            //    if (previousHighlightedBase != null && targetBase.gameObject != previousHighlightedBase.transform.parent)
            //    {
            //        previousHighlightedBase.highlighted = false;
            //    }

            //    var baseHighlight = targetBase.GetComponentInChildren<HighlightEffect>();
            //    baseHighlight.highlighted = true;
            //    previousHighlightedBase = baseHighlight;

            //    //Debug.Log("Base highlighted: " + baseHighlight.gameObject.name);
            //}
            //else if (previousHighlightedBase != null)
            //{
            //    previousHighlightedBase.highlighted = false;
            //    previousHighlightedBase = null;
            //}
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
    private void ReleaseStack(Bubble activeStack, Vector3 position)
    {
        if (activeStack == null)
            return;

        Bubble targetBubble = highlightedBubble;
        var hexGrid = CategoryManager.Instance?.HexGrid;

        if (hexGrid == null || targetBubble == null)
            return;

        activeStack.transform
            .DOMove(position, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Target might have been destroyed while animating
                if (targetBubble == null || activeStack == null)
                    return;

                if (activeStack.Category != targetBubble.Category)
                    return;
                Vector2Int rootPos = hexGrid.GetGridPosition(targetBubble.transform.position);

                List<Bubble> connected =
                    GetConnectedBubblesFrom(rootPos, targetBubble.Category);

                // Find all connected bubbles of same category
                connected.Insert(0, activeStack);

                // Need at least 2 to merge
                if (connected.Count > 1)
                {
                    MergeBubbles(connected);
                }
            });

        highlightedBubble = null;
        this.activeStack = null;
        isDragging = false;
        SpawnNewBubble();
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
        // Remove destroyed/null bubbles
        bubbles.RemoveAll(b => b == null);

        if (bubbles.Count <= 1)
            yield break;

        // First bubble stays in place
        Bubble current = bubbles[1];
        TryMerge(bubbles[0], bubbles[1], out current);

        current.transform.DOKill();

        for (int i = 2; i < bubbles.Count; i++)
        {
            Bubble next = bubbles[i];

            if (current == null || next == null)
                continue;

            next.transform.DOKill();

            // Move CURRENT merged bubble towards NEXT bubble
            Tween moveTween = current.transform
                .DOMove(next.transform.position, mergeMoveDuration)
                .SetEase(mergeEase);

            yield return moveTween.WaitForCompletion();

            // Merge when reached
            TryMerge(current, next, out Bubble mergedResult);

            // Use merged bubble if created
            if (mergedResult != null)
            {
                current = mergedResult;
            }

            // Small delay between merges
            yield return new WaitForSeconds(mergeInterval);
        }
    }


    public void SpawnNewBubble()
    {
        activeStack = CategoryManager.Instance.SpawnNewBubble(spawnPosition.position);
    }

}