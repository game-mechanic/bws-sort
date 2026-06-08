using UnityEngine;

public class RopeVisual : MonoBehaviour
{
    [SerializeField] private Transform[] pivotPositions;

    [SerializeField] private LineRenderer ropePartA;
    [SerializeField] private LineRenderer ropePartB;


    [SerializeField] private int breakIndex; // Pivot where rope breaks

    private bool isBroken;

    private void Update()
    {
        if (!isBroken)
        {
            DrawWholeRope();
        }
        else
        {
            DrawBrokenRope();
        }
    }

    void DrawWholeRope()
    {
        ropePartB.enabled = false;

        ropePartA.positionCount = pivotPositions.Length;

        for (int i = 0; i < pivotPositions.Length; i++)
        {
            ropePartA.SetPosition(i, pivotPositions[i].position);
        }
    }

    void DrawBrokenRope()
    {
        ropePartB.enabled = true;

        // First half
        ropePartA.positionCount = breakIndex;

        for (int i = 0; i < breakIndex; i++)
        {
            ropePartA.SetPosition(i, pivotPositions[i].position);
        }

        // Second half
        int secondCount = pivotPositions.Length - breakIndex;

        ropePartB.positionCount = secondCount;

        for (int i = breakIndex; i < pivotPositions.Length; i++)
        {
            ropePartB.SetPosition(i - breakIndex, pivotPositions[i].position);
        }
    }

    public void Break()
    {
        var breakPoint = pivotPositions[breakIndex].GetComponent<HingeJoint2D>();
        breakPoint.connectedBody = null;
        breakPoint.useConnectedAnchor = false;
        isBroken = true;
    }
}