using UnityEngine;

public class RopeVisual : MonoBehaviour
{
    [SerializeField] private Transform[] pivotPositions;
    [SerializeField] private LineRenderer ropeRenderer;

    private void Update()
    {
        if (ropeRenderer == null || pivotPositions == null || pivotPositions.Length == 0)
            return;

        ropeRenderer.positionCount = pivotPositions.Length;

        for (int i = 0; i < pivotPositions.Length; i++)
        {
            if (pivotPositions[i] != null)
            {
                ropeRenderer.SetPosition(i, pivotPositions[i].position);
            }
        }
    }
}
