
using System.Collections.Generic;
using UnityEngine;

public class SpriteColliderGenerator : MonoBehaviour
{
    [SerializeField, Range(0.001f, 0.5f)] private float colliderTolerance = 0.03f;

    #region Generate Collider
    [EditorButton("Generate Collider")]
    public void GeneratePolygonCollider()
    {
        // Get the first sticker variant
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("No SpriteRenderer or sprite found on the first sticker variant.");
            return;
        }

        // Remove existing PolygonCollider2D if present
        PolygonCollider2D existingCollider = GetComponent<PolygonCollider2D>();
        if (existingCollider != null)
        {
            DestroyImmediate(existingCollider, true);
        }

        // Add new PolygonCollider2D
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();

        // Get the sprite's physics shape
        Sprite sprite = spriteRenderer.sprite;
        List<Vector2> physicsShape = new List<Vector2>();

        // Get the number of physics shapes in the sprite
        int shapeCount = sprite.GetPhysicsShapeCount();

        if (shapeCount > 0)
        {
            // Use the first physics shape
            sprite.GetPhysicsShape(0, physicsShape);

            // Optimize the points using Douglas-Peucker algorithm
            List<Vector2> optimizedPoints = OptimizePolygonPoints(physicsShape, colliderTolerance); // Adjust tolerance as needed

            // Convert the optimized physics shape points to world space relative to this transform
            Vector2[] colliderPoints = new Vector2[optimizedPoints.Count];
            for (int i = 0; i < optimizedPoints.Count; i++)
            {
                // Convert from sprite local space to collider local space
                Vector3 worldPoint = spriteRenderer.transform.TransformPoint(optimizedPoints[i]);
                colliderPoints[i] = transform.InverseTransformPoint(worldPoint);
            }

            // Set the collider points
            polygonCollider.points = colliderPoints;

            // Set to Delaunay mesh for better performance
            polygonCollider.useDelaunayMesh = true;

            Debug.Log($"Optimized collider: {physicsShape.Count} points reduced to {optimizedPoints.Count} points");
        }
        else
        {
            Debug.LogWarning("Sprite has no physics shape defined. Make sure the sprite import settings have 'Generate Physics Shape' enabled.");
        }
    }

    /// <summary>
    /// Optimizes polygon points using the Douglas-Peucker algorithm to reduce point count while preserving shape
    /// </summary>
    /// <param name="points">Original points</param>
    /// <param name="tolerance">Tolerance for point reduction (smaller = more accurate, larger = fewer points)</param>
    /// <returns>Optimized list of points</returns>
    private List<Vector2> OptimizePolygonPoints(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2)
            return new List<Vector2>(points);

        // Use Douglas-Peucker algorithm for line simplification
        return DouglasPeucker(points, tolerance);
    }

    /// <summary>
    /// Douglas-Peucker line simplification algorithm
    /// </summary>
    private List<Vector2> DouglasPeucker(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2)
            return new List<Vector2>(points);

        // Find the point with the maximum distance from the line segment
        float maxDistance = 0f;
        int maxIndex = 0;
        Vector2 start = points[0];
        Vector2 end = points[points.Count - 1];

        for (int i = 1; i < points.Count - 1; i++)
        {
            float distance = PerpendicularDistance(points[i], start, end);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        List<Vector2> result = new List<Vector2>();

        // If max distance is greater than tolerance, recursively simplify
        if (maxDistance > tolerance)
        {
            // Recursive call on the first half
            List<Vector2> firstHalf = points.GetRange(0, maxIndex + 1);
            List<Vector2> recResults1 = DouglasPeucker(firstHalf, tolerance);

            // Recursive call on the second half
            List<Vector2> secondHalf = points.GetRange(maxIndex, points.Count - maxIndex);
            List<Vector2> recResults2 = DouglasPeucker(secondHalf, tolerance);

            // Build the result list
            result.AddRange(recResults1.GetRange(0, recResults1.Count - 1));
            result.AddRange(recResults2);
        }
        else
        {
            // If max distance is less than tolerance, just return the endpoints
            result.Add(start);
            result.Add(end);
        }

        return result;
    }

    /// <summary>
    /// Calculate the perpendicular distance from a point to a line segment
    /// </summary>
    private float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineVector = lineEnd - lineStart;
        Vector2 pointVector = point - lineStart;

        float lineLength = lineVector.magnitude;
        if (lineLength < 0.0001f) // Very small line segment
            return Vector2.Distance(point, lineStart);

        // Project point onto line
        float t = Vector2.Dot(pointVector, lineVector) / (lineLength * lineLength);
        t = Mathf.Clamp01(t);

        Vector2 projection = lineStart + t * lineVector;
        return Vector2.Distance(point, projection);
    }
    #endregion
}