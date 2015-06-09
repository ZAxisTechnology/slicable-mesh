using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlicableMesh : MonoBehaviour {

	public static int maxCuts = 4;
	
	public Transform cutPlane;
	public int timesCut = -1;
	public bool conserveMass;
	
	
	private bool cutStarted;
	private bool cutFinished;
	
	
	
	private Mesh mesh;
	private MeshCollider meshCollider;
	private List<Vector3> meshVerts = new List<Vector3>();
	private List<MeshTriangle> meshTris;
	
	private GameObject slicedChunk;

	private float startTimer;
	private bool cuttable;



	// Use this for initialization
	void Start () {
		mesh = GetComponent<MeshFilter>().mesh;
		meshCollider = GetComponent<MeshCollider>();
		mesh.MarkDynamic();
		meshVerts.Clear();
		meshVerts.AddRange(mesh.vertices);

		//meshCollider.sharedMesh = null;
		//meshCollider.sharedMesh = mesh;
		meshTris = initializeMesh(meshVerts, mesh);


	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.T)) {
			foreach (MeshTriangle mT in meshTris) {
				mT.DrawTriangle(Color.red);
			}
		}
	}

	void OnTriggerEnter(Collider other){
		if(other.gameObject.tag == "Slicer" && timesCut < maxCuts){
			cutPlane = other.transform;
			cutStarted = true;
			cutFinished = false;
			
			//Freeze any motion while cut is calculated
			//GetComponent<Rigidbody>().isKinematic = true;
			//StartCoroutine("startCutDelay");
			StartCoroutine("CutMesh");
			
		}
	}
	
	void OnTriggerExit(){
		if(cutStarted & !cutFinished){
			//GetComponent<Rigidbody>().isKinematic = false;
			cutStarted = false;
			cutFinished = true;
			//moveAwayFromCut();
		}
	}

	private void moveAwayFromCut(){
		if(slicedChunk != null){
			GetComponent<Rigidbody>().AddForce(-cutPlane.up, ForceMode.Impulse);
			slicedChunk.GetComponent<Rigidbody>().AddForce(cutPlane.up, ForceMode.Impulse);
		}
		
	}

	public IEnumerator CutMesh(){
		List<MeshTriangle> destroyedTris = new List<MeshTriangle> ();
		List<MeshTriangle> cutTris = new List<MeshTriangle> ();
		List<Vector3> cutVerts = new List<Vector3> ();
		List<Vector3> cutsetA = new List<Vector3> ();
		List<Vector3> cutsetB = new List<Vector3> ();
		//List<Vector2> uvs_A = new List<Vector2> ();
		//List<Vector2> uvs_B = new List<Vector2> ();

		Transform plane = cutPlane;
		//Check mesh tris for plane intersection and add to list of severed triangles
		for (int i = 0; i < meshTris.Count; i++) {
			MeshTriangle[] newTrianulation = new MeshTriangle[3];
			if(meshTris[i].getPlaneIntersection(transform.InverseTransformDirection(cutPlane.up), transform.InverseTransformPoint(cutPlane.position), out newTrianulation)){
				cutTris.AddRange(newTrianulation);
				destroyedTris.Add(meshTris[i]);
				cutVerts.Add(meshTris[i].cutV1);
				cutVerts.Add(meshTris[i].cutV2);
			}
		}
		if (destroyedTris.Count < 3) {
			yield break;
		}
		yield return null;

		Vector3 cutCenter = getBarycentricPoint(cutVerts);
		//Sort cutVerts radially about cutCenter
		Vector3[] sortedCutVerts = sortPointsClockwise(plane, cutVerts, cutCenter);
		//Copy order of sortedCutVerts into list cutVerts
		cutVerts = new List<Vector3>(sortedCutVerts);

		#region Separate verts laying above and below the cut plane, respectively
		foreach(Vector3 v in meshVerts){
			if(Vector3.Dot(transform.TransformPoint(cutCenter) - transform.TransformPoint(v), cutPlane.up) < 0){
				//v is below cutplane
				cutsetB.Add(v);
			}
			else{ 
				//v is above the plane
				cutsetA.Add(v); 
			}
		}
		cutsetA.AddRange(cutVerts);
		cutsetB.AddRange(cutVerts);
		#endregion

		yield return null;


		#region Retringulate Mesh and set the UV maps
		List<int> tris_A = new List<int>();
		List<int> tris_B = new List<int>();
		Vector2[] uvs_a = new Vector2[cutsetA.Count + cutVerts.Count + 1];
		Vector2[] uvs_b = new Vector2[cutsetB.Count + cutVerts.Count + 1];

		//Remove all severed triangles from list
		foreach(MeshTriangle destroyedTri in destroyedTris){
			meshTris.Remove(destroyedTri);
		}
		meshTris.AddRange(cutTris);
		//Create triangle index lists
		foreach(MeshTriangle t in meshTris){
			bool aContains = cutsetA.Contains(t.v1);
			aContains = aContains && cutsetA.Contains(t.v2);
			aContains = aContains && cutsetA.Contains(t.v3);
			//If this triangle belongs to cutsetA...
			if(aContains){
				//Add its triangle indices to the list for A
				tris_A.Add(cutsetA.IndexOf(t.v1));
				tris_A.Add(cutsetA.IndexOf(t.v2));
				tris_A.Add(cutsetA.IndexOf(t.v3));
				//Set uvs
				uvs_a[cutsetA.IndexOf(t.v1)] = t.uv_1;
				uvs_a[cutsetA.IndexOf(t.v2)] = t.uv_2;
				uvs_a[cutsetA.IndexOf(t.v3)] = t.uv_3;
			}
			//Else, this triangle belongs to cutsetB
			else{
				tris_B.Add(cutsetB.IndexOf(t.v1));
				tris_B.Add(cutsetB.IndexOf(t.v2));
				tris_B.Add(cutsetB.IndexOf(t.v3));
				//Set uvs
				uvs_b[cutsetB.IndexOf(t.v1)] = t.uv_1;
				uvs_b[cutsetB.IndexOf(t.v2)] = t.uv_2;
				uvs_b[cutsetB.IndexOf(t.v3)] = t.uv_3;
			}
		}
		#endregion

		#region Set the UV coordinates for the cutSurface
		List<Vector2> cutSurfUVs = new List<Vector2>();
		for(int i = 0; i < sortedCutVerts.Length; i++){
			Vector3 uv3 = cutPlane.InverseTransformDirection(Vector3.ProjectOnPlane((sortedCutVerts[i] - cutCenter), cutPlane.up));
			float x = Mathf.Clamp01(uv3.x + 0.5f);
			float y = Mathf.Clamp01(uv3.z + 0.5f);
			cutSurfUVs.Add(new Vector2(x, y));
		}
		#endregion

		#region Build and triangulate the cut surface
		//Add all cutverts to cutsetA and cutsetB along with the cutCenter
		cutsetA.Add(cutCenter);
		cutsetB.Add(cutCenter);

		uvs_a[cutsetA.IndexOf(cutCenter)] = new Vector2(0.5f, 0.5f);
		uvs_b[cutsetB.IndexOf(cutCenter)] = new Vector2(0.5f, 0.5f);

		//Each edge on the cutSurface's perimeter will produce a new triangle
		for(int i = 0; i < cutVerts.Count; i++){
			if(Vector3.Distance(cutVerts[i], cutVerts[(i + 1) % cutVerts.Count]) == 0){
				continue;
			}
			tris_A.Add(cutsetA.Count + i);
			tris_A.Add(cutsetA.Count + (i + 1) % cutVerts.Count);
			tris_A.Add(cutsetA.IndexOf(cutCenter));
			
			tris_B.Add(cutsetB.IndexOf(cutCenter));
			tris_B.Add(cutsetB.Count + (i + 1) % cutVerts.Count);
			tris_B.Add(cutsetB.Count + i);

			uvs_a[cutsetA.Count + i] = cutSurfUVs[i];
			uvs_b[cutsetB.Count + i] = cutSurfUVs[i];
		}
		cutsetA.AddRange(cutVerts);
		cutsetB.AddRange(cutVerts);
		#endregion  cutSurfTris for A and B now hold the indices of the cutVerts in the respective cutsets

		yield return null;

		#region Create a new mesh and gameObject for the separated cutsetB and reassign this mesh to cutsetA
		Mesh mesh_B = new Mesh();
		mesh_B.vertices = cutsetB.ToArray();
		mesh_B.triangles = tris_B.ToArray();
		mesh_B.uv = uvs_b;

		mesh_B.RecalculateBounds();
		mesh_B.RecalculateNormals();
		
		mesh.Clear();
		mesh.vertices = cutsetA.ToArray();
		mesh.triangles = tris_A.ToArray();
		mesh.uv = uvs_a;
		
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		
		GameObject cutPiece = (GameObject) Instantiate(gameObject, transform.position, transform.rotation);
		cutPiece.GetComponent<MeshFilter>().mesh.Clear();
		cutPiece.GetComponent<MeshFilter>().mesh = mesh_B;
		//cutPiece.SendMessage("initialize");
		
		slicedChunk = cutPiece;
		Start();
		moveAwayFromCut();
		//Debug.Log("(SlicableObject)Time taken for cut: " + (Time.realtimeSinceStartup - startTimer));
		#endregion 
	}

	/// <summary>
	/// Gets the barycentric coordinate for the convex set of points
	/// </summary>
	/// <returns>The barycentric point.</returns>
	/// <param name="points">Points.</param>
	private Vector3 getBarycentricPoint(List<Vector3> points){
		Vector3 centroid = Vector3.zero;
		foreach(Vector3 p in points){
			centroid += p;
		}
		centroid = centroid / points.Count;
		return centroid;
	}



	/// <summary>
	/// Constructs Triangle map for attached mesh.
	/// </summary>
	public List<MeshTriangle> initializeMesh(List<Vector3> meshVerts, Mesh mesh){
		List<MeshTriangle> m_tris = new List<MeshTriangle>();
		//Set up edges
		int[] tris = mesh.triangles;
		for(int i = 0; i < tris.Length; i += 3){
			MeshTriangle T = new MeshTriangle(meshVerts[tris[i + 0]], meshVerts[tris[i + 1]], meshVerts[tris[i + 2]]);
			//Setup UV coords
			T.uv_1 = mesh.uv[meshVerts.IndexOf(meshVerts[tris[i + 0]])];
			T.uv_2 = mesh.uv[meshVerts.IndexOf(meshVerts[tris[i + 1]])];
			T.uv_3 = mesh.uv[meshVerts.IndexOf(meshVerts[tris[i + 2]])];
			m_tris.Add(T);

		}
		return m_tris;
	}

	/// <summary>
	/// Sorts the given points by their clockwise position about the cutCenter w.r.t. the orientation of the plane.
	/// </summary>
	/// <returns>The points sorted clockwise around the center relative to the plane.</returns>
	/// <param name="plane">The plane on which all of the cutPoints lay.</param>
	/// <param name="cutPoints">Cut points.</param>
	/// <param name="globalCutCenter">Barycenter of the coplanar points.</param>
	private Vector3[] sortPointsClockwise(Transform plane, List<Vector3> cutPoints, Vector3 globalCutCenter){
		Vector3[] sortedVerts = new Vector3[cutPoints.Count];
		for(int i = 0; i < cutPoints.Count; i++){
			sortedVerts[i] = cutPoints[i];
		}
		System.Array.Sort<Vector3>(sortedVerts, new ClockwiseComparer(globalCutCenter, plane));
		return sortedVerts;
	}

	private List<Vector3> removeDuplicates(List<Vector3> vList){
		//Remove dupes
		List<Vector3> duplicates = new List<Vector3> ();

		for (int i = 0; i < vList.Count; i++) {
			if(Vector3.Distance(vList[i], vList[(i + 1) % vList.Count]) == 0){
				duplicates.Add(vList[i]);
			}
		}
		foreach (Vector3 v in duplicates) {
			vList.Remove(v);
		}

		return vList;
	}

}
