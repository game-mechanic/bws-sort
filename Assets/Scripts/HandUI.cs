using UnityEngine;
using UnityEngine.UI;

public class HandUI : Singleton<HandUI>
{
    private Transform handTransform;
    private Image hand;
    public Vector2 offset;
    public Sprite idle;
    public Sprite click;
    private Vector3 mousePosition;

    [SerializeField] Transform lineStartPosition;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int pointsCount;
    [SerializeField] Vector3 handJoinOffset;

    Camera mainCamera;

    public Vector3 MousePosition { get => mousePosition; }

    void Start()
    {
        mainCamera = Camera.main;
        handTransform = transform.GetChild(0);

        hand = handTransform.GetComponent<Image>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = pointsCount;
            UpdateLineRenderer();
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0
        || Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
            return;
        UpdateLineRenderer();

        mousePosition = Vector3.Lerp(MousePosition, Input.mousePosition + new Vector3(offset.x, offset.y), 10 * Time.deltaTime);
        handTransform.position = MousePosition;
        if (Input.GetMouseButtonDown(0)) hand.sprite = click;
        else if (Input.GetMouseButtonUp(0)) hand.sprite = idle;
    }
    private void UpdateLineRenderer()
    {
        if (lineRenderer == null)
        {
            return;
        }
        float timePerPoint = 1f / pointsCount;

        Plane plane = new Plane(Vector3.back, Vector3.zero);
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition + handJoinOffset);
        plane.Raycast(ray, out float enter);

        Vector3 mousePosition = ray.origin + ray.direction * enter;


        for (int i = 0; i < pointsCount; i++)
        {
            lineRenderer.SetPosition(i, CubicBezier(lineStartPosition.position, mousePosition, 2, timePerPoint * i));
        }
    }

    /// <summary>
    /// Computes a cubic Bézier curve with a given height.
    /// </summary>
    /// <param name="start">Starting point.</param>
    /// <param name="end">End point.</param>
    /// <param name="height">Height of the midpoint.</param>
    /// <param name="t">Interpolation factor (0 to 1).</param>
    /// <returns>Interpolated point on the Bézier curve.</returns>
    public static Vector3 CubicBezier(Vector3 start, Vector3 end, float height, float t)
    {
        float _1MinusT = 1 - t;
        Vector3 mid = (start + end) / 2;
        Vector3 perpendicular = Vector3.Cross(Vector3.forward, (end - start).normalized);
        mid += perpendicular.normalized * height;


        return _1MinusT * _1MinusT * start
            + 2 * _1MinusT * t * mid
            + t * t * end;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(hand.transform.position + handJoinOffset, 2f);
    }
}