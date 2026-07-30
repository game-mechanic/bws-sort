using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class ZoomCam : MonoBehaviour
{
    [SerializeField] private float initialSize = 5f;
    [SerializeField] private float finalSize = 10.51f;
    [SerializeField] private float lerpSpeed = 5f;

    private Camera cam;
    private bool isZooming = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cam.orthographicSize = initialSize;
            isZooming = true;
        }

        if (isZooming)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, finalSize, Time.deltaTime * lerpSpeed);

            // Stop zooming when we are very close to the final size
            if (Mathf.Abs(cam.orthographicSize - finalSize) < 0.01f)
            {
                cam.orthographicSize = finalSize;
                isZooming = false;
            }
        }
    }
}
