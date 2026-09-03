using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class WaterSpring : MonoBehaviour
{
    public float velocity = 0;
    public float force = 0;
    // current height
    public float height = 0f;
    // normal height
    private float target_height = 0f;
    public Transform springTransform;
    [SerializeField]
    private SpriteShapeController spriteShapeController = null;
    private int waveIndex = 0;
    private List<WaterSpring> springs = new();

    private void Awake()
    {
        // Disable collider — do NOT destroy, destroying causes MissingReferenceException
        // in Unity Editor collider tools when the component is accessed post-destroy.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Disable physics simulation on any Rigidbody2D — do NOT destroy it
        // as other components may still hold a reference.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
    }

    public void Init(SpriteShapeController ssc) { 

        var index = transform.GetSiblingIndex();
        waveIndex = index+1;
        
        spriteShapeController = ssc;
        velocity = 0;
        height = transform.localPosition.y;
        target_height = transform.localPosition.y;
    }
    // with dampening
    // adding the dampening to the force
    public void WaveSpringUpdate(float springStiffness, float dampening) { 
        height = transform.localPosition.y;
        // maximum extension
        var x = height - target_height;
        var loss = -dampening * velocity;

        force = - springStiffness * x + loss;
        velocity += force;
        var y = transform.localPosition.y;  
        transform.localPosition = new Vector3(transform.localPosition.x, y+velocity, transform.localPosition.z);
  
    }
    public void WavePointUpdate() { 
        if (spriteShapeController != null && spriteShapeController.spline != null) {
            Spline waterSpline = spriteShapeController.spline;
            int pointCount = waterSpline.GetPointCount();
            if (waveIndex >= 0 && waveIndex < pointCount) {
                Vector3 wavePosition = waterSpline.GetPosition(waveIndex);
                waterSpline.SetPosition(waveIndex, new Vector3(wavePosition.x, transform.localPosition.y, wavePosition.z));
            }
        }
    }

    // NOTE: OnCollisionEnter2D intentionally removed.
    // Water springs react to NO external physics objects.
    // Use WaterShapeController.Splash() to push springs manually if needed.
}
