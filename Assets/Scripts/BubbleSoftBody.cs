using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Soft-body squish layer for a bubble, modeled as a spring at each surface point:
/// F = k*x + mg, where x is geometric overlap depth with a colliding bubble and mg is
/// that other bubble's weight (Rigidbody2D.mass * Physics2D.gravity) pressing on the
/// contact - matching the sketch where a heavier bubble resting on/pressing into another
/// compresses it further than the bare overlap distance alone would suggest.
///
/// Every FixedUpdate, nearby bubbles are found via Physics2D.OverlapCircle (checked
/// against each Bubble's Radius) and used to compute each point's equilibrium offset
/// (targetOffset = -(x + mg/k)) for points whose rest direction faces that bubble (via
/// dot product). A spring-damper using the same k continuously chases that target, so
/// points stay squished for as long as overlap persists and release smoothly the moment
/// it stops - no reliance on collision callback timing, no separate held/pressed state.
///
/// Rendering is NOT fully handled here beyond an optional LineRenderer outline - read
/// SurfacePoints (or GetWorldPoint/GetLocalPoint) for anything custom.
/// </summary>
[RequireComponent(typeof(Bubble))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(LineRenderer))]
public class BubbleSoftBody : MonoBehaviour
{
    [System.Serializable]
    public struct SurfacePoint
    {
        public Vector3 restDirection;   // unit vector from center, fixed "slot" on the circle
        public float restRadius;        // undisturbed radius for this point
        public float currentOffset;     // signed inward(-)/outward(+) displacement along restDirection
        public float targetOffset;      // where the spring is currently chasing toward (0 = rest, negative = squished)
        public float velocity;          // spring velocity for currentOffset
    }

    [Header("Setup")]
    [SerializeField] int pointCount = 20;
    [SerializeField] float baseRadius = 0.5f; // falls back to Bubble.Radius if <= 0
    [Tooltip("Radius used per surface point when it had its own collider concept. " +
             "Now used to pad/round the polygon collider so thin point spacing doesn't create sharp concave notches.")]
    [SerializeField] float pointRadius = 0.05f;

    [Header("Polygon Collider Rebuild")]
    [Tooltip("Skip SetPath() if no point moved more than this since the last rebuild (perf guard - polygon retriangulation isn't free).")]
    [SerializeField] float rebuildOffsetThreshold = 0.002f;
    [Tooltip("Rebuild at most this many times per second even if points are moving every frame.")]
    [SerializeField] float maxRebuildRate = 60f;

    [Header("Overlap Detection")]
    [Tooltip("LayerMask used for the overlap query that finds nearby bubbles. Set this to whatever layer your bubbles are on.")]
    [SerializeField] LayerMask bubbleLayer = ~0;
    [Tooltip("Extra margin added to the search radius (own radius + this) when querying for nearby bubbles, so fast-moving bubbles don't get missed right as they start overlapping.")]
    [SerializeField] float searchPadding = 1f;
    [Tooltip("Max bubbles to consider overlapping at once (buffer size for the non-alloc overlap query).")]
    [SerializeField] int maxOverlapCandidates = 16;

    [Header("Squish Shaping (F = kx + mg)")]
    [Tooltip("Spring constant k. Equilibrium penetration depth = x + mg/k - higher k means the same overlap/weight pushes points in less (stiffer), lower k pushes them in more (softer). Also drives the spring-damper speed each step.")]
    [SerializeField] float k = 60f;
    [Tooltip("Falloff shaping for how directly a point faces the other bubble (dot product of rest direction vs direction-to-other-bubble, 0-1 range, points facing away are unaffected). 1 = linear falloff across the whole facing hemisphere, higher = squish concentrated tighter on the side directly facing the other bubble.")]
    [SerializeField] float dotStart = 0.5f;
    [SerializeField] float dotFalloffPower = 1.4f;
    [Tooltip("Hard clamp on how far a point can move, as a fraction of restRadius, in either direction (inward squish or outward overshoot). Caps both the computed target AND the live spring position each step, which is what stops jitter from spring overshoot/oscillation - without this, a sudden overlap spike can fling a point past the center or rebound past rest and keep ringing.")]
    [SerializeField, Range(0.05f, 0.95f)] float maxOffsetFraction = 0.6f;
    [Tooltip("Separate, tighter clamp (fraction of restRadius) on how far the ACTUAL PolygonCollider2D shape is allowed to deform, independent of the visual squish above. The collider is solid and Unity's solver reacts to its shape every step, so if it shrinks as much as the visual dent does, the solver's read of the overlap keeps changing and can feed back into oscillation. Keeping this small means physics always sees a near-constant shape and just pushes bodies apart normally, while the LineRenderer/points can still visually squish further.")]
    [SerializeField, Range(0.01f, 0.5f)] float maxColliderDeformFraction = 0.15f;

    [Header("Line Renderer Outline")]
    [Tooltip("If true, LineRenderer.loop is set so the outline closes back to point 0.")]
    [SerializeField] bool closeLoopOutline = true;

    Bubble bubble;
    PolygonCollider2D poly;
    Rigidbody2D rb;
    LineRenderer line;
    SurfacePoint[] points;
    Vector3[] lastBuiltLocalPoints;
    Vector2[] polyBuffer;
    Collider2D[] overlapBuffer;
    ContactFilter2D overlapFilter;
    float lastRebuildTime = -999f;

    public IReadOnlyList<SurfacePoint> SurfacePoints => points;
    public int PointCount => pointCount;

    void Awake()
    {
        bubble = GetComponent<Bubble>();
        poly = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>(); // Bubble already owns/creates this; we just reuse it
        line = GetComponent<LineRenderer>();
        BuildPoints();

        lastBuiltLocalPoints = new Vector3[pointCount];
        polyBuffer = new Vector2[pointCount];
        overlapBuffer = new Collider2D[maxOverlapCandidates];
        overlapFilter = new ContactFilter2D();
        overlapFilter.SetLayerMask(bubbleLayer);
        overlapFilter.useTriggers = false;

        line.positionCount = pointCount;
        line.loop = closeLoopOutline;
        line.useWorldSpace = false; // points are written in local space below

        RebuildPolygon(force: true);
        UpdateLineRenderer();
    }

    void BuildPoints()
    {
        float radius = baseRadius > 0f ? baseRadius : (bubble != null ? bubble.Radius : 0.5f);
        points = new SurfacePoint[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float angle = (i / (float)pointCount) * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            points[i] = new SurfacePoint
            {
                restDirection = dir,
                restRadius = radius,
                currentOffset = 0f,
                targetOffset = 0f,
                velocity = 0f
            };
        }
    }

    /// <summary>
    /// Scans for nearby bubbles using Physics2D.OverlapCircleAll (non-alloc) and computes
    /// each point's targetOffset for this step based on real geometric overlap with each
    /// found bubble's Radius - not collision events. Resets every point's target to 0
    /// first, so a point with no current overlap naturally targets rest.
    /// </summary>
    void ComputeSquishTargets()
    {
        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            p.targetOffset = 0f;
            points[i] = p;
        }

        Vector2 myCenter = transform.position;
        float myRadius = bubble != null ? bubble.Radius : baseRadius;
        float searchRadius = myRadius + searchPadding;

        int count = Physics2D.OverlapCircle(myCenter, searchRadius, overlapFilter, overlapBuffer);

        for (int c = 0; c < count; c++)
        {
            Collider2D otherCol = overlapBuffer[c];
            if (otherCol == null) continue;
            if (otherCol.attachedRigidbody == rb) continue; // skip self (same Rigidbody2D)

            Bubble otherBubble = otherCol.GetComponentInParent<Bubble>();
            if (otherBubble == null || otherBubble == bubble) continue;

            Vector2 otherCenter = otherBubble.transform.position;
            float otherRadius = otherBubble.Radius;

            Vector2 delta = otherCenter - myCenter;
            float distance = delta.magnitude;
            float overlap = (myRadius + otherRadius) - distance;

            if (overlap <= 0f) continue; // not actually touching despite broad-phase hit

            Vector3 dirToOther = distance > 0.0001f
                ? (Vector3)(delta / distance)
                : Vector3.right; // centers coincide (edge case) - arbitrary stable direction

            // mg: weight of the OTHER bubble pressing on this one at this contact, matching
            // the sketch (the big ball's mass*gravity adds to the spring force at the small
            // ball's contact points, on top of the kx deformation term).
            Rigidbody2D otherRb = otherCol.attachedRigidbody;
            float otherMass = otherRb != null ? otherRb.mass : 1f;
            float weightForce = otherMass * Physics2D.gravity.magnitude;

            // Relative closing speed: component of (otherVelocity - myVelocity) along the
            // contact normal (dirToOther). Positive = bubbles approaching, negative = separating.
            // Clamped to >= 0 so only incoming impacts add squish, not separation bouncing away.
            Vector2 myVel = rb != null ? rb.linearVelocity : Vector2.zero;
            Vector2 otherVel = otherRb != null ? otherRb.linearVelocity : Vector2.zero;
            float closingSpeed = Mathf.Max(0f, Vector2.Dot(otherVel - myVel, (Vector2)dirToOther));
            float impactForce = otherMass * closingSpeed;

            ApplyOverlapToTargets(dirToOther, overlap, weightForce, impactForce);
        }
    }

    /// <summary>
    /// Force balance: F = k*x + mg + m*v, where:
    ///   x  = geometric overlap depth (static penetration)
    ///   mg = other bubble's weight (always present while resting on this one)
    ///   mv = other bubble's mass * closing speed along contact normal (spikes on impact,
    ///        self-zeros the moment the bubble bounces away and closing speed goes negative)
    /// Equilibrium displacement = F / k. All terms scaled by the same dot falloff.
    /// </summary>
    void ApplyOverlapToTargets(Vector3 collisionDirectionWorld, float overlapDepth, float weightForce, float impactForce)
    {
        Vector3 localDir = transform.InverseTransformDirection(collisionDirectionWorld).normalized;

        for (int i = 0; i < points.Length; i++)
        {
            float dot = Vector3.Dot(points[i].restDirection, localDir);
            if (dot <= dotStart) continue;

            float falloff = Mathf.Pow(dot - dotStart, dotFalloffPower);

            float x = overlapDepth * falloff;
            float mg = weightForce * falloff;
            float mv = impactForce * falloff;

            // F = kx + mg + mv  →  equilibrium disp = F / k
            float equilibriumDepth = x + (mg / k) + (mv / k);
            float candidateTarget = -equilibriumDepth;

            float minTarget = -points[i].restRadius * maxOffsetFraction;
            candidateTarget = Mathf.Max(candidateTarget, minTarget);

            if (candidateTarget < points[i].targetOffset)
            {
                var p = points[i];
                p.targetOffset = candidateTarget;
                points[i] = p;
            }
        }
    }

    // Damping is derived from k rather than exposed as a separate parameter, kept
    // comfortably underdamped (well below the 2*sqrt(k) critical-damping point) so the
    // squish visibly compresses and wobbles instead of just snapping rigidly to target.
    float Damping => Mathf.Sqrt(k) * 0.5f;

    void FixedUpdate()
    {
        if (points == null) return;
        float dt = Time.fixedDeltaTime;

        // 1. Figure out, from scratch this step, which points should be squished and by
        // how much - based purely on current geometric overlap with nearby bubbles' Radius.
        // No memory of past collisions; a point's target is 0 unless something overlaps it
        // right now, so release is automatic the instant overlap stops.
        ComputeSquishTargets();

        // 2. Spring every point's currentOffset toward its target. While overlap persists,
        // targetOffset stays negative each step so the point is continually pulled toward
        // (and held near) the squished position. The moment overlap stops, targetOffset
        // snaps to 0 and the same spring pulls the point back out to rest.
        float damping = Damping;
        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];

            float displacement = p.currentOffset - p.targetOffset;
            float springForce = -k * displacement;
            float dampingForce = -damping * p.velocity;
            float acceleration = springForce + dampingForce;

            p.velocity += acceleration * dt;
            p.currentOffset += p.velocity * dt;

            // Clamp the live position too (not just the target) so spring overshoot can
            // never push a point past the center or balloon it out past rest - this is
            // what actually stops the jitter, since an unclamped underdamped spring can
            // ring back and forth past 0 a few times before settling.
            float maxOffset = p.restRadius * maxOffsetFraction;
            if (p.currentOffset < -maxOffset)
            {
                p.currentOffset = -maxOffset;
                if (p.velocity < 0f) p.velocity = 0f; // stop pushing further into the clamp
            }
            else if (p.currentOffset > maxOffset)
            {
                p.currentOffset = maxOffset;
                if (p.velocity > 0f) p.velocity = 0f;
            }

            points[i] = p;
        }

        RebuildPolygon(force: false);
        UpdateLineRenderer();
    }

    /// <summary>
    /// Pushes current point positions (with squish applied) into the LineRenderer.
    /// Cheap compared to the polygon rebuild - no retriangulation, just a position
    /// buffer write - so this runs every FixedUpdate unconditionally for smooth visuals.
    /// </summary>
    void UpdateLineRenderer()
    {
        if (line == null) return;
        for (int i = 0; i < points.Length; i++)
            line.SetPosition(i, GetLocalPoint(i));
    }

    /// <summary>
    /// Rewrites the PolygonCollider2D path from current point positions.
    /// Throttled both by a per-point movement threshold and a max rebuild rate,
    /// since SetPath() re-triangulates and isn't cheap to call every physics step
    /// for every bubble on screen.
    /// </summary>
    void RebuildPolygon(bool force)
    {
        if (!force)
        {
            if (Time.time - lastRebuildTime < 1f / Mathf.Max(1f, maxRebuildRate))
                return;

            bool changedEnough = false;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 lp = GetColliderClampedLocalPoint(i);
                if ((lp - lastBuiltLocalPoints[i]).sqrMagnitude > rebuildOffsetThreshold * rebuildOffsetThreshold)
                {
                    changedEnough = true;
                    break;
                }
            }
            if (!changedEnough) return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 lp = GetColliderClampedLocalPoint(i);
            lastBuiltLocalPoints[i] = lp;
            polyBuffer[i] = lp;
        }

        poly.pathCount = 1;
        poly.SetPath(0, polyBuffer);
        lastRebuildTime = Time.time;
    }

    /// <summary>Local-space position of a surface point, including current squish.</summary>
    public Vector3 GetLocalPoint(int index)
    {
        var p = points[index];
        return p.restDirection * (p.restRadius + p.currentOffset);
    }

    /// <summary>
    /// Local-space position of a point clamped to maxColliderDeformFraction instead of the
    /// full visual maxOffsetFraction - used only for the physical PolygonCollider2D shape,
    /// so the solid collider's deformation stays small/stable regardless of how far the
    /// point has visually squished, preventing the collider-shape <-> solver feedback loop.
    /// </summary>
    Vector3 GetColliderClampedLocalPoint(int index)
    {
        var p = points[index];
        float maxColliderOffset = p.restRadius * maxColliderDeformFraction;
        float clampedOffset = Mathf.Clamp(p.currentOffset, -maxColliderOffset, maxColliderOffset);
        return p.restDirection * (p.restRadius + clampedOffset);
    }

    /// <summary>World-space position of a surface point, including current squish.</summary>
    public Vector3 GetWorldPoint(int index)
    {
        return transform.TransformPoint(GetLocalPoint(index));
    }

    public void GetAllLocalPoints(Vector3[] buffer)
    {
        for (int i = 0; i < points.Length; i++)
            buffer[i] = GetLocalPoint(i);
    }

    // Note: real physics bounce/response between bubbles is still handled automatically
    // by Unity's solver acting on the PolygonCollider2D + Rigidbody2D - no collision
    // callback needed here. Squish detection/release is driven entirely by the explicit
    // overlap scan in FixedUpdate (ComputeSquishTargets), which is more reliable than
    // relying on OnCollisionStay2D firing every single step.

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (points == null || !Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 wp = GetWorldPoint(i);
            Gizmos.DrawSphere(wp, 0.02f);
            int next = (i + 1) % points.Length;
            Gizmos.DrawLine(wp, GetWorldPoint(next));
        }
    }
#endif
}