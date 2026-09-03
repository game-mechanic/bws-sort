using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Example10 {
    [ExecuteAlways]
    public class WaterShapeController : MonoBehaviour
    {
        [SerializeField]
        private GameObject box;
        [SerializeField]
        private GameObject wavePointPref;
        //////////////////
        private int CorsnersCount = 2;
        [SerializeField]
        private SpriteShapeController spriteShapeController;
        [SerializeField]
        [Range(1, 100)]
        private int WavesCount = 6;
        [SerializeField]
        private GameObject wavePoints;
        //////////////////
        // How much to spread to the other springs
        public float spread = 0.006f;
        // Slowing the movement over time
        [SerializeField]
        private float dampening = 0.03f;
        // How stiff should our spring be constnat
        [SerializeField]
        private float springStiffness = 0.1f;
        [SerializeField]
        private List<WaterSpring> springs = new();

        [Header("Wave Response")]
        [Tooltip("How strongly horizontal bubble movement drives a slosh wave")]
        [SerializeField] private float horizontalWaveSensitivity = 0.02f;
        [Tooltip("How strongly vertical bubble movement (falling/rising) drives all springs")]
        [SerializeField] private float verticalWaveSensitivity = 0.01f;
        [Tooltip("Clamp max impulse per frame to avoid huge spikes")]
        [SerializeField] private float maxImpulsePerFrame = 0.3f;

        private void Awake()
        {
            DisableAllColliders();
        }

        public void DisableAllColliders()
        {
            foreach (Collider2D col in GetComponentsInChildren<Collider2D>(includeInactive: true))
            {
                col.enabled = false;
                col.isTrigger = true;
            }
            foreach (Rigidbody2D rb in GetComponentsInChildren<Rigidbody2D>(includeInactive: true))
            {
                rb.simulated = false;
            }
        }

        /// <summary>
        /// Call this every FixedUpdate from Bubble.cs with the bubble's current world velocity.
        /// Converts velocity into spring impulses simulating liquid sloshing:
        ///   - Moving right  → left springs push up, right springs push down (slosh left)
        ///   - Moving left   → right springs push up, left springs push down (slosh right)
        ///   - Moving down   → all springs push up (liquid "stays behind")
        ///   - Moving up     → all springs push down
        /// </summary>
        public void ApplyBubbleVelocity(Vector2 worldVelocity)
        {
            if (springs == null || springs.Count == 0) return;

            int count = springs.Count;
            float hv = Mathf.Clamp(-worldVelocity.x * horizontalWaveSensitivity, -maxImpulsePerFrame, maxImpulsePerFrame);
            float vv = Mathf.Clamp(-worldVelocity.y * verticalWaveSensitivity, -maxImpulsePerFrame, maxImpulsePerFrame);

            for (int i = 0; i < count; i++)
            {
                if (springs[i] == null) continue;

                // Gradient: t = 0 at left spring, 1 at right spring
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;

                // Horizontal slosh: left springs get +hv, right get -hv (inverted gradient)
                float horizontalImpulse = hv * (0.5f - t) * 2f;

                // Vertical: all springs equally affected
                float verticalImpulse = vv;

                springs[i].velocity += horizontalImpulse + verticalImpulse;
            }
        }

        void FixedUpdate()
        {
            if (spriteShapeController == null || springs == null || springs.Count == 0) return;

            for (int i = 0; i < springs.Count; i++)
            {
                WaterSpring waterSpringComponent = springs[i];
                if (waterSpringComponent != null)
                {
                    waterSpringComponent.WaveSpringUpdate(springStiffness, dampening);
                    waterSpringComponent.WavePointUpdate();
                }
            }

            UpdateSprings();
        }

        private void SetWaves() { 
            if (spriteShapeController == null) return;
            Spline waterSpline = spriteShapeController.spline;
            if (waterSpline == null) return;

            // Clear active springs while regenerating points to avoid index mismatch
            springs.Clear();

            int waterPointsCount = waterSpline.GetPointCount();

            // Remove middle points for the waves
            // Keep only the corners
            for (int i = CorsnersCount; i < waterPointsCount - CorsnersCount; i++) {
                waterSpline.RemovePointAt(CorsnersCount);
            }

            Vector3 waterTopLeftCorner = waterSpline.GetPosition(1);
            Vector3 waterTopRightCorner = waterSpline.GetPosition(2);
            float waterWidth = waterTopRightCorner.x - waterTopLeftCorner.x;

            float spacingPerWave = waterWidth / (WavesCount+1);
            // Set new points for the waves
            for (int i = WavesCount; i > 0 ; i--) {
                int index = CorsnersCount;

                float xPosition = waterTopLeftCorner.x + (spacingPerWave*i);
                Vector3 wavePoint = new Vector3(xPosition, waterTopLeftCorner.y, waterTopLeftCorner.z);
                waterSpline.InsertPointAt(index, wavePoint);
                waterSpline.SetHeight(index, 0.1f);
                waterSpline.SetCorner(index, false);
                waterSpline.SetTangentMode(index, ShapeTangentMode.Continuous);
            }

            CreateSprings(waterSpline);
        }

        private void CreateSprings(Spline waterSpline) { 
            springs = new();
            if (wavePoints == null || wavePointPref == null) return;
            
            for (int i = 0; i <= WavesCount+1; i++) {
                int index = i + 1; 

                Smoothen(waterSpline, index);

                GameObject wavePoint = Instantiate(wavePointPref, wavePoints.transform, false);
                wavePoint.transform.localPosition = waterSpline.GetPosition(index);

                // Disable physics on the instantiated point
                Collider2D col = wavePoint.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = false;
                    col.isTrigger = true;
                }
                Rigidbody2D rb = wavePoint.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = false;
                }

                WaterSpring waterSpring = wavePoint.GetComponent<WaterSpring>();
                if (waterSpring != null)
                {
                    waterSpring.Init(spriteShapeController);
                    springs.Add(waterSpring);
                }
            }
        }
        
        private void Smoothen(Spline waterSpline, int index)
        {
            Vector3 position = waterSpline.GetPosition(index);
            Vector3 positionPrev = position;
            Vector3 positionNext = position;
            if (index > 1) {
                positionPrev = waterSpline.GetPosition(index-1);
            }
            if (index - 1 <= WavesCount) {
                positionNext = waterSpline.GetPosition(index+1);
            }

            Vector3 forward = gameObject.transform.forward;

            float scale = Mathf.Min((positionNext - position).magnitude, (positionPrev - position).magnitude) * 0.33f;

            Vector3 leftTangent = (positionPrev - position).normalized * scale;
            Vector3 rightTangent = (positionNext - position).normalized * scale;

            SplineUtility.CalculateTangents(position, positionPrev, positionNext, forward, scale, out rightTangent, out leftTangent);
            
            waterSpline.SetLeftTangent(index, leftTangent);
            waterSpline.SetRightTangent(index, rightTangent);
        }

        private void UpdateSprings() { 
            if (springs == null) return;
            int count = springs.Count;
            if (count == 0) return;

            float[] left_deltas = new float[count];
            float[] right_deltas = new float[count];

            for(int i = 0; i < count; i++) {
                if (springs[i] == null) continue;

                if (i > 0 && springs[i - 1] != null) {
                    left_deltas[i] = spread * (springs[i].height - springs[i-1].height);
                    springs[i-1].velocity += left_deltas[i];
                }
                if (i < count - 1 && springs[i + 1] != null) {
                    right_deltas[i] = spread * (springs[i].height - springs[i+1].height);
                    springs[i+1].velocity += right_deltas[i];
                }
            }
        }

        public void Splash(int index, float speed) { 
            if (springs != null && index >= 0 && index < springs.Count && springs[index] != null) {
                springs[index].velocity += speed;
            }
        }

        private Vector3 boxStartPosition = new Vector3(1.25f, 9f, 0f);

        void OnEnable() { 
            DisableAllColliders();

            if (box != null)
                box.transform.position = boxStartPosition;

            // In play mode, if wave points already exist from prefab, initialize them without destroying
            if (wavePoints != null && wavePoints.transform.childCount > 0)
            {
                springs = new List<WaterSpring>(wavePoints.GetComponentsInChildren<WaterSpring>());
                foreach (WaterSpring waterSpringComponent in springs)
                {
                    if (waterSpringComponent != null)
                        waterSpringComponent.Init(spriteShapeController);
                }
            }
            else if (!Application.isPlaying)
            {
                StartCoroutine(CreateWaves());
            }
            else
            {
                SetWaves();
            }
        }

        void OnValidate() {
            if (!gameObject.activeInHierarchy) {
                return;
            }
            if (!Application.isPlaying) {
                StartCoroutine(CreateWaves());
            }
        }

        IEnumerator CreateWaves() {
            if (wavePoints == null) yield break;
            springs.Clear();

            foreach (Transform child in wavePoints.transform) {
                StartCoroutine(DestroyChild(child.gameObject));
            }
            yield return null;
            SetWaves();
            yield return null;
        }

        IEnumerator DestroyChild(GameObject go) {
            yield return null;
            if (go != null)
                DestroyImmediate(go);
        }
    }
}