using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PenInputHandler : MonoBehaviour
{
    [SerializeField] private Color penColor = new Color(1, 1, 0, 1);
    [SerializeField] private Material lineMaterial;
    [SerializeField] private byte orderInLayer = 5;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float minPointDistance = 0.05f;
    [SerializeField] private float fadeDuration = 0.5f;

    private readonly List<LineRenderer> lineRenderers = new();

    private bool isDrawMode;
    private bool isDrawing;

    private LineRenderer currentLine;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ToggleDrawMode();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            FadeLines();
        }

        if (!isDrawMode)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            BeginLine();
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            UpdateLine();
        }

        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            EndLine();
        }
    }

    private void ToggleDrawMode()
    {
        isDrawMode = !isDrawMode;
        FadeLines();
    }

    private void FadeLines()
    {
        if (!isDrawMode)
        {
            if (isDrawing)
                EndLine();

            FadeAndDestroyLines();
        }
    }

    private void BeginLine()
    {
        isDrawing = true;

        GameObject go = new GameObject("Drawn Line");
        currentLine = go.AddComponent<LineRenderer>();

        currentLine.material = new Material(lineMaterial);
        currentLine.startColor = currentLine.endColor = penColor;
        currentLine.widthMultiplier = lineWidth;
        currentLine.sortingOrder = orderInLayer;
        currentLine.positionCount = 1;
        currentLine.useWorldSpace = true;

        Vector3 pos = GetMouseWorldPosition();

        currentLine.SetPosition(0, pos);
    }

    private void UpdateLine()
    {
        Vector3 pos = GetMouseWorldPosition();

        if (currentLine.positionCount > 0)
        {
            Vector3 lastPos = currentLine.GetPosition(currentLine.positionCount - 1);

            if (Vector3.Distance(lastPos, pos) < minPointDistance)
                return;
        }

        currentLine.positionCount++;
        currentLine.SetPosition(currentLine.positionCount - 1, pos);
    }

    private void EndLine()
    {
        isDrawing = false;

        if (currentLine != null)
        {
            lineRenderers.Add(currentLine);
            currentLine = null;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;

        // Draw on Z = 0 plane
        mouse.z = -cam.transform.position.z;

        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = 0;

        return world;
    }

    private void FadeAndDestroyLines()
    {
        foreach (LineRenderer lr in lineRenderers)
        {
            if (lr == null)
                continue;

            Material mat = lr.material;

            Color start = mat.color;

            DOTween.To(
                () => mat.color,
                c => mat.color = c,
                new Color(start.r, start.g, start.b, 0),
                fadeDuration
            )
            .OnComplete(() =>
            {
                if (lr != null)
                    Destroy(lr.gameObject);
            });
        }

        lineRenderers.Clear();
    }
}