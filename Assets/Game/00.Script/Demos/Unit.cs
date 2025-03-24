// using UnityEngine;
// using System.Collections;
// using Game._00.Script._05._Manager;
// using Unity.Mathematics;
//
// public class Unit : MonoBehaviour
// {
//
// 	const float minPathUpdateTime = .2f;
// 	const float pathUpdateMoveThreshold = .5f;
//
// 	public Transform target;
// 	[SerializeField] public float Speed = 20;
// 	[SerializeField] public float turnDistance = 5;
// 	[SerializeField] public float turnSpeed = 3;
// 	[SerializeField] public float stoppingDistance = 5; //how far from the finish that the object start slowing down
//
// 	private Path _path;
// 	private Test_RequestManager _requestManager;
// 	void Start()
// 	{
// 		_requestManager =  GameManager.Instance.Test_RequestManager;
// 		StartCoroutine(UpdatePath());
// 	}
//
// 	private void OnPathFound(Vector2[] waypoints, bool pathSuccessful, Transform target)
// 	{
// 		if (pathSuccessful)
// 		{
// 			_path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);
// 			StopCoroutine(FollowPath());
// 			StartCoroutine(FollowPath());
// 		}
//
// 	}
//
// 	IEnumerator UpdatePath()
// 	{
// 		if (Time.timeSinceLevelLoad < .3f)
// 		{
// 			yield return new WaitForSeconds(.3f);
// 		}
// 		_requestManager.RequestPath(new PathRequest( transform, target, OnPathFound));
// 		//Do not call update path every frame, only when object move far a bit from a certain threshold
// 		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
// 		Vector2 targetPosOld = target.position;
// 		while (true)
// 		{
// 			yield return new WaitForSeconds(minPathUpdateTime);
// 			if (((Vector2)target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold)
// 			{
// 				_requestManager.RequestPath(new PathRequest(transform, target, OnPathFound));
// 				targetPosOld = target.position;
// 			}
// 		}
// 	}
// 	
// 	/// <summary>
// 	/// Follow path, smoothly ease-out
// 	/// </summary>
// 	/// <returns></returns>
// 	IEnumerator FollowPath()
// 	{
// 		bool followingPath = true;
// 		int pathIndex = 0;
// 	    float speedPercent = 1;
// 	    
// 		while (followingPath)
// 		{
// 			//calculate slow down:
// 			Vector2 pos2D = new UnityEngine.Vector2(transform.position.x, transform.position.y);	
//
// 			// Check if we've crossed the current path boundary
// 			if (_path.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
// 			{
// 				if (pathIndex == _path.finishLineIndex)
// 				{
// 					followingPath = false;
// 					break;
// 				}
// 				pathIndex++;
// 				continue;
// 			}
//
// 			// Calculate the buildingDirection and distance to the next point
// 			Vector2 targetPoint = _path.lookPoints[pathIndex];
// 			Vector2 buildingDirection = (targetPoint - pos2D).normalized;
// 			float distanceToNextPoint = Vector2.Distance(pos2D, targetPoint);
//
// 			// If close to the next point, move to the next pathIndex
// 			if (distanceToNextPoint < 0.1f)
// 			{
// 				pathIndex++;
// 				if (pathIndex >= _path.lookPoints.Length)
// 				{
// 					followingPath = false;
// 					break;
// 				}
// 				continue;
// 			}
//
// 			if (followingPath) {
//
// 				if (pathIndex >= _path.slowDownIndex && stoppingDistance > 0) {
// 					speedPercent = Mathf.Clamp01 (_path.turnBoundaries [_path.finishLineIndex].DistanceFromPoint (pos2D) / stoppingDistance);
// 					if (speedPercent < 0.01f) {
// 						followingPath = false;
// 					}
// 				}
// 			}
//
// 			// Move smoothly towards the next point
// 			transform.Translate(buildingDirection * Speed * speedPercent * Time.deltaTime, Space.World);
//
// 			// Smooth rotation with -90 degree adjustment
// 			float angle = Mathf.Atan2(buildingDirection.y, buildingDirection.x) * Mathf.Rad2Deg - 90;
// 			Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
//
// 			// Optimize rotation Speed for smooth turning
// 			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
//
// 			yield return null; // Wait until next frame
// 		}
// 	}
//
// 	public void OnDrawGizmos()
// 	{
// 		if (_path != null)
// 		{
// 			_path.DrawWithGizmos();
// 		}
// 	}
// }