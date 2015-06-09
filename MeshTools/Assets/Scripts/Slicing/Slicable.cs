using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Slicable : MonoBehaviour {

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
	private float startTimer;

	// Use this for initialization
	void Start () {
		timesCut++;
		mesh = GetComponent<MeshFilter>().mesh;
		meshCollider = GetComponent<MeshCollider>();
		mesh.MarkDynamic();
		meshVerts.Clear();
		meshVerts.AddRange(mesh.vertices);
		if(timesCut >= 1){
			checkTriangles();
		}
		//meshCollider.sharedMesh = null;
		//meshCollider.sharedMesh = mesh;
		meshEdges = initializeMeshEdges(meshVerts, mesh);


		//StartCoroutine("drawTriangleNormals");
	}
	
	void OnTriggerEnter(Collider other){
		if(other.gameObject.tag == "Slicer" && !cutStarted && timesCut < maxCuts){
			cutPlane = other.transform;
			cutStarted = true;
			cutFinished = false;

			//Freeze any motion while cut is calculated
			//GetComponent<Rigidbody>().isKinematic = true;

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
		if(slicedChunk != null && cutFinished){
			GetComponent<Rigidbody>().AddForce(-cutPlane.up, ForceMode.Impulse);
			slicedChunk.GetComponent<Rigidbody>().AddForce(cutPlane.up, ForceMode.Impulse);
		}

	}

	public IEnumerator CutMesh(){
		startTimer = Time.realtimeSinceStartup;
		List<Vector3> meshVs = meshVerts;
		Transform plane = cutPlane;
		List<Vector3> cutsetA = new List<Vector3>();
		List<Vector3> cutsetB = new List<Vector3>();

		List<Vector3> cutVerts = new List<Vector3>();
		List<Edge> cutEdges = new List<Edge>();
		List<Edge> destroyedEdges = new List<Edge>();

		Dictionary<Vector3, Vector2> cutVertToEdgeIndexMap = new Dictionary<Vector3, Vector2>();


		#region Generate Vertices laying on cut edges
		foreach(Edge e in meshEdges){
			Vector3 intersectPoint = Vector3.zero;
			if(e.getPlaneIntersection(transform.InverseTransformDirection(cutPlane.up), transform.InverseTransformPoint(cutPlane.position), out intersectPoint)){
				cutVerts.Add(intersectPoint);
				//Make new edge from e.u to e.intersection and e.v to e.intersection, then mark destroyed edge for removal
				Edge u_i = new Edge(e.u, intersectPoint);
				Edge v_i = new Edge(e.v, intersectPoint);
				cutEdges.Add(u_i);
				cutEdges.Add(v_i);
				destroyedEdges.Add(e);
				if(!cutVertToEdgeIndexMap.ContainsKey(intersectPoint)){
					cutVertToEdgeIndexMap.Add(intersectPoint, new Vector2((float)cutEdges.IndexOf(u_i), (float)cutEdges.IndexOf(v_i)));
				}
			}
		}

		if(cutVerts.Count < 3){
			yield break;
		}

		Vector3 cutCenter = getBarycentricPoint(cutVerts);
		//Sort cutVerts radially about cutCenter
		Vector3[] sortedCutVerts = sortPointsClockwise(plane, cutVerts, cutCenter);
		//Copy order of sortedCutVerts into list cutVerts
		for(int i = 0; i < sortedCutVerts.Length; i++){
			cutVerts[i] = sortedCutVerts[i];
		}
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
//		#region Reassign Edges to Cutsets
//		List<Edge> edges_A = new List<Edge>();
//		List<Edge> edges_B = new List<Edge>();
//		//Remove destroyed edges
//		for(int i = 0; i < destroyedEdges.Count; i++){
//			meshEdges.Remove(destroyedEdges[i]);
//		}
//		meshEdges.AddRange(cutEdges);
//		//Add edges to appropriate edge set
//		foreach(Edge e in meshEdges){
//			if(cutsetA.Contains(e.u) || cutsetA.Contains(e.v)){
//				//Debug.DrawLine(transform.TransformPoint(e.u), transform.TransformPoint(e.v), Color.green, 10f);
//				edges_A.Add(e);
//			}
//			else if(cutsetB.Contains(e.u) || cutsetB.Contains(e.v)){
//				//Debug.DrawLine(transform.TransformPoint(e.u), transform.TransformPoint(e.v), Color.blue, 10f);
//				edges_B.Add(e);
//			}
//		}
//		#endregion
//		yield return null;
		#region Build and triangulate the cut surface
		//Add all cutverts to cutsetA and cutsetB along with the cutCenter
		cutsetA.AddRange(cutVerts);
		cutsetB.AddRange(cutVerts);
		cutsetA.Add(cutCenter);
		cutsetB.Add(cutCenter);

		List<Edge> perimeterEdges = new List<Edge>();
		//Build the perimeter edges of the cutsurface
		for(int i = 0; i < sortedCutVerts.Length; i++){
			Edge e = new Edge(sortedCutVerts[i], sortedCutVerts[(i + 1) % sortedCutVerts.Length]);
			perimeterEdges.Add(e);
		}
		//Each edge on the cutSurface's perimeter will produce a new triangle
		int[] cutSurfTris_A = new int[3 * perimeterEdges.Count];
		int[] cutSurfTris_B = new int[3 * perimeterEdges.Count];
		int t = 0;
		for(int i = 0; i < perimeterEdges.Count; i++){
			cutSurfTris_A[t] = cutsetA.IndexOf(perimeterEdges[i].u);
			cutSurfTris_A[t + 1] = cutsetA.IndexOf(perimeterEdges[i].v);
			cutSurfTris_A[t + 2] = cutsetA.IndexOf(cutCenter);

			cutSurfTris_B[t + 2] = cutsetB.IndexOf(perimeterEdges[i].u);
			cutSurfTris_B[t + 1] = cutsetB.IndexOf(perimeterEdges[i].v);
			cutSurfTris_B[t] = cutsetB.IndexOf(cutCenter);

			t += 3;
		}
		#endregion  cutSurfTris for A and B now hold the indices of the cutVerts in the respective cutsets

		yield return null;
		#region Re-triangulate the vertices near the cutSurface in cutVertsA and B
		//For the Top Verts: Go through the perimeterEdges and find an edge in cutEdges_A that contains e.u
		//Once found, store that edge's other endpoint as u_cutEdge.v
		//Next, find the edge in cutEdges_A that cntains e.v
		//Once found, store that edges other endpoint as v_cutEdge.v
		//If u_cutEdge.v is the same as v_cutEdge.v, then we have a triangle.  Otherwise, we have a quad and we can make two new triangles.
		List<int> tris_A = new List<int>();
		List<int> tris_B = new List<int>();
		for(int i = 0; i < perimeterEdges.Count; i++){
			//Each Perimeter edge has 2 points of a new triangle with its third point the endpoint of a cutEdge
			//Store the indices in cutEdges of the edges connected to this edge's u vertex
			int u_idx1 = (int)cutVertToEdgeIndexMap[perimeterEdges[i].u].x;
			int u_idx2 = (int)cutVertToEdgeIndexMap[perimeterEdges[i].u].y;

			int v_idx1 = (int)cutVertToEdgeIndexMap[perimeterEdges[i].v].x;
			int v_idx2 = (int)cutVertToEdgeIndexMap[perimeterEdges[i].v].y;

			//One of these is in edges_A and the other is in edges_B
			Edge connected_u1 = cutEdges[u_idx1];
			Edge connected_u2 = cutEdges[u_idx2];

			Edge connected_v1 = cutEdges[v_idx1];
			Edge connected_v2 = cutEdges[v_idx2];

			Vector3 vert_a1 = Vector3.zero;
			Vector3 vert_a2 = Vector3.zero;

			Vector3 vert_b1 = Vector3.zero;
			Vector3 vert_b2 = Vector3.zero;

			//We know that the above two edges .v point is a cutvert, so check if its .u point is in the top or bottom cutset
			if(cutsetA.Contains(connected_u1.u)){
				//connected_u1 is in the top cutset and connected_u2 is in the bottom
				vert_a1 = connected_u1.u;
				vert_b1 = connected_u2.u;
			}
			else if(cutsetB.Contains(connected_u1.u)){
				//connected_u2 is in the top cutset and connected_u1 is in the bottom
				vert_a1 = connected_u2.u;
				vert_b1 = connected_u1.u;
			}

			//Now do the same for this perimeter edge's other endpoint
			if(cutsetA.Contains(connected_v1.u)){
				//connected_v1 is in the top cutset and connected_v2 is in the bottom
				vert_a2 = connected_v1.u;
				vert_b2 = connected_v2.u;
			}
			else if(cutsetB.Contains(connected_v1.u)){
				//connected_v2 is in the top cutset and connected_v1 is in the bottom
				vert_a2 = connected_v2.u;
				vert_b2 = connected_v1.u;
			}

			//Now check if the two verts from cutsetA are the same
			if(vert_a1 == vert_a2){
				//Then this perimeter edge's two endpoints form a triangle with vert_a1
				tris_A.Add(cutsetA.IndexOf(vert_a1));
				tris_A.Add(cutsetA.IndexOf(perimeterEdges[i].v));
				tris_A.Add(cutsetA.IndexOf(perimeterEdges[i].u));
			}
			else{
				//We must make two new triangles from this edges endpoints and vert_a1 and vert_a2
				tris_A.Add(cutsetA.IndexOf(vert_a1));
				tris_A.Add(cutsetA.IndexOf(perimeterEdges[i].v));
				tris_A.Add(cutsetA.IndexOf(perimeterEdges[i].u));

				tris_A.Add(cutsetA.IndexOf(perimeterEdges[i].v));
				tris_A.Add(cutsetA.IndexOf(vert_a1));
				tris_A.Add(cutsetA.IndexOf(vert_a2));
			}
			//Now do the same for cutsetB
			if(vert_b1 == vert_b2){
				//Then this perimeter edge's two endpoints form a triangle with vert_a1
				tris_B.Add(cutsetB.IndexOf(perimeterEdges[i].u));
				tris_B.Add(cutsetB.IndexOf(perimeterEdges[i].v));
				tris_B.Add(cutsetB.IndexOf(vert_b1));
			}
			else{
				//We must make two new triangles from this edges endpoints and vert_a1 and vert_a2
				tris_B.Add(cutsetB.IndexOf(perimeterEdges[i].u));
				tris_B.Add(cutsetB.IndexOf(perimeterEdges[i].v));
				tris_B.Add(cutsetB.IndexOf(vert_b1));

				tris_B.Add(cutsetB.IndexOf(vert_b2));
				tris_B.Add(cutsetB.IndexOf(vert_b1));
				tris_B.Add(cutsetB.IndexOf(perimeterEdges[i].v));

			}
		}

		#endregion
		//drawNewTriangulation(tris_A, tris_B, cutsetA, cutsetB);
		yield return null;
		#region Consolidate triangle and vertex arrays
		//Grab all triangles that were unaffected by the cut
		int[] meshTris = mesh.triangles;
		for(int i = 0; i < meshTris.Length; i += 3){
			Vector3 t1 = meshVerts[meshTris[i + 0]];
			Vector3 t2 = meshVerts[meshTris[i + 1]];
			Vector3 t3 = meshVerts[meshTris[i + 2]];

			bool a1 = cutsetA.Contains(t1);
			bool a2 = cutsetA.Contains(t2);
			bool a3 = cutsetA.Contains(t3);

			bool b1 = cutsetB.Contains(t1);
			bool b2 = cutsetB.Contains(t2);
			bool b3 = cutsetB.Contains(t3);
			//If cutsetA contains all of these vertices, then this triangle was not affected by the cut, so add their indices to tris_A
			if(a1 && a2 && a3){
				tris_A.Add(cutsetA.IndexOf(t1));
				tris_A.Add(cutsetA.IndexOf(t2));
				tris_A.Add(cutsetA.IndexOf(t3));
			}
			//Otherwise, if cutsetB contains all of these vertices, add their indices to tris_B
			else if(b1 && b2 && b3){
				tris_B.Add(cutsetB.IndexOf(t1));
				tris_B.Add(cutsetB.IndexOf(t2));
				tris_B.Add(cutsetB.IndexOf(t3));
			}
			//Otherwise, this triangle was cut, so dont copy it into the cutsets triangle list

		}
		//Add all the triangles from the cut surface
		tris_A.AddRange(cutSurfTris_A);
		tris_B.AddRange(cutSurfTris_B);
		#endregion  tris_A now contains ALL of the triangles for cutsetA and similarly for tris_B


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

		yield return null;
		#region Create a new mesh and gameObject for the separated cutsetB and reassign this mesh to cutsetA
		//checkTriangles();
		Mesh mesh_B = new Mesh();
		mesh_B.vertices = cutsetB.ToArray();
		mesh_B.triangles = tris_B.ToArray();
		mesh_B.RecalculateBounds();
		mesh_B.RecalculateNormals();

		mesh.Clear();
		mesh.vertices = cutsetA.ToArray();
		mesh.triangles = tris_A.ToArray();

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();

		GameObject cutPiece = (GameObject) Instantiate(gameObject, transform.position, transform.rotation);
		cutPiece.GetComponent<MeshFilter>().mesh = mesh_B;

		slicedChunk = cutPiece;
		meshEdges.Clear();
		Start();
		moveAwayFromCut();
		//meshEdges = initializeMeshEdges(meshVerts, mesh);
		#endregion
		Debug.Log("(Slicable)Time taken for cut: " + (Time.realtimeSinceStartup - startTimer));

	}


	public void checkTriangles(){
		Mesh m = GetComponent<MeshFilter>().mesh;
		//Calc Barycenter of entire mesh
		List<Vector3> mVerts = new List<Vector3>();
		mVerts.AddRange(m.vertices);
		Vector3 meshCentroid = getBarycentricPoint(mVerts);
		//Loop through triangles and calc its barycenter and Normal Vector in clockwise order
		int[] mTris = m.triangles;
		bool fixTris = false;
		for(int i = 0; i < mTris.Length; i += 3){
			Vector3 t1 = mVerts[mTris[i + 0]];
			Vector3 t2 = mVerts[mTris[i + 1]];
			Vector3 t3 = mVerts[mTris[i + 2]];

			Vector3 triCentroid = (t1 + t2 + t3) / 3f;
			Vector3 relTriPos = meshCentroid - triCentroid;
			Vector3 triEdge1 = t2 - t1;
			Vector3 triEdge2 = t3 - t1;
			Vector3 triNormal = Vector3.Cross(triEdge1, triEdge2);
			//Calc Dot Product of normal and rel pos to meshCentroid
			//if(Vector3.Dot(transform.TransformDirection(triNormal), transform.TransformPoint(relTriPos)) > 0){
			if(Vector3.Dot(triNormal, relTriPos) > 0){
				//if dot product is > 0, reverse the triangle
				//Debug.DrawRay(transform.TransformPoint(meshCentroid), transform.TransformPoint(triCentroid - meshCentroid), Color.magenta, 1f);
				int temp = mTris[i + 1];
				mTris[i + 1] = mTris[i + 2];
				mTris[i + 2] = temp;
				fixTris = true;
			}
		}
		if(fixTris){
			m.vertices = mVerts.ToArray();
			m.triangles = mTris;
			m.RecalculateNormals();
		}
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

	private void drawCutsets(List<Vector3> cutsetA, List<Vector3> cutsetB){
		foreach(Vector3 a in cutsetA){
			Debug.DrawLine(transform.position, transform.TransformPoint(a), Color.green, 10f);
		}
		foreach(Vector3 b in cutsetB){
			Debug.DrawLine(transform.position, transform.TransformPoint(b), Color.red, 10f);
		}
	}

	private void drawNewTriangulation(List<int> tris_a, List<int> tris_b, List<Vector3> cutsetA, List<Vector3> cutsetB){
		for(int i = 0; i < tris_a.Count; i += 3){
			Debug.DrawLine(transform.TransformPoint(cutsetA[tris_a[i + 0]]), transform.TransformPoint(cutsetA[tris_a[i + 1]]), Color.green, 15f);
			Debug.DrawLine(transform.TransformPoint(cutsetA[tris_a[i + 1]]), transform.TransformPoint(cutsetA[tris_a[i + 2]]), Color.green, 15f);
			Debug.DrawLine(transform.TransformPoint(cutsetA[tris_a[i + 2]]), transform.TransformPoint(cutsetA[tris_a[i + 0]]), Color.green, 15f);
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
