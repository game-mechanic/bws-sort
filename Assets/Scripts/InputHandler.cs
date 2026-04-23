using DG.Tweening;
using System;
using UnityEngine;

public class InputHandler : Singleton<InputHandler>
{
    Bubble draggable;
    Bubble highlightedBubble;
    Camera mainCamera;
    bool isDragging;
    Vector3 startScale;
    Vector3 offset;
    private void Start()
    {
        draggable = null;
        isDragging = false;
        highlightedBubble = null;
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
            startScale = draggable.transform.localScale;
            offset = new Vector2(draggable.transform.position.x, draggable.transform.position.y) - hit.point;
            draggable.StartDrag();
            draggable.Bounce(GameSettings.Instance.MaxBounceAmplitude, GameSettings.Instance.BounceTime);
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

            draggable.transform.position = Vector3.Lerp(draggable.transform.position, hitPoint, GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);

            if (GetOverlap(hitPoint, draggable.Radius, out Collider2D hit))
            {
                if (hit.TryGetComponent(out Bubble d))
                {
                    if (d != highlightedBubble && d != draggable)
                        Highlight(d);
                }
                else
                    Highlight(null);
            }
            else
            {
                Highlight(null);
            }
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
        if (highlightedBubble == null || !TryMerge(draggable, highlightedBubble))
        {
            Plane plane = new(Vector3.back, new Vector3(0, 0, 0));
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            plane.Raycast(ray, out float enter);

            Vector3 hitPoint = ray.origin + ray.direction * enter * 0.9f;
            RaycastHit2D hit = Physics2D.Raycast(hitPoint, Vector3.forward);
            if (hit.transform.CompareTag("MergePlace") && draggable.Category == CategoryManager.Instance.CurrentType)
            {
                draggable.EndDrag(false);
                draggable.transform.DOMove(hit.transform.position, 0.3f);
                SplineObjectPlacer.instance.RemovePlacedStack(draggable.Container);
                CategoryManager.Instance.HideText();
                draggable = null;
            }
            else
            {
                draggable.EndDrag(true);
            }

            Highlight(null);
        }
    }

    public bool TryMerge(Bubble a, Bubble b)
    {
        if (a.Category != b.Category) return false;

        byte maxIndex = Math.Max(a.Index, b.Index);
        int nextIndex = a.Index + b.Index + 1;
        var bigBubble = a.Index == maxIndex ? a : b;
        var newBubble = Instantiate(GameSettings.Instance.Bubbles[nextIndex]);
        newBubble.transform.SetPositionAndRotation(b.transform.position, bigBubble.transform.rotation);
        var names = a.Names;
        names.AddRange(b.Names);
        newBubble.Category = a.Category;
        newBubble.SetName(names);
        newBubble.Bounce();
        a.transform.DOKill();
        b.transform.DOKill();
        Destroy(a.gameObject);
        Destroy(b.gameObject);
        Destroy(draggable.Container.gameObject);
        SplineObjectPlacer.instance.RemovePlacedStack(draggable.Container);
        if (CategoryManager.Instance.ReduceCount(a.Category) <= 0)
        {
            newBubble.Blast();
        }
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
}