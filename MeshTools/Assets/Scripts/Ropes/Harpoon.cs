using UnityEngine;
using System.Collections;

public class Harpoon : MonoBehaviour {

	public RopeScript ropeController;
	public float launchForce;

	private bool launched;
	private bool ropeBuilt;
	private GameObject penetratedTarget;
	private Rigidbody rigidBody;
	// Use this for initialization
	void Start () {
		rigidBody = GetComponent<Rigidbody> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown (0) && !launched) {
			launched = true;
			rigidBody.useGravity = true;
			rigidBody.isKinematic = false;
			rigidBody.AddForce(transform.up * launchForce, ForceMode.Impulse);
		}
	}

	void OnCollisionEnter(Collision other){
		if (launched && !ropeBuilt) {
			ropeController.BuildRope();
			ropeBuilt = true;
			penetratedTarget = other.gameObject;
			rigidBody.isKinematic = true;
			rigidBody.useGravity = false;
		}
	}
}
