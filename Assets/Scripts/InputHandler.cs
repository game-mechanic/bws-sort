using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : Singleton<InputHandler>
{
    Tube selectedTube;
    Bubble draggable;
    Bubble highlightedBubble;
    Camera mainCamera;
    bool isDragging;
    Vector3 startScale;
    Vector3 offset;
    public UnityEvent OnSuccessfullMerge;
    int layerMask;
    private void Start()
    {
        mainCamera = Camera.main;
        ParticlePool.Init();
        layerMask = LayerMask.GetMask("Tube");
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

        if (Input.GetMouseButtonDown(0)
            && TryRaycast2D(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit2D hit)
            && hit.collider.TryGetComponent(out Tube d))
        {
            if (selectedTube == null)
            {
                if (!d.IsEmpty())
                {
                    selectedTube = d;
                    selectedTube.Highlight(true);
                }
            }
            else if (d.CanRecieve(selectedTube.TopCategory))
            {
                if (d != selectedTube)
                {
                    d.Recieve(selectedTube);
                    selectedTube = null;
                }
                else
                {
                    selectedTube.Highlight(false);
                    selectedTube = null;
                }
            }
            else
            {
                selectedTube.Highlight(false);
                selectedTube = null;
            }
        }

        //if (Input.GetMouseButtonUp(0) && draggable != null)
        //{
        //    ReleaseDrag();
        //}
    }



    public bool TryRaycast2D(Ray ray, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(ray.origin, ray.direction, 100, layerMask: layerMask);
        return hit.collider != null;
    }
    //private void FixedUpdate()
    //{
    //    if (draggable != null && isDragging)
    //    {
    //        Plane plane = new(Vector3.back, new Vector3(0, 0, 0));
    //        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

    //        plane.Raycast(ray, out float enter);

    //        Vector3 hitPoint = ray.origin + ray.direction * enter;
    //        hitPoint += offset;

    //        draggable.transform.position = Vector3.Lerp(draggable.transform.position, hitPoint, GameSettings.Instance.DragSpeed * Time.fixedDeltaTime);

    //        if (GetOverlap(hitPoint, draggable.Radius, out Collider2D hit))
    //        {
    //            if (hit.TryGetComponent(out Bubble d))
    //            {
    //                if (d != highlightedBubble && d != draggable)
    //                    Highlight(d);
    //            }
    //            else
    //                Highlight(null);
    //        }
    //        else
    //        {
    //            Highlight(null);
    //        }
    //    }
    //}
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
            draggable.EndDrag();
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
            Highlight(null);
        }
        else
        {
            draggable.EndDrag();
            if (GameSettings.Instance.CanCreateGhost)
                draggable.BlastGhost();
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
    public bool TryMerge(Bubble a, Bubble b, out Bubble bubble)
    {
        if (a.Category != b.Category)
        {
            bubble = null;
            return false;
        }

        byte maxIndex = Math.Max(a.Index, b.Index);
        int nextIndex = a.Index + b.Index + 1;
        var bigBubble = a.Index == maxIndex ? a : b;
        var newBubble = Instantiate(GameSettings.Instance.Bubbles[nextIndex]);
        newBubble.transform.SetPositionAndRotation(bigBubble.transform.position, bigBubble.transform.rotation);
        var names = a.Names;
        names.AddRange(b.Names);
        newBubble.Category = a.Category;
        newBubble.SetName(names);
        newBubble.Bounce();
        a.transform.DOKill();
        b.transform.DOKill();
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (nextIndex == 3)
        {
            newBubble.Blast();
        }
        bubble = newBubble;
        return true;
    }
    public bool TryMerge(Bubble a, Bubble b)
    {
        if (a.Category != b.Category) return false;

        byte maxIndex = Math.Max(a.Index, b.Index);
        int nextIndex = a.Index + b.Index + 1;
        var bigBubble = a.Index == maxIndex ? a : b;
        var newBubble = Instantiate(GameSettings.Instance.Bubbles[nextIndex]);
        newBubble.transform.SetPositionAndRotation(bigBubble.transform.position, bigBubble.transform.rotation);
        var names = a.Names;
        names.AddRange(b.Names);
        newBubble.Category = a.Category;
        newBubble.SetName(names);
        newBubble.Bounce();
        a.transform.DOKill();
        b.transform.DOKill();
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (CategoryManager.Instance.ReduceCount(a.Category) <= 0)
        {
            newBubble.Blast(/*()=> OnSuccessfullMerge?.Invoke()*/);
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
}