// using System.Collections;
// using System.Collections.Generic;
// using Game._00.Script._05._Manager;
// using UnityEngine;
//
// public abstract class TestUnitBase : MonoBehaviour
// {
// 	const float minPathUpdateTime = .2f;
// 	const float pathUpdateMoveThreshold = .5f;
//
// 	public float speed = 20;
// 	public float turnSpeed = 3;
// 	public float turnDst = 5;
// 	public float stoppingDst = 10;
//
// 	Path path;
//
// 	
// 	public void StartUpdatePath(Transform target)
// 	{
// 		StartCoroutine (UpdatePath (target));
//
// 	}
//
// 	public void OnPathFound(Vector3[] waypoints, bool pathSuccessful) {
// 		if (pathSuccessful) {
// 			// path = new Path(waypoints, transform.position, turnDst, stoppingDst);
//
// 			StopCoroutine("FollowPath");
// 			StartCoroutine("FollowPath");
// 		}
// 	}
//
// 	IEnumerator UpdatePath(Transform target) {
//
// 		if (Time.timeSinceLevelLoad < .3f) {
// 			yield return new WaitForSeconds (.3f);
// 		}
// 		Test_RequestManager.RequestPath(transform.position, target.position, OnPathFound);
//
// 		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
// 		Vector3 targetPosOld = target.position;
//
// 		while (true) {
// 			yield return new WaitForSeconds (minPathUpdateTime);
// 			if ((target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
// 				Test_RequestManager.RequestPath (transform.position, target.position, OnPathFound);
// 				targetPosOld = target.position;
// 			}
// 		}
// 	}
//
// 	IEnumerator FollowPath() 
// 	{
//
// 		// bool followingPath = true;
// 		// int pathIndex = 0;
// 		// // transform.LookAt(path.lookPoints [0]);
// 		//
// 		// float speedPercent = 1;
// 		//
// 		// while (followingPath) {
// 		// 	Vector2 pos2D = new Vector2 (transform.position.x, transform.position.y);
// 		// 	while (path.turnBoundaries [pathIndex].HasCrossedLine(pos2D)) {
// 		// 		if (pathIndex == path.finishLineIndex) {
// 		// 			followingPath = false;
// 		// 			break;
// 		// 		} else {
// 		// 			pathIndex++;
// 		// 		}
// 		// 	}
// 		//
// 		// 	if (followingPath) {
// 		//
// 		// 		if (pathIndex >= path.slowDownIndex && stoppingDst > 0) {
// 		// 			speedPercent = Mathf.Clamp01 (path.turnBoundaries [path.finishLineIndex].DistanceFromPoint (pos2D) / stoppingDst);
// 		// 			if (speedPercent < 0.01f) {
// 		// 				followingPath = false;
// 		// 			}
// 		// 		}
// 		//
// 		// 		// Quaternion targetRotation = Quaternion.LookRotation (path.lookPoints [pathIndex] - transform.position);
// 		// 		// transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
// 		// 		transform.Translate (Vector2.up * Time.deltaTime * Speed * speedPercent, Space.Self);
// 		// 	}
//
// 			yield return null;
//
// 		
// 	}
// 	
// 	public void OnDrawGizmos() {
// 		if (path != null) {
// 			path.DrawWithGizmos ();
// 		}
// 	}
// }
