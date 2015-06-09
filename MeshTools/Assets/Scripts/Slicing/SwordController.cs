using UnityEngine;
using System.Collections;

public class SwordController : MonoBehaviour {

	public Transform sword;
	public Animator sliceAnim;
	public float sliceSpeed;
	public float maxSwingAngle;

	public float minX;
	public float maxX;
	public float minY;
	public float maxY;

	private Vector3 mouseScreenpoint;
	private Vector3 swordPivotScreenPoint;
	private float swordAngle;
	private Vector3 screenAxisLeft;
	private bool slicing;
	private bool forSwing;
	private bool backSwing;

	Quaternion normalRot;
	Quaternion swingRot;
	float angle;

	// Use this for initialization
	void Start () {

		Vector3 screenLeft = new Vector3(0f, Screen.height / 2f, 0f);
		swordPivotScreenPoint = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
		screenAxisLeft = screenLeft - swordPivotScreenPoint;
	}
	
	// Update is called once per frame
	void Update () {
		mouseScreenpoint = Camera.main.ScreenToViewportPoint(Input.mousePosition);
		swordAngle = (1f - mouseScreenpoint.x) * 180f;
		Quaternion targetRot = Quaternion.Euler(0f, (swordAngle / 2f - 45f), swordAngle);
		//sword.localRotation = Quaternion.Slerp(sword.localRotation, targetRot, sword.localRotation.eulerAngles.z / swordAngle);
		sword.localRotation = targetRot;
		Vector3 targetPos = new Vector3(minX + mouseScreenpoint.x, minY + mouseScreenpoint.y, sword.position.z);
		sword.position = targetPos;
		if(Input.GetMouseButtonDown(0)){
			//slicing = true;
			//normalRot = sword.localRotation;
			//swingRot = Quaternion.Euler(maxSwingAngle, 0f, normalRot.eulerAngles.z);
			//angle = 0f;
			sliceAnim.SetTrigger("Slice");
		}

	}

	private void slice(){
		Debug.Log("angle: " + angle);
		if(sword.localRotation.eulerAngles.x < maxSwingAngle){
			angle += Time.deltaTime * sliceSpeed;
			sword.localRotation = Quaternion.Slerp(normalRot, swingRot, angle);

		}
		else if(angle < 2f * maxSwingAngle){
			angle += Time.deltaTime * sliceSpeed;
			sword.localRotation = Quaternion.Slerp(swingRot, normalRot, (angle) / (2f * maxSwingAngle));

		}
		else{
			slicing = false;
		}
	}

}
