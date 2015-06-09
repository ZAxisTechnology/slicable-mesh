using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Compares Vectors for radial sorting in XZ plane.
/// </summary>
public class ClockwiseComparer : IComparer<Vector3> {

	public Vector3 localCutCenter;
	public Transform cutPlaneTransform;
	
	public ClockwiseComparer(Vector3 cutCenter, Transform cutPlane){
		localCutCenter = cutPlane.InverseTransformPoint(cutCenter);
		cutPlaneTransform = cutPlane;
	}
	
	public int Compare(Vector3 a, Vector3 b){
		return isClockwise(a, b, localCutCenter, cutPlaneTransform);
	}
	
	public static int isClockwise(Vector3 a, Vector3 b, Vector3 origin, Transform plane){
		a = plane.InverseTransformPoint(a);
		b = plane.InverseTransformPoint(b);

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
