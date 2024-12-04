// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class CurvePathCreator : MonoBehaviour {
//
//     [HideInInspector]
//     public CurvePath path;
//     
//     Vector2 defaultPos = Vector2.zero;
// 	public Color anchorCol = Color.red;
//     public Color controlCol = Color.white;
//     public Color segmentCol = Color.green;
//     public Color selectedSegmentCol = Color.yellow;
//     public float anchorDiameter = .1f;
//     public float controlDiameter = .075f;
//     public bool displayControlPoints = true;
//     public float curveWidth = 2f;
//     
//     public void CreatePath()
//     {
//         path = new CurvePath(this.transform.position);
//     }
//
//     void Reset()
//     {
//         CreatePath();
//     }
// }