using DG.Tweening;
using System;
using UnityEditor;
using UnityEngine;

public class AimCamera : Singleton<AimCamera>
{
    [SerializeField] CanvasGroup canvasGroup;
    float zDistance;
    float targetSize;
    Camera mainCamera;

    public bool CanMove { get; private set; }

    //private void Awake()
    //{
    //    mainCamera = Camera.main;
    //    targetSize = GetComponent<Camera>().orthographicSize;
    //    zDistance = mainCamera.transform.position.z;
    //}

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        targetSize = GetComponent<Camera>().orthographicSize;
        zDistance = mainCamera.transform.position.z;
        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        UpdatePosition();
    }
    public void Open()
    {
        UpdatePosition();
        gameObject.SetActive(true);

        Camera camera = GetComponent<Camera>();

        float time=0;
        DOTween.To(
            getter: () =>
            {
                return time;
            },
            setter: (x) =>
            {
                time =x;
                camera.transform.position = Vector3.Lerp(camera.transform.position, GetPosition(), time);
            },
            endValue: 1f,
            duration: 0.4f);

        camera.DOOrthoSize(targetSize, 0.5f)
            .From(mainCamera.orthographicSize)
            .OnComplete(() => CanMove = true);

        canvasGroup.DOFade(1, 0.4f);
    }

    void UpdatePosition()
    {
        if (!CanMove) return;
        Vector3 pos = GetPosition();
        transform.position = pos;
    }

    private Vector3 GetPosition()
    {
        Plane plane = new Plane(Vector3.back, Vector3.zero);
        Ray ray = mainCamera.ScreenPointToRay(HandUI.Instance.MousePosition);
        plane.Raycast(ray, out float enter);
        Vector3 pos = ray.origin + ray.direction * enter;
        pos.z = zDistance;
        return pos;
    }

    public void Close()
    {
        Camera camera = GetComponent<Camera>();
        CanMove = false;
        camera.DOOrthoSize(mainCamera.orthographicSize, 0.5f)
            .From(camera.orthographicSize)
            .OnComplete(() => gameObject.SetActive(false));
        camera.transform.DOMove(mainCamera.transform.position, 0.4f);
        canvasGroup.DOFade(0, 0.4f);
    }
}
