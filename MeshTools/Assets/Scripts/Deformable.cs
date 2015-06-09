using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deformable : MonoBehaviour {

	public float deformRadius;
	public float minImpactForce;
	public float maxImpactForce;
	public float deformForce;
	public bool deformMeshCollider;
	public float maxDeformDistance;
	public float density;

	public bool useDefaultSettings;
	public bool subdivide;
	public int subdivisions;

	private List<Vector3> meshVerts = new List<Vector3>();
	private Mesh mesh;
	private MeshCollider meshCollider;

	private Dictionary<int, float> meshIndexDeformMap = new Dictionary<int, float>();

	// Use this for initialization
	void Start () {
		if(useDefaultSettings){
			deformRadius = 3f;
			minImpactForce = 0.01f;
			maxImpactForce = 3f;
			maxDeformDistance = 0.3f;
			density = 0.1f;
		}

		

		mesh = GetComponent<MeshFilter>().mesh;
		mesh.MarkDynamic();
		meshVerts.AddRange(mesh.vertices);

		if(subdivide){
			MeshHelper.Subdivide(mesh, 8);
			mesh = GetComponent<MeshFilter>().mesh;
			mesh.MarkDynamic();
			meshVerts.Clear();
			meshVerts.AddRange(mesh.vertices);
		}

		density = 1f + Mathf.Clamp01(density);

		if(GetComponent<MeshCollider>() == null){
			deformMeshCollider = false;
		}
		else{
			meshCollider = GetComponent<MeshCollider>();
		}
		//Initialize Deformation Map
		for(int i = 0; i < meshVerts.Count; i++){
			meshIndexDeformMap.Add(i, 0f);
		}
	}

	void Update(){
		if(Input.GetKeyDown(KeyCode.D)){
			//Draw Mesh deformation map
			foreach(Vector3 v in meshVerts){
				Color deformColor = Color.Lerp(Color.green, Color.red, meshIndexDeformMap[meshVerts.IndexOf(v)] / maxDeformDistance);
				Debug.DrawRay(transform.TransformPoint(v), transform.TransformDirection(mesh.normals[meshVerts.IndexOf(v)]), deformColor, 10f);

			}
		}
	}

	//Single Point Collision
	void OnCollisionEnter(Collision other){
		Vector3 hitNormal = Vector3.up;
		List<Vector3> nearbyMeshVs = new List<Vector3>();
		List<Vector3> unaffectedVerts = new List<Vector3>();
		List<int> meshVertIndices = new List<int>();
		
		if(other.relativeVelocity.magnitude < minImpactForce){
			return;
		}
		hitNormal = other.contacts[0].normal;
		//hitNormal = -transform.InverseTransformDirection(other.rigidbody.velocity.normalized);
		float farthest = 0f;
		//Get all mesh verts within distance from hit points
		foreach(Vector3 v in meshVerts){
			float d = Vector3.Distance(transform.TransformPoint(v), other.contacts[0].point);  
			if(d <= deformRadius){
				nearbyMeshVs.Add(v);
				meshVertIndices.Add(meshVerts.IndexOf(v));
				//Find the largest distance away from the hit center
				if(d > farthest){
					farthest = d;
				}
				//Debug.DrawLine(transform.TransformPoint(v), (hit.point - hit.normal), Color.blue, 100f);
				//Debug.DrawRay(hit.point, hit.normal, Color.blue, 100f);
			}
		}
		//Debug.Log("Num Nearby mesh vertices: " + nearbyMeshVs.Count);
		Debug.DrawRay(other.contacts[0].point, hitNormal, Color.white, 10f);
		deformForce = (other.relativeVelocity.magnitude * other.rigidbody.mass) / 10f;
		deformForce = Mathf.Clamp(deformForce, 0f, maxImpactForce) / (density * density);
		deformMesh(meshVertIndices, hitNormal, other.contacts[0].point, farthest);
	}

	private void deformMesh(List<int> hitVertIndices, Vector3 hitNorm, Vector3 hitCenter, float maximalDist){
		for(int i = 0; i < hitVertIndices.Count; i++){
			//Get distance of mesh vert to hit center
			float d = Vector3.Distance(transform.TransformPoint(meshVerts[hitVertIndices[i]]), hitCenter);
			//Calc the length of the overall mesh vertex displacement
			float displacement = Mathf.Clamp(deformForce / d, 0f, maxDeformDistance);
			//Check that the vertex hasnt been deformed too much
			if(meshIndexDeformMap[hitVertIndices[i]] + displacement <= maxDeformDistance){

				//Get world space coordinated of mesh vertex
				Vector3 worldPoint = transform.TransformPoint(meshVerts[hitVertIndices[i]]);
				//Calc direction of stretch applied to vertex towards hit center
				Vector3 stretch = Vector3.ClampMagnitude(((hitCenter - worldPoint) * d), d / maximalDist);
				Vector3 translation = Vector3.ClampMagnitude(hitNorm * displacement + stretch, displacement);
				meshVerts[hitVertIndices[i]] = transform.InverseTransformPoint(worldPoint + translation);
				Debug.DrawLine(worldPoint, transform.TransformPoint(meshVerts[hitVertIndices[i]]), Color.blue, 10f);
				meshIndexDeformMap[hitVertIndices[i]] += displacement;
			}
		}
		int[] tris = mesh.triangles;
		mesh.Clear();
		mesh.vertices = meshVerts.ToArray();
		mesh.triangles = tris;
		//mesh.UploadMeshData(false);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		if(deformMeshCollider){
			meshCollider.sharedMesh = null;
			meshCollider.sharedMesh = mesh;
		}
	}


	private void PrintVertexInfo(){
		
		Debug.Log("Vertex count: " + meshVerts.Count);
		int[] tris = mesh.triangles;
		for(int i = 0; i < tris.Length; i++){
			Debug.Log("Vertex index: " + tris[i] + " ||  " + "Vector: " + meshVerts[tris[i]]);
			Debug.DrawRay(meshVerts[tris[i]], mesh.normals[tris[i]], Color.red, 100f);
		}
	}
}

