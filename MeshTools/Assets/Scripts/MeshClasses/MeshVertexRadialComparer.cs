using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MeshVertexRadialComparer : IComparer<MeshVertex>{
	
	public Vector3 localCutCenter;
	
	public MeshVertexRadialComparer(Vector3 localCenter){
		localCutCenter = localCenter;
	}
	
	public int Compare(MeshVertex a, MeshVertex b){
		return isClockwise(a.localPosition, b.localPosition, localCutCenter);
	}
	
	public static int isClockwise(Vector3 a, Vector3 b, Vector3 origin){
		if(a == b){
			return 0;
		}
		
		Vector3 offset_a = a - origin;
		Vector3 offset_b = b - origin;
		
		float angle1 = Mathf.Atan2(offset_a.x, offset_a.z);
		float angle2 = Mathf.Atan2(offset_b.x, offset_b.z);
		
		if(angle1 < angle2){
			return -1;
		}
		if(angle1 > angle2){
			return 1;
		}
		return (offset_a.sqrMagnitude < offset_b.sqrMagnitude) ? -1 : 1;
	}
}

