using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Center drop-target circle with 4 fixed slots.
/// - Accepted bubbles fly into a slot and sit there (sprite renderer disabled, slot icon shown).
/// - When all slots are filled the circle + all slotted bubbles explode (SpriteRenderers disabled),
///   then the next category pops in with empty slots and the circle scales 0 → original.
/// - Label is center-aligned while empty, top-aligned once the first bubble lands.
/// - Circle tint: white idle, green on correct hover, red on wrong hover / wrong drop.
///   After a correct drop the tint returns to white.
/// </summary>
public class CenterCircle : Singleton<CenterCircle>
{
    [Header("References")]
    [SerializeField] SpriteRenderer circleRenderer;
    [SerializeField] TextMeshPro categoryLabel;
    [SerializeField] Transform visualRoot;          // scaled for pop-in / pop-out

    [Header("Slots — assign 4 child Transforms in order")]
    [SerializeField] Transform[] slots = new Transform[4];    // world positions inside circle
    [SerializeField] SpriteRenderer[] slotHighlights;         // one per slot (optional tint indicator)
    [SerializeField] float slotBubbleScale = 0.6f;            // scale multiplier for bubbles in slots

    [Header("Drop Zone")]
    [SerializeField] float dropRadius = 1.2f;

    [Header("Colors")]
    [SerializeField] Color idleColor      = Color.white;
    [SerializeField] Color correctColor   = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] Color wrongColor     = new Color(0.95f, 0.2f, 0.2f, 1f);

    [Header("Explosion Animation")]
    [SerializeField] float anticipationCircleScale = 1.25f;   // Scale multiplier for circle during anticipation
    [SerializeField] float anticipationTextScale = 1.25f;     // Scale multiplier for text during anticipation

    [Header("FX")]
    [SerializeField] Vector3 popFxRotation = new Vector3(-90f, 0f, 0f);  // kept for reference

    // ── events ─────────────────────────────────────────────────────────────
    public static event Action<BubbleType> OnCategoryCompleted;
    public static event Action<BubbleType> OnCategoryAdvanced;

    // ── state ──────────────────────────────────────────────────────────────
    BubbleType currentCategory;
    int filledSlots;                            // how many slots are occupied
    bool isAnimating;
    Queue<BubbleType> categoryQueue = new Queue<BubbleType>();
    Vector3 originalScale;
    Vector3 originalLabelScale;                 // design-time scale of categoryLabel
    // keep refs to bubble GameObjects sitting in slots so we can explode them
    List<GameObject> slottedBubbles = new List<GameObject>();

    public BubbleType CurrentCategory => currentCategory;

    // ── lifecycle ──────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        originalScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        originalLabelScale = categoryLabel != null ? categoryLabel.transform.localScale : Vector3.one;
    }

    // ── public API ─────────────────────────────────────────────────────────

    public void SetCategorySequence(IEnumerable<BubbleType> orderedCategories)
    {
        categoryQueue.Clear();
        foreach (var c in orderedCategories)
            categoryQueue.Enqueue(c);

        AdvanceToNext();
    }

    public bool IsInsideDropZone(Vector3 worldPos)
    {
        return Vector2.Distance(worldPos, transform.position) <= dropRadius;
    }

    /// <summary>
    /// Try to accept a dropped bubble. spawnPos = where the bubble was picked up from
    /// so a replacement can be spawned there.
    /// </summary>
    public bool TryDrop(Bubble bubble, Vector3 spawnPos)
    {
        if (isAnimating || currentCategory == null) return false;

        if (bubble.Category != currentCategory)
        {
            FlashColor(wrongColor);
            PlayWrongFeedback();
            return false;
        }

        AcceptBubble(bubble, spawnPos);
        return true;
    }

    /// <summary>
    /// Called while a bubble is being dragged over the circle.
    /// Circle stays white regardless — color feedback only happens on drop.
    /// </summary>
    public void SetHoverHighlight(bool active, bool isMatch = false)
    {
        // No color change during hover — circle always stays idle white
    }

    // ── private ────────────────────────────────────────────────────────────

    void AcceptBubble(Bubble bubble, Vector3 spawnPos)
    {
        CategoryManager.Instance.ReduceCount(bubble.Category);

        // Determine which slot this bubble flies into
        int slotIndex = filledSlots;
        filledSlots++;

        Vector3 slotWorldPos = slots[slotIndex].position;

        bubble.transform.DOKill();
        bubble.SetColliderEnabled(false);

        // Fly bubble into its slot
        Sequence flyIn = DOTween.Sequence();
        flyIn.Append(bubble.transform.DOMove(slotWorldPos, 0.28f).SetEase(Ease.InBack));
        flyIn.Join(bubble.transform.DOScale(Vector3.one * slotBubbleScale, 0.28f).SetEase(Ease.InBack));
        flyIn.AppendCallback(() =>
        {
            // Make the bubble a child of visualRoot so it scales with the center circle during pop
            bubble.transform.SetParent(visualRoot, worldPositionStays: true);

            // Disable background sprite — only text/icon remains visible in the slot
            bubble.DisableBackground();

            // Track it for the explosion later
            slottedBubbles.Add(bubble.gameObject);

            // Spawn replacement at the slot the player dragged from
            CategoryManager.Instance.SpawnNewBubbleAt(spawnPos);
        });

        // Flash green then back to white on correct drop
        FlashColor(correctColor);

        // Pulse visual root
        visualRoot.DOKill();
        visualRoot.DOPunchScale(originalScale * 0.15f, 0.25f, 5, 0.5f);

        // Switch label alignment once first bubble is placed
        if (filledSlots == 1)
            SetLabelAlignment(top: true);

        if (filledSlots >= slots.Length)
            DOVirtual.DelayedCall(0.4f, ExplodeAndAdvance);
    }

    void ExplodeAndAdvance()
    {
        if (isAnimating) return;
        isAnimating = true;

        OnCategoryCompleted?.Invoke(currentCategory);

        // Scale-up sequence: anticipation → hide sprites & label → explosion → pause → advance
        visualRoot.DOKill();
        Sequence explode = DOTween.Sequence();

        // 1. Anticipation — scale up the circle (visualRoot automatically scales slots inside)
        //    AND scale the text separately since it's a separate GameObject
        //    Circle sprite STAYS VISIBLE during this phase
        explode.Append(visualRoot.DOScale(originalScale * anticipationCircleScale, 0.3f).SetEase(Ease.OutQuad));
        
        if (categoryLabel != null)
        {
            categoryLabel.transform.DOKill();
            explode.Join(categoryLabel.transform.DOScale(originalLabelScale * anticipationTextScale, 0.3f).SetEase(Ease.OutQuad));
        }

        // 2. Brief pause at peak
        explode.AppendInterval(0.15f);

        // 3. NOW disable sprites — circle sprite, slotted bubble sprites, and label
        explode.AppendCallback(() =>
        {
            // Disable circle sprite renderer
            circleRenderer.enabled = false;

            // Disable sprite renderers on all slotted bubbles (explosion effect)
            foreach (var go in slottedBubbles)
            {
                if (go == null) continue;
                foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                    sr.enabled = false;
            }

            // Hide label and reparent it out of visualRoot
            if (categoryLabel != null)
            {
                categoryLabel.transform.SetParent(transform, worldPositionStays: true);
                categoryLabel.enabled = false;
            }
        });

        // 4. Particle burst
        explode.AppendCallback(() =>
        {
            ParticlePool.PlayRevealFx(transform.position);
            foreach (var go in slottedBubbles)
                if (go != null) ParticlePool.PlayRevealFx(go.transform.position);
        });

        // 5. Big explosion scale: current → 5× → 0 (slower outward push)
        explode.Append(visualRoot.DOScale(originalScale * 5f, 0.45f).SetEase(Ease.OutQuad));
        explode.Append(visualRoot.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InQuad));

        // 6. Wait 1 second after the explosion, then advance to next category
        explode.AppendInterval(1f);
        explode.AppendCallback(() =>
        {
            // Destroy leftover slot bubble GOs
            foreach (var go in slottedBubbles)
                if (go != null) Destroy(go);
            slottedBubbles.Clear();

            AdvanceToNext();
        });
    }

    void AdvanceToNext()
    {
        filledSlots = 0;
        slottedBubbles.Clear();

        // Restore label under visualRoot and reset its scale to original
        if (categoryLabel != null)
        {
            categoryLabel.transform.SetParent(visualRoot, worldPositionStays: true);
            categoryLabel.transform.localScale = originalLabelScale;
            // Keep label HIDDEN during the circle pop-in
            categoryLabel.enabled = false;
        }

        if (categoryQueue.Count == 0)
        {
            if (categoryLabel != null)
            {
                categoryLabel.text = "Done!";
                categoryLabel.enabled = true;
            }
            isAnimating = false;
            return;
        }

        currentCategory = categoryQueue.Dequeue();

        OnCategoryAdvanced?.Invoke(currentCategory);

        // Reset label to center-aligned (no bubbles yet)
        SetLabelAlignment(top: false);
        RefreshLabel();

        // Re-enable circle renderer and scale in from 0 → original
        circleRenderer.enabled = true;
        circleRenderer.color = idleColor;
        visualRoot.localScale = Vector3.zero;
        visualRoot.DOScale(originalScale, 0.38f)
                  .SetEase(Ease.OutBack)
                  .OnComplete(() =>
                  {
                      // Pop the text in with a cartoonistic overshoot animation
                      if (categoryLabel != null)
                      {
                          categoryLabel.enabled = true;
                          categoryLabel.transform.localScale = Vector3.zero;
                          categoryLabel.transform.DOKill();
                          categoryLabel.transform
                              .DOScale(originalLabelScale, 0.45f)
                              .SetEase(Ease.OutBack, overshoot: 3.5f);
                      }
                      isAnimating = false;
                  });
    }

    void RefreshLabel()
    {
        if (categoryLabel == null) return;
        categoryLabel.text = currentCategory != null ? currentCategory.name : string.Empty;
    }

    void SetLabelAlignment(bool top)
    {
        if (categoryLabel == null) return;
        categoryLabel.alignment = top
            ? TextAlignmentOptions.Top
            : TextAlignmentOptions.Center;
    }

    void FlashColor(Color flash)
    {
        circleRenderer.DOKill();
        circleRenderer.DOColor(flash, 0.15f)
                      .OnComplete(() => circleRenderer.DOColor(idleColor, 1f)); // 1 second fade back to white
    }

    void PlayWrongFeedback()
    {
        visualRoot.DOKill();
        visualRoot.DOShakePosition(0.22f, new Vector3(0.1f, 0.1f, 0), 14);
    }

    // ── gizmos ─────────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dropRadius);

        if (slots == null) return;
        Gizmos.color = Color.yellow;
        foreach (var s in slots)
            if (s != null) Gizmos.DrawSphere(s.position, 0.15f);
    }
}
