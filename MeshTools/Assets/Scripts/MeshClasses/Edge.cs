using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlaneIntersection {NONE, POINT, PARALLEL};

/// <summary>
/// Edge of a mesh defined by two endpoints in an objects local space.
/// </summary>
public class Edge {
	private static int ID = 0;

	public int id;

	public Vector3 u;
	public Vector3 v;

	public MeshVertex node_u;
	public MeshVertex node_v;

	public Edge e_tail;
	public Edge e_head;

	public bool severed;
	public bool isTriangleRoot;

	public Edge(Vector3 a, Vector3 b){
		u = a;
		v = b;

		id = ID;
		ID++;
	}

	public float Length(){
		return Vector3.Distance(u, v);
	}

	public bool adjacent(Vector3 endpoint){
		return u == endpoint || v == endpoint;
	}

	public bool getPlaneIntersection(Transform plane, out Vector3 intersectPoint){
		//Plane Equation:  (p - p_0) * normal = 0
		Vector3 pNormal = plane.up;
		Vector3 p0 = plane.position;
		//Convert edge to vector representation:  d * edgeDir + u = p
		Vector3 edgeDir = u - v;
		float d = 0f;
		Vector3 intersection = Vector3.zero;
		intersectPoint = intersection;
		bool hasIntersection = false;
		//Check if line and plane intersect:
		if(Mathf.Abs(Vector3.Dot(edgeDir, pNormal)) > 0.0001f){
			d = Vector3.Dot((p0 - u), pNormal) / Vector3.Dot(edgeDir, pNormal);
			intersection = d * edgeDir + u;

			//Check if intersection lays on the line segment
			Vector3 dir_u = u - intersection;
			Vector3 dir_v = v - intersection;

			if(dir_u.magnitude <= Length() && dir_v.magnitude <= Length()){
				//Intersection point lies on our linesegment
				intersectPoint = intersection;
				hasIntersection = true;
			}
		}
		//Otherwise, they are parallel
		else{
			//Debug.Log("No intersection or parallel");
			hasIntersection = false;
		}
		severed = hasIntersection;
		return hasIntersection;
	}

	public bool getPlaneIntersection(Vector3 planeNormal, Vector3 planeCenter, out Vector3 intersectPoint){
		//Plane Equation:  (p - p_0) * normal = 0
		Vector3 pNormal = planeNormal;
		Vector3 p0 = planeCenter;
		//Convert edge to vector representation:  d * edgeDir + u = p
		Vector3 edgeDir = u - v;
		float d = 0f;
		Vector3 intersection = Vector3.zero;
		intersectPoint = intersection;
		bool hasIntersection = false;
		//Check if line and plane intersect:
		if(Mathf.Abs(Vector3.Dot(edgeDir, pNormal)) > 0.0001f){
			d = Vector3.Dot((p0 - u), pNormal) / Vector3.Dot(edgeDir, pNormal);
			intersection = d * edgeDir + u;
			
			//Check if intersection lays on the line segment
			Vector3 dir_u = u - intersection;
			Vector3 dir_v = v - intersection;
			
			if(dir_u.magnitude <= Length() && dir_v.magnitude <= Length()){
				//Intersection point lies on our linesegment
				intersectPoint = intersection;
				hasIntersection = true;
			}
		}
		//Otherwise, they are parallel
		else{
			//Debug.Log("No intersection or parallel");
			hasIntersection = false;
		}
		return hasIntersection;
	}

	public void attachMeshVertex(MeshVertex node){
		if(node_u == null){
			node_u = node;
		}
		else if(node_v == null){
			node_v = node;
		}
	}

	public void detachMeshVertices(){
		if(node_u != null){
			node_u.detachEdge(this);
		}
		if(node_v != null){
			node_v.detachEdge(this);
		}
	}

	public void setNode_u(MeshVertex n){
		node_u = n;
	}

	public void setNode_v(MeshVertex n){
		node_v = n;
	}

	public void drawDirection(){
		if(e_head != null && e_tail != null){
			Vector3 tail1 = Vector3.Lerp(e_tail.v, e_head.u, 0.75f);
			Debug.DrawLine(e_tail.v, tail1, Color.green, 5f);
			Debug.DrawLine(tail1, e_head.u, Color.red, 5f);
		}
		else{
			Vector3 tail1 = Vector3.Lerp(u, v, 0.75f);
			Debug.DrawLine(u, tail1, Color.yellow, 5f);
			Debug.DrawLine(tail1, v, Color.magenta, 5f);
		}
	}

	private void visualizeIntersection(Vector3 intersectionPoint){
		GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		p.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		p.transform.position = intersectionPoint;
		Debug.Log("intersection!");
		Debug.DrawLine(u, intersectionPoint, Color.blue, 30f);
	}


}
