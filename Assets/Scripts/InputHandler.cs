using DG.Tweening;
using System;
using UnityEngine;

public class InputHandler : Singleton<InputHandler>
{
    Bubble draggable;
    Bubble highlightedBubble;
    Camera mainCamera;
    bool isDragging;
    Vector3 startScale;
    private void Start()
    {
        mainCamera = Camera.main;
        ParticlePool.Init();
    }
    private void Update()
    {
        if (!isDragging
            && Input.GetMouseButtonDown(0)
            && TryRaycast2D(mainCamera.ScreenPointToRay(HandUI.Instance.MousePosition), out RaycastHit2D hit)
            && hit.collider.TryGetComponent(out Bubble d))
        {
            if (d.Category == CategoryManager.Instance.CurrentCategory)
            {
                draggable = d;
                //isDragging = true;
                //startScale = draggable.transform.localScale;
                //draggable.StartDrag();
                //draggable.transform.DOScale(startScale * 1.1f, 0.1f).SetEase(Ease.OutQuad);
                //draggable.Bounce(GameSettings.Instance.MaxBounceAmplitude, GameSettings.Instance.BounceTime);
                Vector3 pos = draggable.transform.position;
                //draggable.StartDrag();
                //draggable.OnBubbleBlasted.AddListener(OnBlast);
                OnBlast();
                highlightedBubble = null;
                void OnBlast()
                {
                    CategoryManager.Instance.ReduceCount(d.Category);
                    ParticlePool.PlayRevealFx(pos);
                    Destroy(draggable.gameObject);
                    draggable.OnBubbleBlasted.RemoveListener(OnBlast);
                }
            }
            else
            {
                d.transform.DOKill();
                Vector3 pos = d.transform.position;
                PerformClickEffect(d.transform, Vector3.one);
            }
        }


        //if (Input.GetMouseButtonUp(0) && draggable != null)
        //{
        //    ReleaseDrag();
        //}
    }
    public void PerformClickEffect(Transform t, Vector3 startScale)
    {
        t.DOKill();

        const float SCALE_AMOUNT = -0.1f;

        Sequence seq = DOTween.Sequence();
        Vector3 pos = t.position;
        seq.Append(t.DOPunchScale(new Vector3(SCALE_AMOUNT, SCALE_AMOUNT, 0f), 0.15f, 1))
           .Join(t.DOShakePosition(0.2f, strength: new Vector3(0.0501f, 0.051f, 0), vibrato: 10))
           .OnKill(() =>
           {
               t.transform.position = pos;
               t.localScale = startScale;
           });
    }
    public bool TryRaycast2D(Ray ray, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(ray.origin, ray.direction, 100);
        return hit.collider != null;
    }
    private void FixedUpdate()
    {
        //if (draggable != null && isDragging)
        //{
        Plane plane = new(Vector3.back, new Vector3(0, 0, 0));
        Ray ray = mainCamera.ScreenPointToRay(HandUI.Instance.MousePosition);

        plane.Raycast(ray, out float enter);

        Vector3 hitPoint = ray.origin + ray.direction * enter;

        if (TryRaycast2D(mainCamera.ScreenPointToRay(HandUI.Instance.MousePosition), out RaycastHit2D hit))
        {
            if (hit.collider.TryGetComponent(out Bubble d))
            {
                if (d != highlightedBubble)
                    Highlight(d);
            }
            else
                Highlight(null);
        }
        else
        {
            Highlight(null);
        }
        //} 
    }

    private bool GetOverlap(Vector3 center, float radius, out Collider2D hit)
    {
        hit = Physics2D.OverlapCircle(center, radius);
        return hit != null;
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
        if (highlightedBubble == null || !TryMerge(draggable, highlightedBubble))
        {
            draggable.transform.DOScale(startScale, 0.1f).SetEase(Ease.OutQuad);
            Highlight(null);
        }
    }

    public bool TryMerge(Bubble a, Bubble b)
    {
        if (a.Category != b.Category) return false;

        byte maxIndex = Math.Max(a.Index, b.Index);

        var bigBubble = a.Index == maxIndex ? a : b;
        var newBubble = Instantiate(GameSettings.Instance.Bubbles[maxIndex + 1]);
        newBubble.transform.SetPositionAndRotation(bigBubble.transform.position, bigBubble.transform.rotation);
        var names = a.Names;
        names.AddRange(b.Names);
        newBubble.Category = a.Category;
        newBubble.SetName(names);
        a.transform.DOKill();
        b.transform.DOKill();
        Destroy(a.gameObject);
        Destroy(b.gameObject);
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
}