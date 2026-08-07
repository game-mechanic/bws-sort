using UnityEngine;
using Water2D;

public class ClickWaterSpawner : MonoBehaviour
{
    public Camera mainCamera;
    public int particlesPerClick = 30;
    public Vector2 initialVelocity = new Vector2(0, -2f);

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f;

            Water2D_Spawner.instance.Spawn(
                particlesPerClick,
                worldPos,
                initialVelocity
            );
        }
    }
}