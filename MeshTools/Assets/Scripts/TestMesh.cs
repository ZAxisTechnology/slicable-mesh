using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestMesh : MonoBehaviour {

	Mesh mesh;
	public int t;

	// Use this for initialization
	void Start () {
		mesh = GetComponent<MeshFilter>().mesh;
		Vector3 t1 = new Vector3(0f, 1f, 0f);
		Vector3 t2 = new Vector3(1f, 0f, 1f);
		Vector3 t3 = new Vector3(1f, 0f, -1f);

		Vector3 t4 = new Vector3(0f, 1f, 0f);
		Vector3 t5 = new Vector3(-1f, 0f, -1f);
		Vector3 t6 = new Vector3(-1f, 0f, 1f);

		Vector3 t7 = new Vector3(0f, 1f, 0f);
		Vector3 t8 = new Vector3(1f, 0f, -1f);
		Vector3 t9 = new Vector3(-1f, 0f, -1f);

		Vector3 t10 = new Vector3(0f, 1f, 0f);
		Vector3 t11 = new Vector3(-1f, 0f, 1f);
		Vector3 t12 = new Vector3(1f, 0f, 1f);

		mesh.Clear();
		Vector3[] verts = new Vector3[]{t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12};
		int[] tris = new int[]{0, 2, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11};
		//System.Array.Reverse(tris);

		mesh.vertices = verts;
		mesh.triangles = tris;
		//mesh.RecalculateNormals();


		Debug.Log("t1" + verts[tris[0]]);

		Debug.Log("t2" + verts[tris[1]]);

		Debug.Log("t3" + verts[tris[2]]);
		t = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.T)){
			drawTriNormals(t);
			t = (t + 3) % mesh.triangles.Length;
		}
	}

	private void checkTriangles(){
		int[] tris = mesh.triangles;
		Vector3[] verts = mesh.vertices;
		Vector3 meshCentroid = getBarycentricPoint(new List<Vector3>(verts));

		for(int i = 0; i < tris.Length; i += 3){
			Vector3 t1 = verts[tris[i + 0]];
			Vector3 t2 = verts[tris[i + 1]];
			Vector3 t3 = verts[tris[i + 2]];
			
			Vector3 cross1 = t2 - t1;
			Vector3 cross2 = t3 - t1;
			
			Vector3 triCenter = (t1 + t2 + t3) / 3f;
			Vector3 triNormal = Vector3.Cross(cross1, cross2);
			Vector3 relTriPos = triCenter - meshCentroid;

			if(Vector3.Dot(triNormal, relTriPos) > 0){
				Debug.Log("Flipping triangle");
			}

			//Debug.DrawRay(transform.TransformPoint(triCenter), transform.TransformDirection(Vector3.Cross(cross1, cross2)), Color.green, 3f);
			//Debug.DrawRay(transform.TransformPoint(triCenter), transform.TransformDirection(relTriPos), Color.blue, 5f);
			//Debug.Log("World Space normal: " + transform.TransformPoint(Vector3.Cross(cross1, cross2)));
			//Debug.Log("Local Space normal: " + Vector3.Cross(cross1, cross2));
		}


	}

	private Vector3 getBarycentricPoint(List<Vector3> points){
		Vector3 centroid = Vector3.zero;
		foreach(Vector3 p in points){
			centroid += p;
		}
		centroid = centroid / points.Count;
		return centroid;
	}

	private void drawTriNormals(int i){
		int[] tris = mesh.triangles;
		Vector3[] verts = mesh.vertices;
		Vector3 meshCentroid = getBarycentricPoint(new List<Vector3>(verts));

		Vector3 t1 = verts[tris[i + 0]];
		Vector3 t2 = verts[tris[i + 1]];
		Vector3 t3 = verts[tris[i + 2]];
		
		Vector3 cross1 = t2 - t1;
		Vector3 cross2 = t3 - t1;

		Vector3 cross3 = t1 - t2;
		Vector3 cross4 = t3 - t2;
		
		Vector3 triCenter = (t1 + t2 + t3) / 3f;
		
		Vector3 triNormal1 = Vector3.Cross(cross1, cross2);


		//Vector from this triangles position to the centroid of the mesh
		Vector3 relTriPos = meshCentroid - triCenter;

		if(Vector3.Dot(triNormal1, relTriPos) > 0){
			Debug.Log("Flipping triangle");
			int temp = tris[i + 1];
			tris[i + 1] = tris[i + 2];
			tris[i + 2] = temp;

			mesh.triangles = tris;
			mesh.RecalculateNormals();
		}

		Debug.DrawRay(transform.TransformPoint(triCenter), transform.TransformDirection(triNormal1), Color.magenta, 3f);
		//Debug.DrawRay(transform.TransformPoint(triCenter), transform.TransformDirection(triNormal2), Color.red, 3f);
		//Debug.DrawRay(transform.TransformPoint(triCenter), transform.TransformDirection(triNormal3), Color.blue, 3f);

		Debug.DrawRay(transform.TransformPoint(triCenter), transform.TransformDirection(relTriPos), Color.black, 3f);

		//Draw points in order
		Debug.DrawLine(transform.TransformPoint(meshCentroid), transform.TransformPoint(t1), Color.red, 3f);
		Debug.Log("t1" + t1);
		Debug.DrawLine(transform.TransformPoint(meshCentroid), transform.TransformPoint(t2), Color.green, 3f);
		Debug.Log("t2" + t2);
		Debug.DrawLine(transform.TransformPoint(meshCentroid), transform.TransformPoint(t3), Color.blue, 3f);
		Debug.Log("t3" + t3);

//		Debug.DrawRay((triCenter), (triNormal), Color.green, 3f);
//		Debug.DrawRay((triCenter), (relTriPos), Color.blue, 5f);

		//Debug.Log("World Space normal: " + transform.TransformPoint(Vector3.Cross(cross1, cross2)));
		//Debug.Log("Local Space normal: " + Vector3.Cross(cross1, cross2));


	}
}
