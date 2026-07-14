using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] private Transform anchor;

    [Header("Rope")]
    [SerializeField] private int pointCount = 20;
    [SerializeField] private float segmentLength = 0.25f;
    [SerializeField] private int constraintIterations = 10;
    [SerializeField] private Vector2 gravity = new Vector2(0, -9.81f);
    [SerializeField, Range(0f, 1f)]
    private float drag = 0.98f;   // 1 = no damping, lower = more damping
    [SerializeField] private float maxVelocity = 8f;
    class RopePoint
    {
        public Vector2 position;
        public Vector2 previousPosition;

        public RopePoint(Vector2 pos)
        {
            position = pos;
            previousPosition = pos;
        }
    }

    private readonly List<RopePoint> points = new();
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        Initialize();
        Draw();
    }

    void Initialize()
    {
        points.Clear();

        Vector2 start = anchor.position;

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 pos = start + Vector2.down * segmentLength * i;
            points.Add(new RopePoint(pos));
        }

        // Relax the rope before the first simulation frame.
        for (int i = 0; i < 200; i++)
            SolveConstraints();

        // Remove any initial velocity.
        foreach (var p in points)
            p.previousPosition = p.position;

        lineRenderer.positionCount = pointCount;
        Draw();
        lineRenderer.enabled = true;
    }

    void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
    }

    void LateUpdate()
    {
        Draw();
    }

    void Simulate(float dt)
    {
        // Anchor first point
        points[0].position = anchor.position;
        points[0].previousPosition = anchor.position;

        // Verlet integration
        for (int i = 1; i < points.Count; i++)
        {
            RopePoint p = points[i];

            Vector2 velocity = (p.position - p.previousPosition) * drag;

            if (velocity.magnitude > maxVelocity)
                velocity = velocity.normalized * maxVelocity;

            p.previousPosition = p.position;
            p.position += velocity;
            p.position += gravity * dt * dt;
        }

        // Solve constraints multiple times
        for (int iteration = 0; iteration < constraintIterations; iteration++)
        {
            points[0].position = anchor.position;

            for (int i = 0; i < points.Count - 1; i++)
            {
                RopePoint a = points[i];
                RopePoint b = points[i + 1];

                Vector2 delta = b.position - a.position;
                float distance = delta.magnitude;

                if (distance <= Mathf.Epsilon)
                    continue;

                float error = distance - segmentLength;
                Vector2 correction = delta.normalized * error;

                if (i == 0)
                {
                    // First point fixed
                    b.position -= correction;
                }
                else
                {
                    a.position += correction * 0.5f;
                    b.position -= correction * 0.5f;
                }
            }
        }
    }
    void SolveConstraints()
    {
        points[0].position = anchor.position;

        for (int i = 0; i < points.Count - 1; i++)
        {
            RopePoint a = points[i];
            RopePoint b = points[i + 1];

            Vector2 delta = b.position - a.position;
            float dist = delta.magnitude;

            if (dist < 0.0001f)
                continue;

            Vector2 correction = delta * ((dist - segmentLength) / dist);

            if (i == 0)
            {
                b.position -= correction;
            }
            else
            {
                a.position += correction * 0.5f;
                b.position -= correction * 0.5f;
            }
        }
    }

    void Draw()
    {
        for (int i = 0; i < points.Count; i++)
        {
            lineRenderer.SetPosition(i, points[i].position);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (points == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (var p in points)
            Gizmos.DrawSphere(p.position, 0.03f);
    }
#endif
}