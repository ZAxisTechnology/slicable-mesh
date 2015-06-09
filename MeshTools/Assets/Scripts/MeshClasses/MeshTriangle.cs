using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Representation of a Mesh Triangle holding references to this triangles locations in 
/// the mesh's triangle array, the indices of the corner vertices and the vertices themselves.
/// </summary>
public class MeshTriangle {

	public static int ID = 0;
	public int id;

	/// <summary>
	/// Location in space of the triangle's first corner.
	/// </summary>
	public Vector3 v1;
	/// <summary>
	/// Location in space of the triangle's second corner.
	/// </summary>
	public Vector3 v2;
	/// <summary>
	/// Location in space of the triangle's third corner.
	/// </summary>
	public Vector3 v3;

	/// <summary>
	/// Index in mesh.triangles of first corner.
	/// </summary>
	public int tri_1;
	/// <summary>
	/// Index in mesh.triangles of second corner.
	/// </summary>
	public int tri_2;
	/// <summary>
	/// Index in mesh.triangles of third corner.
	/// </summary>
	public int tri_3;

	/// <summary>
	/// Index in mesh.vertices of first corner.
	/// </summary>
	public int v_i1;
	/// <summary>
	/// Index in mesh.vertices of second corner.
	/// </summary>
	public int v_i2;
	/// <summary>
	/// Index in mesh.vertices of third corner.
	/// </summary>
	public int v_i3;


	public Vector2 uv_1;
	public Vector2 uv_2;
	public Vector2 uv_3;

	public Vector3 cutV1;
	public Vector3 cutV2;

	public MeshTriangle(Vector3 a, Vector3 b, Vector3 c){
		v1 = a;
		v2 = b;
		v3 = c;

		id = ID;
		ID++;
	}

	/// <summary>
	/// Creates a new instance of a mesh triangle and sets up the indices of the corners.
	/// </summary>
	/// <param name="a">The first vector3 corner.</param>
	/// <param name="b">The second vector3 corner.</param>
	/// <param name="c">The third vector3 corner.</param>
	/// <param name="mesh">The Mesh that contains these triangles.</param>
	public MeshTriangle(Vector3 a, Vector3 b, Vector3 c, Mesh mesh){
		v1 = a;
		v2 = b;
		v3 = c;

		List<Vector3> vertList = new List<Vector3>();
		vertList.AddRange(mesh.vertices);

		List<int> trisList = new List<int>();
		trisList.AddRange(mesh.triangles);

		v_i1 = vertList.IndexOf(v1);
		v_i2 = vertList.IndexOf(v2);
		v_i3 = vertList.IndexOf(v3);

		tri_1 = trisList.IndexOf(v_i1);
		tri_2 = trisList.IndexOf(v_i2);
		tri_3 = trisList.IndexOf(v_i3);

		id = ID;
		ID++;

	}

	public bool containsVertex(Vector3 v){
		return (v == v1 || v == v2 || v == v3);
	}

	public bool getPlaneIntersection(Vector3 planeNormal, Vector3 planeCenter, out MeshTriangle[] newTris){
		bool severed = false;

		Vector3 intersect1 = Vector3.zero;
		PlaneIntersection e1_result = getEdgeIntersection (v1, v2, planeCenter, planeNormal, out intersect1);

		Vector3 intersect2 = Vector3.zero;
		PlaneIntersection e2_result = getEdgeIntersection (v2, v3, planeCenter, planeNormal, out intersect2);

		Vector3 intersect3 = Vector3.zero;
		PlaneIntersection e3_result = getEdgeIntersection (v3, v1, planeCenter, planeNormal, out intersect3);

		newTris = new MeshTriangle[3];

		//Check if there was any intersection at all
		if (e1_result == PlaneIntersection.NONE && e2_result == PlaneIntersection.NONE && e3_result == PlaneIntersection.NONE) {
			return false;
		}

		severed = true;

		MeshTriangle T1 = new MeshTriangle (v1, v2, v3);
		MeshTriangle T2 = new MeshTriangle (v2, v3, v1);
		MeshTriangle T3 = new MeshTriangle (v3, v1, v2);

		Vector2[] t1_uvs = new Vector2[3];
		Vector2[] t2_uvs = new Vector2[3];
		Vector2[] t3_uvs = new Vector2[3];

		//Otherwise there are 3 Cases:
		//e1 & e2 was cut
		if (e1_result == PlaneIntersection.POINT && e2_result == PlaneIntersection.POINT) {
			T1 = new MeshTriangle(v1, intersect1, v3);
			T2 = new MeshTriangle(intersect1, v2, intersect2);
			T3 = new MeshTriangle(intersect2, v3, intersect1);
			//Get UV positions of intersect points along cut edges
			Vector2 i1_uv = Vector2.Lerp(uv_1, uv_2, Vector3.Distance(v1, intersect1) / Vector3.Distance(v1, v2));
			Vector2 i2_uv = Vector2.Lerp(uv_2, uv_3, Vector3.Distance(v2, intersect2) / Vector3.Distance(v2, v3));
			//Set UV coordinates for new triangles
			t1_uvs = new Vector2[]{uv_1, i1_uv, uv_3};
			t2_uvs = new Vector2[]{i1_uv, uv_2, i2_uv};
			t3_uvs = new Vector2[]{i2_uv, uv_3, i1_uv};
			//Assign cutV1 and cutV2
			cutV1 = intersect1;
			cutV2 = intersect2;
		}
		//e1 & e3 was cut
		else if (e1_result == PlaneIntersection.POINT && e3_result == PlaneIntersection.POINT) {
			T1 = new MeshTriangle(v1, intersect1, intersect3);
			T2 = new MeshTriangle(intersect1, v2, v3);
			T3 = new MeshTriangle(intersect3, intersect1, v3);

			Vector2 i1_uv = Vector2.Lerp(uv_1, uv_2, Vector3.Distance(v1, intersect1) / Vector3.Distance(v1, v2));
			Vector2 i3_uv = Vector2.Lerp(uv_3, uv_1, Vector3.Distance(v3, intersect3) / Vector3.Distance(v3, v1));

			t1_uvs = new Vector2[]{uv_1, i1_uv, i3_uv};
			t2_uvs = new Vector2[]{i1_uv, uv_2, uv_3};
			t3_uvs = new Vector2[]{i3_uv, i1_uv, uv_3};
			//Assign cutV1 and cutV2
			cutV1 = intersect1;
			cutV2 = intersect3;
		}
		//e2 & e3 was cut
		else if (e2_result == PlaneIntersection.POINT && e3_result == PlaneIntersection.POINT) {
			T1 = new MeshTriangle(v1, v2, intersect3);
			T2 = new MeshTriangle(v2, intersect2, intersect3);
			T3 = new MeshTriangle(intersect2, v3, intersect3);

			Vector2 i2_uv = Vector2.Lerp(uv_2, uv_3, Vector3.Distance(v2, intersect2) / Vector3.Distance(v2, v3));
			Vector2 i3_uv = Vector2.Lerp(uv_3, uv_1, Vector3.Distance(v3, intersect3) / Vector3.Distance(v3, v1));

			t1_uvs = new Vector2[]{uv_1, uv_2, i3_uv};
			t2_uvs = new Vector2[]{uv_2, i2_uv, i3_uv};
			t3_uvs = new Vector2[]{i2_uv, uv_3, i3_uv};
			//Assign cutV1 and cutV2
			cutV1 = intersect2;
			cutV2 = intersect3;
		}

		T1.uv_1 = t1_uvs [0];
		T1.uv_2 = t1_uvs [1];
		T1.uv_3 = t1_uvs [2];

		T2.uv_1 = t2_uvs [0];
		T2.uv_2 = t2_uvs [1];
		T2.uv_3 = t2_uvs [2];

		T3.uv_1 = t3_uvs [0];
		T3.uv_2 = t3_uvs [1];
		T3.uv_3 = t3_uvs [2];

		newTris [0] = T1;
		newTris [1] = T2;
		newTris [2] = T3;

		return severed;
	}

	public PlaneIntersection getEdgeIntersection(Vector3 a, Vector3 b, Vector3 planeCenter, Vector3 planeNormal, out Vector3 intersect){
		Vector3 edgeDir = a - b;
		float d = 0f;
		Vector3 intersection = Vector3.zero;
		intersect = intersection;
		PlaneIntersection result = PlaneIntersection.NONE;
		float Length = Vector3.Distance (a, b);
		//Check if line and plane intersect:
		if(Mathf.Abs(Vector3.Dot(edgeDir, planeNormal)) > 0.0001f){
			d = Vector3.Dot((planeCenter - a), planeNormal) / Vector3.Dot(edgeDir, planeNormal);
			intersection = d * edgeDir + a;
			
			//Check if intersection lays on the line segment
			Vector3 dir_u = a - intersection;
			Vector3 dir_v = b - intersection;
			
			if(dir_u.magnitude <= Length && dir_v.magnitude <= Length){
				//Intersection point lies on our linesegment
				intersect = intersection;
				result = PlaneIntersection.POINT;
			}
		}
		//Otherwise, they are parallel
		else{
			//Debug.Log("No intersection or parallel");
			result = PlaneIntersection.NONE;
		}
		return result;
	}


	public Vector3 getPlaneNormal(){
		Vector3 cross1 = v2 - v1;
		Vector3 cross2 = v3 - v1;
		Vector3 normal = Vector3.Cross (cross1, cross2).normalized;
		return normal;
	}

	public Vector3 getBarycenter(){
		Vector3 barycenter = (v1 + v2 + v3) / 3f;
		return barycenter;
	}

	public void DrawTriangle(Color col){
		Debug.DrawLine (v1, v2, col, 5f);
		Debug.DrawLine (v2, v3, col, 5f);
		Debug.DrawLine (v3, v1, col, 5f);
	}



}
