using DG.Tweening;
using UnityEngine;

/// <summary>
/// Handles drag-and-drop input.
/// The player drags a single bubble and drops it onto the CenterCircle.
/// If the bubble's category matches the current center category it is accepted;
/// otherwise it snaps back and a wrong-click shake plays.
/// </summary>
public class InputHandler : Singleton<InputHandler>
{
    // ── state ──────────────────────────────────────────────────────────────
    Bubble draggable;
    Camera mainCamera;
    bool isDragging;
    Vector3 dragOffset;
    Vector3 originPosition;     // world position the bubble started at (for snap-back)
    bool isOverCenter;          // true while draggable is hovering over CenterCircle

    // ── Unity messages ─────────────────────────────────────────────────────

    private void Start()
    {
        mainCamera = Camera.main;
        ParticlePool.Init();
    }

    private void Update()
    {
        HandlePickUp();
        HandleRelease();
    }

    private void FixedUpdate()
    {
        HandleDrag();
    }

    // ── input phases ───────────────────────────────────────────────────────

    void HandlePickUp()
    {
        if (isDragging) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!TryRaycast2D(ray, out RaycastHit2D hit)) return;
        if (!hit.collider.TryGetComponent(out Bubble bubble)) return;

        draggable    = bubble;
        isDragging   = true;
        originPosition = draggable.transform.position;
        dragOffset   = (Vector2)draggable.transform.position - hit.point;

        draggable.StartDrag();
        draggable.Bounce(GameSettings.Instance.MaxBounceAmplitude, GameSettings.Instance.BounceTime);
        isOverCenter = false;
    }

    void HandleDrag()
    {
        if (draggable == null || !isDragging) return;

        // Project mouse onto the world XY plane (z = 0)
        Plane plane = new Plane(Vector3.back, Vector3.zero);
        Ray ray     = mainCamera.ScreenPointToRay(Input.mousePosition);
        plane.Raycast(ray, out float enter);
        Vector3 targetPos = ray.origin + ray.direction * enter + dragOffset;

        draggable.transform.position = Vector3.Lerp(
            draggable.transform.position,
            targetPos,
            GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);

        // Check hover over center circle
        if (CenterCircle.Instance != null)
        {
            bool wasOver  = isOverCenter;
            isOverCenter  = CenterCircle.Instance.IsInsideDropZone(targetPos);
            bool isMatch  = draggable.Category == GetCurrentCategory();

            if (isOverCenter != wasOver)
                CenterCircle.Instance.SetHoverHighlight(isOverCenter, isMatch);
        }
    }

    void HandleRelease()
    {
        if (!Input.GetMouseButtonUp(0) || draggable == null) return;

        isDragging = false;

        // NOTE: We do NOT call EndDrag() here for snap-backs because EndDrag
        // restores Dynamic physics which would fight the tween. Instead we call
        // it ourselves after the tween completes (or immediately for accepted drops).

        // Try to drop onto center circle
        if (CenterCircle.Instance != null && isOverCenter)
        {
            bool accepted = CenterCircle.Instance.TryDrop(draggable, originPosition);
            if (accepted)
            {
                // CenterCircle owns the bubble now — just clear highlight
                draggable.EndDrag();
            }
            else
            {
                // Wrong category — snap back (EndDrag called inside SnapBack on complete)
                SnapBack(draggable);
            }
        }
        else
        {
            // Released outside — snap back
            SnapBack(draggable);
        }

        CenterCircle.Instance?.SetHoverHighlight(false);
        isOverCenter = false;
        draggable    = null;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    /// <summary>Animate the bubble back to where it was picked up, then restore physics.</summary>
    void SnapBack(Bubble bubble)
    {
        if (bubble == null) return;

        // Keep kinematic during the tween so physics doesn't fight it.
        // DOMove and DOShakePosition both run on transform — run them sequentially
        // so the shake doesn't kill the move tween.
        bubble.transform.DOKill();

        Sequence snapSeq = DOTween.Sequence();
        // First snap home
        snapSeq.Append(bubble.transform.DOMove(originPosition, 0.35f).SetEase(Ease.OutBack));
        // Then shake in-place for wrong-drop feedback
        snapSeq.Append(bubble.transform.DOShakePosition(0.2f,
            strength: new Vector3(0.051f, 0.051f, 0), vibrato: 10));
        snapSeq.OnComplete(() =>
        {
            // Restore physics + clear highlight once fully landed
            bubble.EndDrag();
            bubble.Bounce(GameSettings.Instance.MaxBounceAmplitude,
                          GameSettings.Instance.BounceTime);
        });
    }

    /// <summary>Get the category currently shown in the center circle.</summary>
    static BubbleType GetCurrentCategory()
    {
        // Reached through the CenterCircle's public currentCategory field.
        // We expose it below in CenterCircle; for now a quick reflection-free
        // approach is to compare via the drop result. The highlight check just
        // needs the category which CenterCircle exposes.
        return CenterCircle.Instance != null
            ? CenterCircle.Instance.CurrentCategory
            : null;
    }

    public bool TryRaycast2D(Ray ray, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(ray.origin, ray.direction, 100);
        return hit.collider != null;
    }

    public void PerformClickEffect(Transform target)
    {
        target.DOKill();
        target.DOShakePosition(0.2f, strength: new Vector3(0.051f, 0.051f, 0), vibrato: 10);
    }
}
