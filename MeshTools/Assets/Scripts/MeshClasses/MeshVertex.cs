using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MeshVertex {
	private static int ID = 0;

	public int id;

	public Vector3 position;
	public Vector3 localPosition;
	public List<Edge> adjacentEdges;

	public Vector3 localCutCenter;

	private List<Vector3> sharedVertices;
	private Dictionary<Vector3, int> vertsToTrisMap;

	public MeshVertex(Vector3 pos){
		position = pos;
		adjacentEdges = new List<Edge>();
		sharedVertices = new List<Vector3>();
		vertsToTrisMap = new Dictionary<Vector3, int>();

		sharedVertices.Add(pos);

		id = ID;
		ID++;
	}


	public bool sharesVertex(Vector3 alias){
		return sharedVertices.Contains(alias);
	}

	public void attachEdge(Edge e){
		adjacentEdges.Add(e);

		//Debug.Log("Mesh Vertex " + id + " has " + adjacentEdges.Count + " attached edges!");
	}

	public void detachEdge(Edge e){
		adjacentEdges.Remove(e);
	}

	public void addSharedVertex(Vector3 v){
		sharedVertices.Add(v);
	}

	public void addSharedVertices(List<Vector3> aliases){
		sharedVertices.AddRange(aliases);
	}

	public void connectToTriangle(Vector3 sharedVertex, int triangleIndex){
		vertsToTrisMap.Add(sharedVertex, triangleIndex);
	}

	public void visualizeEdges(){
		foreach(Edge e in adjacentEdges){
			Debug.DrawLine(e.u, e.v, Color.cyan, 3f);
		}
		Debug.Log("Number of adjacent edges: " + adjacentEdges.Count);
	}
	

	private float ccw(MeshVertex o, MeshVertex a, MeshVertex b){
		return (a.localPosition.x - o.localPosition.x) * (b.localPosition.z - o.localPosition.z) - (a.localPosition.z - o.localPosition.z) * (b.localPosition.x - o.localPosition.x);
	}
}
