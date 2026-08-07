using System.Collections;
using System.Collections.Generic;
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

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Collid") && !isChanging)
		{
			print("Sui");
			isChanging = true;
			StartCoroutine(ChangeToDiscrete());
		}
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
