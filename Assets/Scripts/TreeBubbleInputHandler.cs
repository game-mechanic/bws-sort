using UnityEngine;

/// <summary>
/// Simplified input handler for tree bubbles.
/// Tree bubbles handle their own click events directly via OnMouseDown.
/// This class can be used for additional input processing if needed.
/// </summary>
public class TreeBubbleInputHandler : MonoBehaviour
{
    Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        // Tree bubbles use OnMouseDown directly
        // Additional input logic can be added here if needed
    }
    
    public bool TryRaycastTreeBubble(Vector3 screenPos, out TreeBubble bubble)
    {
        bubble = null;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f);
        
        if (hit.collider != null)
        {
            bubble = hit.collider.GetComponent<TreeBubble>();
            return bubble != null;
        }
        
        return false;
    }
}
