using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlicableObject : MonoBehaviour {

	
	public int maxCuts = 4;
	
	public Transform cutPlane;
	public int timesCut = -1;
	public bool conserveMass;
	
	
	private bool cutStarted;
	private bool cutFinished;
	
	
	
	private Mesh mesh;
	private MeshCollider meshCollider;
	private List<Vector3> meshVerts = new List<Vector3>();
	private List<Edge> meshEdges;
	
	private GameObject slicedChunk;

	private int t;
	private float startTimer;
	private bool cuttable;
	
	// Use this for initialization
	void Start () {
		timesCut++;
		mesh = GetComponent<MeshFilter>().mesh;
		meshCollider = GetComponent<MeshCollider>();
		mesh.MarkDynamic();
		meshVerts.Clear();
		meshVerts.AddRange(mesh.vertices);
		StartCoroutine ("startCutDelay");
		//meshCollider.sharedMesh = null;
		//meshCollider.sharedMesh = mesh;
		meshEdges = initializeMeshEdges(meshVerts, mesh);
		
		
		//StartCoroutine("drawTriangleNormals");
	}

	public void initialize(){
		timesCut++;
		mesh = GetComponent<MeshFilter>().mesh;
		meshCollider = GetComponent<MeshCollider>();
		mesh.MarkDynamic();
		meshVerts.Clear();
		meshVerts.AddRange(mesh.vertices);
		//meshCollider.sharedMesh = null;
		//meshCollider.sharedMesh = mesh;
		meshEdges = initializeMeshEdges(meshVerts, mesh);
	}

	void Update(){
		if(Input.GetKeyDown(KeyCode.E)){
			//drawDirectedEdges();
		}
		if(Input.GetKeyDown(KeyCode.C)){
			//StartCoroutine("CutMesh");
		}

		if (Input.GetKeyDown (KeyCode.N)) {
			//drawNormals();
			showNormal();
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
		startTimer = Time.realtimeSinceStartup;

		List<Vector3> meshVs = meshVerts;
		Transform plane = cutPlane;
		List<Vector3> cutsetA = new List<Vector3> ();
		List<Vector3> cutsetB = new List<Vector3> ();
		
		List<Vector3> cutVerts = new List<Vector3> ();
		List<Edge> cutEdges = new List<Edge> ();
		List<Edge> destroyedEdges = new List<Edge> ();

		List<Vector2> uvs_A = new List<Vector2> ();
		List<Vector2> uvs_B = new List<Vector2> ();

		Debug.Log ("Vertex Count: " + mesh.vertexCount);

		#region Generate Vertices laying on cut edges
		for(int i = 0; i < meshEdges.Count; i++){
			Vector3 intersect = Vector3.zero;
			//Grab the adjacent triangle edges
			Edge e = meshEdges[i];
			Edge head = e.e_head; 
			Edge tail = e.e_tail;
			
			if(e.getPlaneIntersection(transform.InverseTransformDirection(cutPlane.up), transform.InverseTransformPoint(cutPlane.position), out intersect)){
				if(!cutVerts.Contains(intersect)){
					//Avoid duplicating vertices
					cutVerts.Add(intersect);
				}
				destroyedEdges.Add(e);
				//Split this edge into to new edges
				Edge e_u = new Edge(e.u, intersect);
				Edge e_v = new Edge(intersect, e.v);
				//Make two opposite pointing edges to complete the triangles
				Edge u_h = new Edge(e_u.v, tail.u);
				Edge v_t = new Edge(head.v, e_v.u);
				//Reconnect our new edges with our head and tail to form two new triangles replacing the old one
				tail.e_head = e_u;
				tail.e_tail = u_h;
				
				head.e_tail = e_v;
				head.e_head = v_t;
				
				e_u.e_tail = tail;
				e_u.e_head = u_h;
				
				e_v.e_head = head;
				e_v.e_tail = v_t;
				
				u_h.e_tail = e_u;
				u_h.e_head = tail;
				
				v_t.e_head = e_v;
				v_t.e_tail = head;
				
				cutEdges.Add(e_u);
				cutEdges.Add(e_v);
				cutEdges.Add(v_t);
				cutEdges.Add(u_h);
			}
		}

		if(destroyedEdges.Count < 3){
			yield break;
		}
		Vector3 cutCenter = getBarycentricPoint(cutVerts);
		//Sort cutVerts radially about cutCenter
		Vector3[] sortedCutVerts = sortPointsClockwise(plane, cutVerts, cutCenter);
		//Copy order of sortedCutVerts into list cutVerts
		cutVerts = new List<Vector3>(sortedCutVerts);
		#endregion
		yield return null;
		#region Separate verts laying above and below the cut plane, respectively
		foreach(Vector3 v in meshVs){
			if(Vector3.Dot(transform.TransformPoint(cutCenter) - transform.TransformPoint(v), cutPlane.up) < 0){
				//v is below cutplane
				cutsetB.Add(v);
			}
			else{ 
				//v is above the plane
				cutsetA.Add(v); 
			}
		}
		#endregion
		yield return null;

		//Remove destroyed edges
		for(int i = 0; i < destroyedEdges.Count; i++){
			meshEdges.Remove(destroyedEdges[i]);
		}
		meshEdges.AddRange(cutEdges);

		#region ReTriangulate mesh
		cutsetA.AddRange(cutVerts);
		cutsetB.AddRange(cutVerts);
		List<int> tris_A = new List<int>();
		List<int> tris_B = new List<int>();
		bool[] edgeChecklist = new bool[meshEdges.Count];
		for(int i = 0; i < meshEdges.Count; i++){
			if(!edgeChecklist[i]){
				Edge e = meshEdges[i];
				bool a_contains = cutsetA.Contains(e.u);
				a_contains = a_contains && cutsetA.Contains(e.v);
				a_contains = a_contains && cutsetA.Contains(e.e_head.v);
				edgeChecklist[i] = true;
				if(a_contains){
					//CutsetA contains this triangle
					tris_A.Add(cutsetA.IndexOf(e.u));
					tris_A.Add(cutsetA.IndexOf(e.v));
					tris_A.Add(cutsetA.IndexOf(e.e_head.v));
				}
				else{
					//CutsetB contains this triangle
					tris_B.Add(cutsetB.IndexOf(e.u));
					tris_B.Add(cutsetB.IndexOf(e.v));
					tris_B.Add(cutsetB.IndexOf(e.e_head.v));
				}
				edgeChecklist[meshEdges.IndexOf(e.e_head)] = true;
				edgeChecklist[meshEdges.IndexOf(e.e_tail)] = true;
			}

		}

		#endregion
		yield return null;

		#region Build and triangulate the cut surface
		//Add all cutverts to cutsetA and cutsetB along with the cutCenter
		cutsetA.Add(cutCenter);
		cutsetB.Add(cutCenter);
		//Each edge on the cutSurface's perimeter will produce a new triangle
		int[] cutSurfTris_A = new int[3 * cutVerts.Count];
		int[] cutSurfTris_B = new int[3 * cutVerts.Count];
		int t = 0;
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

		}
		cutsetA.AddRange(cutVerts);
		cutsetB.AddRange(cutVerts);
		yield return null;
		#endregion  cutSurfTris for A and B now hold the indices of the cutVerts in the respective cutsets

		#region Set the UV coordinates for the cutSurface
		//TODO: Store generated UVs in correct place in list
		List<Vector2> cutSurfUVs = new List<Vector2>();
		cutSurfUVs.Add(new Vector2(0.5f, 0.5f));
		float angleOffset_x = 0f;
		float angleOffset_y = 0f;
		for(int i = 0; i < sortedCutVerts.Length; i++){
			Vector3 uv3 = cutPlane.InverseTransformDirection(Vector3.ProjectOnPlane((sortedCutVerts[i] - cutCenter), cutPlane.up));
			float x = Mathf.Clamp01(uv3.x + 0.5f);
			float y = Mathf.Clamp01(uv3.z + 0.5f);
			//Debug.DrawLine(new Vector3(0.5f, 0f, 0.5f), new Vector3(x, 0f, y), Color.cyan, 10f);
			cutSurfUVs.Add(new Vector2(x, y));
		}
		#endregion

		#region Set UV coordinates of meshes
		Vector2[] uvs_a = new Vector2[cutsetA.Count];
		Vector2[] uvs_b = new Vector2[cutsetB.Count];

//		for(int i = 0; i < mesh.vertexCount; i++){
//
//		}
		int cutIndex = 1;
		Debug.Log("uv a count : " + uvs_a.Length);
		//Debug.Log("cutset a count : " + cutsetA.Count);
		Debug.Log("cut surf uvs count : " + cutSurfUVs.Count);
		for(int i = 0; i < cutsetA.Count; i++){
			Vector3 v_a = cutsetA[i];
			if(meshVs.Contains(v_a)){
				//Copy original uvs
				uvs_a[i] = mesh.uv[meshVs.IndexOf(v_a)];
			}
			else if(cutVerts.Contains(v_a) && cutIndex < cutSurfUVs.Count){
				Debug.Log("Cut index: " + cutIndex);
				//Otherwise, this is a newly created vertex
				uvs_a[i] = cutSurfUVs[cutIndex];
				cutIndex++;
				//Debug.Log("Index of cut vertex: " + i);
			}
		}
		cutIndex = 1;
		for(int i = 0; i < cutsetB.Count; i++){
			Vector3 v_b = cutsetB[i];
			if(meshVs.Contains(v_b)){
				//Copy original uvs
				uvs_b[i] = mesh.uv[meshVs.IndexOf(v_b)];
			}
			else{
				//Otherwise, this is a newly created vertex
				//uvs_b[cutsetB.IndexOf(v_b)] = cutSurfUVs[cutVerts.IndexOf(v_b)];
				
			}
		}
		#endregion

	
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
		cutPiece.GetComponent<MeshFilter>().mesh = mesh_B;
		//cutPiece.SendMessage("initialize");
		
		slicedChunk = cutPiece;
		//drawNewTriangulation(tris_A, tris_B, cutsetA, cutsetB);
		meshEdges.Clear();
		//initialize();
		Start();
		moveAwayFromCut();
		Debug.Log("(SlicableObject)Time taken for cut: " + (Time.realtimeSinceStartup - startTimer));
		#endregion
		
		
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
	
	/// <summary>
	/// Constructs edge connectivity map for attached mesh.
	/// </summary>
	public List<Edge> initializeMeshEdges(List<Vector3> meshVerts, Mesh mesh){
		List<Edge> meshEdges = new List<Edge>();
		//Set up edges
		int[] tris = mesh.triangles;
		for(int i = 0; i < tris.Length; i += 3){
			Edge e1 = new Edge(meshVerts[tris[i + 0]], meshVerts[tris[i + 1]]);
			meshEdges.Add(e1);
			
			Edge e2 = new Edge(meshVerts[tris[i + 1]], meshVerts[tris[i + 2]]);
			meshEdges.Add(e2);
			
			Edge e3 = new Edge(meshVerts[tris[i + 2]], meshVerts[tris[i + 0]]);
			meshEdges.Add(e3);
			
			e1.isTriangleRoot = true;
			e1.e_head = e2;
			e1.e_tail = e3;
			
			e2.e_head = e3;
			e2.e_tail = e1;
			
			e3.e_head = e1;
			e3.e_tail = e2;

		}
		return meshEdges;
	}

	private void drawDirectedEdges(){
		Edge e = meshEdges[t % meshEdges.Count];
		Edge e_t = e.e_tail;
		Edge e_h = e.e_head;

		e.drawDirection();
		e_t.drawDirection();
		e_h.drawDirection();

		t++;
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

	private IEnumerator startCutDelay(){
		//cuttable = false;
		yield return new WaitForSeconds (1f);
		cuttable = true;
	}
	
	private void drawCutsets(List<Vector3> cutsetA, List<Vector3> cutsetB){
		foreach(Vector3 a in cutsetA){
			Debug.DrawLine(transform.position, transform.TransformPoint(a), Color.green, 10f);
		}
		foreach(Vector3 b in cutsetB){
			Debug.DrawLine(transform.position, transform.TransformPoint(b), Color.red, 10f);
		}
	}

	private void drawNormals(){
		for(int i = 0; i < mesh.vertexCount; i++){
			Debug.DrawRay(transform.TransformPoint(mesh.vertices[i]), transform.TransformDirection(mesh.normals[i]), Color.red, 10f);
		}
		Debug.Log ("number of vertices: " + mesh.vertexCount);
	}

	private void showNormal(){
		Debug.Log ("Vert Index: " + t);
		Debug.DrawRay(transform.TransformPoint(mesh.vertices[t]), transform.TransformDirection(mesh.normals[t]), Color.red, 3f);
		Debug.DrawLine(Vector3.zero, transform.TransformPoint(meshVerts[t]), Color.green, 3f);
		Debug.Log ("Normal Length: " + mesh.normals [t].magnitude);
		t = (t + 1) % mesh.vertexCount;
	}
	
	private void drawNewTriangulation(List<int> tris_a, List<int> tris_b, List<Vector3> cutsetA, List<Vector3> cutsetB){
		for(int i = 0; i < tris_a.Count; i += 3){
			Debug.DrawLine(transform.TransformPoint(cutsetA[tris_a[i + 0]]), transform.TransformPoint(cutsetA[tris_a[i + 1]]), Color.red, 15f);
			Debug.DrawLine(transform.TransformPoint(cutsetA[tris_a[i + 1]]), transform.TransformPoint(cutsetA[tris_a[i + 2]]), Color.red, 15f);
			Debug.DrawLine(transform.TransformPoint(cutsetA[tris_a[i + 2]]), transform.TransformPoint(cutsetA[tris_a[i + 0]]), Color.red, 15f);
		}
		for(int i = 0; i < tris_b.Count; i += 3){
			Debug.DrawLine(transform.TransformPoint(cutsetB[tris_b[i + 0]]), transform.TransformPoint(cutsetB[tris_b[i + 1]]), Color.blue, 15f);
			Debug.DrawLine(transform.TransformPoint(cutsetB[tris_b[i + 1]]), transform.TransformPoint(cutsetB[tris_b[i + 2]]), Color.blue, 15f);
			Debug.DrawLine(transform.TransformPoint(cutsetB[tris_b[i + 2]]), transform.TransformPoint(cutsetB[tris_b[i + 0]]), Color.blue, 15f);
		}
	}
	
	private void drawOrderedCutVerts(Vector3[] sortedCutVerts){
		for(int i = 0; i < sortedCutVerts.Length; i++){
			Debug.DrawLine(transform.TransformPoint(sortedCutVerts[i]), transform.TransformPoint(sortedCutVerts[(i + 1) % sortedCutVerts.Length]), Color.red, 10f);
		}
	}
}


