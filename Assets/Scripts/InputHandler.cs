using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : Singleton<InputHandler>
{
    MapChunk draggable;
    MapChunk highlightedBubble;
    Camera mainCamera;
    bool isDragging;
    [SerializeField] float dragOffset = .5f;
    [SerializeField] GameObject dragPointer;
    Vector3 startScale;
    Vector3 offset;
    [SerializeField] Transform endPoint;
    public UnityEvent OnSuccessfullMerge;

    private void Start()
    {
        mainCamera = Camera.main;
        ParticlePool.Init();
    }
    private void OnDestroy()
    {
        ParticlePool.ReleasePool();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 3f;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Time.timeScale = 1f;
        }

        if (!isDragging
            && Input.GetMouseButtonDown(0)
            && TryRaycast2D(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit2D hit)
            && hit.collider.TryGetComponent(out MapChunk d))
        {
            draggable = d;
            isDragging = true;
            startScale = draggable.transform.localScale;
            offset = new Vector2(draggable.transform.position.x, draggable.transform.position.y) - hit.point;
            d.StartDrag();
            dragPointer.SetActive(true);
        }

        if (Input.GetMouseButtonUp(0) && draggable != null)
        {
            ReleaseDrag();
            dragPointer.SetActive(false);
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
            Vector3 targetPosition = Vector3.Lerp(draggable.transform.position, hitPoint, GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);
            draggable.transform.position = targetPosition;
            targetPosition += Vector3.up * dragOffset;
            dragPointer.transform.position = targetPosition;

            if (GetOverlap(targetPosition, .1f, out Collider2D hit))
            {
                if (hit.TryGetComponent(out MapChunk d))
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
            if (results[i].TryGetComponent(out MapChunk b))
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
                //if (b.Category == draggable.Category && b != draggable)
                //{
                //    hit = results[i];
                //    return true;
                //}
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

        if (!GameSettings.Instance.CanMerge)
        {
            Highlight(null);
            draggable = null;
            isDragging = false;
            return;
        }


        //draggable.EndDrag();
        if (highlightedBubble == null)
        {
            Highlight(null);
            draggable.ReturnBack();
        }
        else if (!TryMerge(draggable, highlightedBubble))
        {
            draggable.ReturnBack();
            highlightedBubble.Highlight(true, GameSettings.Instance.WrongColor);
            DOVirtual.DelayedCall(0.2f, () =>
            {
                Highlight(null);
            });
        }
        else
        {
            draggable.EndDrag();
            draggable = null;
        }
        //if (!TryMerge(draggable, highlightedBubble))
        //{
        //    if (highlightedBubble == null)
        //    {
        //        draggable.ReturnBack();
        //    }
        //    else
        //    {
        //        draggable.ReturnBack();
        //        Highlight(null);
        //    }
        //}
        //else
        //{
        //    draggable.EndDrag();
        //    draggable.BlastGhost();
        //}
        //Highlight(null);
    }

    public bool TryMerge(MapChunk a, MapChunk b)
    {
        if (a.BubbleType != b.BubbleType) return false;

        a.transform.DOMove(b.transform.position, 0.2f);
        b.gameObject.SetActive(false);
        MapStatesQueue.Instance.Remove(a);
        return true;
    }

    void Highlight(MapChunk newBubble)
    {
        if (highlightedBubble != null)
        {
            highlightedBubble.Highlight(false);
        }
        highlightedBubble = newBubble;
        if (highlightedBubble != null)
        {
            highlightedBubble.Highlight(true, GameSettings.Instance.HighlightColor);
        }
    }
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        // Distance from camera to world z=0 plane
        float distance = Mathf.Abs(Camera.main.transform.position.z);

        Vector3 screenTopLeft =
            Camera.main.ScreenToWorldPoint(
                new Vector3(0, Screen.height * 2, distance));

        screenTopLeft.z = 0;

        float offset = 1f;

        for (int i = 0; i < GameSettings.Instance.Order.Length; i++)
        {
            Color color = GameSettings.Instance.Order[i % GameSettings.Instance.Order.Length].Color;
            color.a = 1;
            Gizmos.color =
                color;

            Gizmos.DrawSphere(
                screenTopLeft + Vector3.down * offset * i,
                0.5f);
        }
    }
    public void PanCamera()
    {
        DOVirtual.DelayedCall(0.2f, () => { mainCamera.transform.DOMove(endPoint.position, 2f).SetEase(Ease.InSine); });
    }
}