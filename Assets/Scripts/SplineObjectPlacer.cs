using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using Dreamteck.Splines;

public class SplineObjectPlacer : MonoBehaviour
{
    [System.Serializable]
    public class Data
    {
        public BubbleType name;
        public Bubble.Data data;
    }
    public static SplineObjectPlacer instance;
    [SerializeField] BubbleContainer bubbleContainerPrefab;
    public SplineComputer splineComputer;
    public int objectCount = 10;
    public float offsetDistance = 1.0f;
    public bool useDistanceMode = false; // Toggle between count-based and distance-based
    public bool useWorldSpace = true;
    public Vector3 positionOffset = Vector3.zero;
    public bool alignToSpline = true;

    public bool randomStackGeneration = false;
    [SerializeField] private int stackCount = 1;
    [SerializeField] List<Data> colorTypes = new();
    //public List<ColorType> colorTypes = new List<ColorType>();
    public List<BubbleContainer> placedBubbles = new();

    [Header("Rearrange Animation Settings")]
    public float rearrangeAnimationDuration = 0.5f;
    public Ease rearrangeAnimationEase = Ease.OutCubic;

    [Header("Gizmo Settings")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoSphereSize = 0.3f;
    public bool showLines = true;
    public bool showDirections = true;
    public float directionLineLength = 1f;

    [SerializeField] private GameObject containerObject;
    public List<float> splineSlots = new List<float>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

    }
    private void Start()
    {
        PlaceObjects();
    }
    private void Update()
    {
        const float moveSpeed = 0.003f;
        for (int i = 0; i < splineSlots.Count; i++)
        {
            splineSlots[i] = Mathf.Clamp01(splineSlots[i] + Time.deltaTime * moveSpeed);
        }
        for (int i = 0; i < placedBubbles.Count; i++)
        {
            placedBubbles[i].UpdatePosition(splineComputer, splineSlots[i]);
        }
    }

    [EditorButton("Place objects")]
    public void PlaceObjects()
    {
        ClearObjects();

        if (splineComputer == null)
        {
            Debug.LogError("Spline Computer or Prefab is not assigned!");
            return;
        }

        containerObject.transform.parent = transform;

        if (useDistanceMode)
        {
            PlaceObjectsByDistance();
        }
        else
        {
            PlaceObjectsByCount();
        }
    }

    private void PlaceObjectsByCount()
    {
        splineSlots.Clear();
        ClearObjects();

        double step = 1.0 / (objectCount - 1);

        for (int i = 0; i < objectCount; i++)
        {
            float percent = (float)(i * step);
            splineSlots.Add(percent);

            SplineSample sample = splineComputer.Evaluate(percent);
            CreateObjectAtSample(sample, i);
        }
    }


    private void PlaceObjectsByDistance()
    {
        splineSlots.Clear();
        ClearObjects();

        double splineLength = splineComputer.CalculateLength();
        float currentDistance = 0f;
        int index = 0;

        while (currentDistance <= splineLength)
        {
            float moved;
            double percent = splineComputer.Travel(
                0.0,
                currentDistance,
                out moved,
                Spline.Direction.Forward
            );

            splineSlots.Add((float)percent);

            SplineSample sample = splineComputer.Evaluate(percent);
            CreateObjectAtSample(sample, index);

            currentDistance += offsetDistance;
            index++;
        }
    }


    private void CreateObjectAtSample(SplineSample sample, int index)
    {
        Data data = colorTypes[index % colorTypes.Count];
        var bubble = CategoryManager.CreateBubble(Vector3.zero, data.name, data.data);

        var container = Instantiate(bubbleContainerPrefab, Vector3.zero, Quaternion.identity);
        bubble.transform.SetParent(container.transform);
        bubble.transform.localPosition = Vector3.zero;
        bubble.Container = container;
        container.Bubble = bubble;

        container.UpdatePosition(splineComputer, splineSlots[index]);

        // Apply rotation if needed (this won't be overridden by the follower)
        if (alignToSpline)
        {
            bubble.transform.rotation = sample.rotation;
        }


        //EditorUtility.SetDirty(hexaStack.splineFollower);

        placedBubbles.Add(container);
    }

    public void RearrangePlacedStacks(BubbleContainer stackToRemove)
    {
        int removedIndex = placedBubbles.IndexOf(stackToRemove);
        //RemovePlacedStack(stackToRemove);

        // Only rearrange if there are remaining stacks and the removed index is valid
        if (placedBubbles.Count == 0 || removedIndex == -1) return;

        // Only rearrange stacks that were after the removed stack
        if (useDistanceMode)
        {
            RearrangeByDistanceSmooth(removedIndex);
        }
        else
        {
            RearrangeByCountSmooth(removedIndex);
        }
    }

    private void RearrangeByCountSmooth(int startFromIndex)
    {
        //if (placedStacks.Count <= 1) return;

        //double step = 1.0 / (placedStacks.Count - 1);

        //// Create a sequence to animate all movements together
        //Sequence rearrangeSequence = DOTween.Sequence();

        //for (int i = startFromIndex; i < placedStacks.Count; i++)
        //{
        //    // Calculate target percent based on the new position after removal
        //    // The stack at index i should move to position (i - 1) since one was removed before it
        //    double targetPercent = (i) * step;  // This gives us the correct target position

        //    HexaStack stack = placedStacks[i];
        //    double currentPercent = stack.splineFollower.result.percent;

        //    // Create a tween for smooth movement along the spline
        //    var moveTween = DOTween.To(
        //        () => currentPercent,
        //        (value) =>
        //        {
        //            stack.splineFollower.SetPercent(value);

        //            // Update rotation if alignment is enabled
        //            if (alignToSpline)
        //            {
        //                SplineSample currentSample = splineComputer.Evaluate(value);
        //                stack.transform.rotation = currentSample.rotation;
        //            }
        //        },
        //        targetPercent,
        //        rearrangeAnimationDuration
        //    ).SetEase(rearrangeAnimationEase);

        //    // Update the object name to reflect new index
        //    moveTween.OnComplete(() =>
        //    {
        //        int finalIndex = placedStacks.IndexOf(stack);
        //        stack.gameObject.name = prefabToPlace.name + "_" + finalIndex;
        //        EditorUtility.SetDirty(stack.splineFollower);
        //    });

        //    rearrangeSequence.Join(moveTween);
        //}
    }

    bool setSpeed = false;

    private void RearrangeByDistanceSmooth(int startFromIndex)
    {
        if (placedBubbles.Count == 0) return;

        //double splineLength = splineComputer.CalculateLength();

        // Create a sequence to animate all movements together
        //Sequence rearrangeSequence = DOTween.Sequence();

        //Debug.Log($"Rearranging from index {startFromIndex}, total stacks: {placedStacks.Count}");

        // for (int i = startFromIndex + 1; i < placedStacks.Count; i++)
        //{
        //  Bubble stack = placedStacks[i];

        // Calculate where this stack should be positioned
        // Since we removed one stack, each remaining stack should move to fill the gap
        /// float targetDistance = i * offsetDistance;

        //double currentPercent = stack.splineFollower.result.percent;

        //// Calculate target percent
        //float moved = 0.0f;
        //double targetPercent = splineComputer.Travel(0.0, targetDistance, out moved, Spline.Direction.Forward);

        //targetPercent = placedStacks[i - 1].splineFollower.result.percent;

        //// Ensure we don't exceed the spline length
        //if (targetDistance > splineLength)
        //{
        //    Debug.LogWarning($"Target distance {targetDistance} exceeds spline length {splineLength}");
        //    break;
        //}


        ////Debug.Log($"Stack {i} moving from percent {currentPercent:F3} to percent {targetPercent:F3} (distance {targetDistance})");

        ////if (!setSpeed)
        ////    stack.splineFollower.followSpeed *= 2f;

        //// Create a tween for smooth movement along the spline
        //var moveTween = DOTween.To(
        //    () => currentPercent,
        //    (value) =>
        //    {
        //        stack.splineFollower.SetPercent(value);

        //        // Update rotation if alignment is enabled
        //        if (alignToSpline)
        //        {
        //            SplineSample currentSample = splineComputer.Evaluate(value);
        //            stack.transform.rotation = currentSample.rotation;
        //        }
        //    },
        //    targetPercent,
        //    rearrangeAnimationDuration
        //).SetEase(rearrangeAnimationEase);

        //// Update the object name to reflect new index
        //moveTween.OnComplete(() =>
        //{
        //    stack.gameObject.name = prefabToPlace.name + "_" + i;
        //    EditorUtility.SetDirty(stack.splineFollower);
        //});

        //rearrangeSequence.Join(moveTween);

        //setSpeed = true;
        //}

        RemovePlacedStack(placedBubbles[startFromIndex]);

        //for (int i = 0; i < startFromIndex; i++)
        //{
        //    var stack = placedStacks[i];
        //    stack.splineFollower.followSpeed /= 2;
        //}
    }

    public void RemovePlacedStack(BubbleContainer stack)
    {
        if (placedBubbles.Contains(stack))
        {
            placedBubbles.Remove(stack);
        }
    }

    [EditorButton("Clear Objects")]
    public void ClearObjects()
    {
        foreach (var placedStack in placedBubbles)
        {
            DestroyImmediate(placedStack.gameObject);
        }

        placedBubbles.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || splineComputer == null)
            return;

        Gizmos.color = gizmoColor;

        if (useDistanceMode)
        {
            DrawGizmosByDistance();
        }
        else
        {
            DrawGizmosByCount();
        }
    }

    private void DrawGizmosByCount()
    {
        double step = 1.0 / (objectCount - 1);
        Vector3 previousPosition = Vector3.zero;

        for (int i = 0; i < objectCount; i++)
        {
            double percent = i * step;
            SplineSample sample = splineComputer.Evaluate(percent);
            Vector3 position = sample.position + positionOffset;

            DrawGizmoAtPosition(position, sample, i > 0, previousPosition);
            previousPosition = position;
        }
    }

    private void DrawGizmosByDistance()
    {
        double splineLength = splineComputer.CalculateLength();
        float currentDistance = 0.0f;
        Vector3 previousPosition = Vector3.zero;
        int index = 0;

        while (currentDistance <= splineLength)
        {
            float moved = 0.0f;
            double percent = splineComputer.Travel(0.0, currentDistance, out moved, Spline.Direction.Forward);
            SplineSample sample = splineComputer.Evaluate(percent);
            Vector3 position = sample.position + positionOffset;

            DrawGizmoAtPosition(position, sample, index > 0, previousPosition);
            previousPosition = position;

            currentDistance += offsetDistance;
            index++;
        }
    }

    private void DrawGizmoAtPosition(Vector3 position, SplineSample sample, bool drawLine, Vector3 previousPosition)
    {
        // Draw sphere at position
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(position, gizmoSphereSize);

        // Draw line connecting positions
        if (showLines && drawLine)
        {
            Gizmos.DrawLine(previousPosition, position);
        }

        // Draw direction indicator
        if (showDirections && alignToSpline)
        {
            Vector3 forward = sample.forward * directionLineLength;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(position, position + forward);
            Gizmos.color = gizmoColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || splineComputer == null)
            return;

        Gizmos.color = Color.green;

        if (useDistanceMode)
        {
            double splineLength = splineComputer.CalculateLength();
            float currentDistance = 0.0f;

            while (currentDistance <= splineLength)
            {
                float moved = 0.0f;
                double percent = splineComputer.Travel(0.0, currentDistance, out moved, Spline.Direction.Forward);
                SplineSample sample = splineComputer.Evaluate(percent);
                Vector3 position = sample.position + positionOffset;

                Gizmos.DrawSphere(position, gizmoSphereSize * 0.5f);
                currentDistance += offsetDistance;
            }
        }
        else
        {
            double step = 1.0 / (objectCount - 1);

            for (int i = 0; i < objectCount; i++)
            {
                double percent = i * step;
                SplineSample sample = splineComputer.Evaluate(percent);
                Vector3 position = sample.position + positionOffset;

                Gizmos.DrawSphere(position, gizmoSphereSize * 0.5f);
            }
        }
    }
}
