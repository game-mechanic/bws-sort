using DG.Tweening;
using UnityEngine;

public class InputHandler : Singleton<InputHandler>
{
    Bubble draggable;
    Camera mainCamera;
    bool isDragging;
    Vector3 startScale;
    private void Start()
    {
        mainCamera = Camera.main;
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
            draggable.StartDrag();
            draggable.transform.DOScale(startScale * 1.1f, 0.1f).SetEase(Ease.OutQuad);
            draggable.Bounce();
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

            draggable.transform.position = Vector3.Lerp(draggable.transform.position, hitPoint, GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);
        }
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
        draggable.transform.DOScale(startScale, 0.1f).SetEase(Ease.OutQuad);
        draggable = null;
    }
}