using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MetaballParticleClass : MonoBehaviour {


	public GameObject MObject;
	public float LifeTime;
	public Rigidbody2D RB_;
	public bool Active{
		get{ return _active;}
		set{ _active = value;
			if (MObject) {
				MObject.SetActive (value);

				if (tr)
					tr.Clear ();

			}
		}
	}
	public bool witinTarget; // si esta dentro de la zona de vaso de vidrio en la meta



	bool _active;
	float delta;
	Rigidbody2D rb;
	TrailRenderer tr;

	void Start () {
		//MObject = gameObject;
		rb = GetComponent<Rigidbody2D> ();
		tr = GetComponent<TrailRenderer> ();
	}

	private bool isChanging;
	[SerializeField] private float waterAmount = 1f;
	[Header("Drop Merge")]
	[SerializeField] private float mergeDuration = 0.3f;

	[SerializeField] private Ease mergeEase = Ease.InOutSine;

	private bool hasHit;
	private Vector3 originalScale;
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Collid") && !isChanging)
		{
			print("Sui");
			isChanging = true;
			StartCoroutine(ChangeToDiscrete());
		}
		if (hasHit)
			return;

		GlassWater glass =
			other.GetComponentInParent<GlassWater>();

		if (glass == null)
			return;

		hasHit = true;

		// Exact position where the water drop hits.
		Vector2 hitPosition = transform.position;

		// Add water and spawn splash at hit position.
		glass.AddWater(
			waterAmount,
			hitPosition
		);

		// Smooth drop merge.
		transform
			.DOScale(
				Vector3.zero,
				mergeDuration
			)
			.SetEase(mergeEase)
			.OnComplete(() =>
			{
				gameObject.SetActive(false);
			});
	}
	private void OnEnable()
	{
		hasHit = false;

		transform.localScale = originalScale;

		// Clean up any previous tween.
		transform.DOKill();
	}
	private IEnumerator ChangeToDiscrete()
	{
		yield return new WaitForSeconds(3f);

		RB_.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
		isChanging = false;
	}

	private IEnumerator ChangeCollisionDetection()
	{
		yield return new WaitForSeconds(3f);

		RB_.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
	}

	void Update () {

		if (Active == true) {

			VelocityLimiter ();

			if (LifeTime < 0)
				return;

			if (delta > LifeTime) {
				delta *= 0;
				Active = false;
			} else {
				delta += Time.deltaTime;
			}


		}

	}



	void VelocityLimiter()
	{
		
		
		Vector2 _vel = rb.linearVelocity;
		if (_vel.y < -8f) {
			_vel.y = -8f;
		}
		rb.linearVelocity = _vel;
	}

}
