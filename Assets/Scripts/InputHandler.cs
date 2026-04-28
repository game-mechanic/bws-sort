using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : Singleton<InputHandler>
{
    Bubble draggable;
    Bubble highlightedBubble;
    Camera mainCamera;
    bool isDragging;
    Vector3 offset;
    LineRenderer lineRenderer;
    List<Bubble> hoveredBubbles = new List<Bubble>();  // Track hovered bubbles

    private void Start()
    {
        mainCamera = Camera.main;
        ParticlePool.Init();
    }
    private void Update()
    {
        if (!isDragging
            && Input.GetMouseButtonDown(0)
            && TryRaycast2D(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit2D hit)
            && hit.collider.TryGetComponent(out Bubble d))
        {
            draggable = d;
            isDragging = true;
            offset = new Vector2(draggable.transform.position.x, draggable.transform.position.y) - hit.point;
            draggable.StartDrag();
            draggable.Bounce(GameSettings.Instance.MaxBounceAmplitude, GameSettings.Instance.BounceTime);
            lineRenderer = Instantiate(GameSettings.Instance.LineRendererPrefab);
            lineRenderer.gameObject.SetActive(false);
            hoveredBubbles.Clear();
            hoveredBubbles.Add(draggable);  // Start with the dragged bubble

            UpdateLineRenderer(draggable.transform.position);
            lineRenderer.gameObject.SetActive(true);
        }

        if (Input.GetMouseButtonUp(0) && draggable != null)
        {
            ReleaseDrag();
        }
    }
    public bool TryRaycast2D(Ray ray, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(ray.origin, ray.direction, 100);
        return hit.collider != null;
    }
    private void FixedUpdate()
    {
        if (draggable != null && isDragging)
        {
            Plane plane = new(Vector3.back, new Vector3(0, 0, 0));
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            plane.Raycast(ray, out float enter);

            Vector3 hitPoint = ray.origin + ray.direction * enter;
            hitPoint += offset;

            // Check if mouse moved backwards - continuously remove bubbles while moving back
            while (hoveredBubbles.Count >= 2)
            {
                Vector3 secondLastPos = hoveredBubbles[hoveredBubbles.Count - 2].transform.position;
                Vector3 lastPos = hoveredBubbles[hoveredBubbles.Count - 1].transform.position;
                Vector3 direction = (lastPos - secondLastPos).normalized;
                Vector3 toMouse = (hitPoint - lastPos).normalized;

                // If dot product is negative, mouse moved backwards
                if (Vector3.Dot(direction, toMouse) < -0.98f)
                {
                    if (hoveredBubbles.Count > 0)
                    {
                        hoveredBubbles[^1].Highlight(false);
                        hoveredBubbles.RemoveAt(hoveredBubbles.Count - 1);
                        RefreshHighlights();   // update visuals once
                    }
                }
                else
                {
                    break;  // Stop if mouse is no longer moving backwards
                }
            }

            // Update the line renderer with current positions
            UpdateLineRenderer(hitPoint);

            //draggable.transform.position = Vector3.Lerp(draggable.transform.position, hitPoint, GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);

            if (TryRaycast2D(ray, out var hit))
            {
                if (hit.collider.TryGetComponent(out Bubble d))
                {
                    // Only highlight if not already in the hovered list and not the dragged bubble
                    if (!hoveredBubbles.Contains(d) && d != draggable)
                    {
                        TryAddBubble(d);
                        Highlight(d);
                    }
                }
                else if (highlightedBubble != null)
                    Highlight(null);
            }
            else if (highlightedBubble != null)
            {
                Highlight(null);
            }
        }
    }
    void TryAddBubble(Bubble b)
    {
        if (b == null || hoveredBubbles.Contains(b) || b == draggable)
            return;


        hoveredBubbles.Add(b);

        RefreshHighlights();   // update visuals once
    }

    private void UpdateLineRenderer(Vector3 currentMousePosition = default)
    {
        int positionCount = hoveredBubbles.Count + 1;  // +1 for current mouse position
        lineRenderer.positionCount = positionCount;

        for (int i = 0; i < hoveredBubbles.Count; i++)
        {
            lineRenderer.SetPosition(i, hoveredBubbles[i].transform.position);
        }
        // Last position is the current mouse position (or last bubble if not dragging)
        lineRenderer.SetPosition(positionCount - 1, currentMousePosition);

    }
    public void PerformClickEffect(Transform cube)
    {
        cube.DOKill();
        cube.DOShakePosition(0.2f, strength: new Vector3(0.051f, 0.051f, 0), vibrato: 10);
    }
    void RefreshHighlights()
    {
        // First clear all
        foreach (var bubble in hoveredBubbles)
        {
            bubble.Highlight(false);
        }

        // Then apply highlight
        foreach (var bubble in hoveredBubbles)
        {
            bubble.Highlight(true);
        }
    }
    Collider2D[] results = new Collider2D[10];
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
                if (b.Category == draggable.Category && b != draggable)
                {
                    hit = results[i];
                    return true;
                }
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
        draggable.EndDrag();
        Highlight(null);

        if (lineRenderer != null)
            Destroy(lineRenderer.gameObject);

        foreach (var item in hoveredBubbles)
        {
            item.Highlight(false);
        }
        if (hoveredBubbles.Count < 4)
        {
            WrondClick(hoveredBubbles);

            hoveredBubbles.Clear();  // Clear bubbles when releasing
            return;
        }

        BubbleType bubbleType = hoveredBubbles[0].Category;

        Vector3[] positions = new Vector3[hoveredBubbles.Count];
        positions[0] = hoveredBubbles[0].transform.position;

        for (int i = 1; i < hoveredBubbles.Count; i++)
        {
            if (hoveredBubbles[i].Category != bubbleType)
            {
                WrondClick(hoveredBubbles);
                hoveredBubbles.Clear();
                return; // If any bubble is of a different category, do not merge
            }
            positions[i] = hoveredBubbles[i].transform.position;
        }

        CategoryManager.Instance.MergeBubbles(hoveredBubbles, positions);

        hoveredBubbles.Clear();  // Clear bubbles when releasing
    }
    void WrondClick(List<Bubble> bubbles)
    {
        foreach (var bubble in bubbles)
        {
            PerformClickEffect(bubble.transform);
        }
    }

    public bool TryMerge(Bubble a, Bubble b, out Bubble bubble)
    {
        if (a.Category != b.Category)
        {
            bubble = null;
            return false;
        }

        byte maxIndex = Math.Max(a.Index, b.Index);
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
            newBubble.Blast();
        }
        bubble = newBubble;
        return true;
    }

    void Highlight(Bubble newBubble)
    {
        if (highlightedBubble == newBubble) return;

        if (highlightedBubble != null && !hoveredBubbles.Contains(highlightedBubble))
            highlightedBubble.Highlight(false);

        highlightedBubble = newBubble;

        if (highlightedBubble != null)
        {
            highlightedBubble.Highlight(true);
            highlightedBubble.Bounce(0.2f, 0.5f);
        }
    }
}