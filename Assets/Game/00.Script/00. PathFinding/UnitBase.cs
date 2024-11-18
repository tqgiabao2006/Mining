using System.Collections;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;

public abstract class UnitBase:MonoBehaviour
{
	const float minPathUpdateTime = .2f;
	const float pathUpdateMoveThreshold = .5f;
	
	[SerializeField] protected float speed = 20;
	[SerializeField] protected float turnDistance = 5;
	[SerializeField] protected float turnSpeed = 3;
	[SerializeField] protected float stoppingDistance = 5; //how far from the finish that the object start slowing down

    
    protected Path _path;
    protected GameManager _gameManager;
    protected PathRequestManager _requestManager;
    protected PathFinding _pathFinding;
	void Start()
	{
		_gameManager = GameManager.Instance;
		_requestManager = _gameManager.PathRequestManager;
		_pathFinding = _gameManager.PathFinding;
	}

	protected void StartUpdatePath(Transform target)
	{
		StartCoroutine(UpdatePath(target));
	}
	
	protected IEnumerator UpdatePath(Transform target)
	{

		if (Time.timeSinceLevelLoad < .3f)
		{
			yield return new WaitForSeconds(.3f);
		}
		_requestManager.RequestPath(new PathRequest( transform.position, target.position, OnPathFound));
		//Do not call update path every frame, only when object move far a bit from a certain threshold
		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
		Vector2 targetPosOld = target.position;
		while (true)
		{
			yield return new WaitForSeconds(minPathUpdateTime);
			if (((Vector2)target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold)
			{
				_requestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
				targetPosOld = target.position;
			}
		}
	}
	
	protected void OnPathFound(Vector2[] waypoints, bool pathSuccessful, Transform target)
	{
		if (pathSuccessful)
		{
			_path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);
			StopCoroutine("FollowPath");
			StartCoroutine("FollowPath");
		}

	}
	
	protected IEnumerator FollowPath()
	{
		bool followingPath = true;
		int pathIndex = 0;
		float speedPercent = 1;
	    
		while (followingPath)
		{
			//calculate slow down:

			Vector2 pos2D = new UnityEngine.Vector2(transform.position.x, transform.position.y);	

			// Check if we've crossed the current path boundary
			if (_path.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
			{
				if (pathIndex == _path.finishLineIndex)
				{
					followingPath = false;
					break;
				}
				pathIndex++;
				continue;
			}

			// Calculate the direction and distance to the next point
			Vector2 targetPoint = _path.lookPoints[pathIndex];
			Vector2 direction = (targetPoint - pos2D).normalized;
			float distanceToNextPoint = Vector2.Distance(pos2D, targetPoint);

			// If close to the next point, move to the next pathIndex
			if (distanceToNextPoint < 0.1f)
			{
				pathIndex++;
				if (pathIndex >= _path.lookPoints.Length)
				{
					followingPath = false;
					break;
				}
				continue;
			}

			if (followingPath) {

				if (pathIndex >= _path.slowDownIndex && stoppingDistance > 0) {
					speedPercent = Mathf.Clamp01 (_path.turnBoundaries [_path.finishLineIndex].DistanceFromPoint (pos2D) / stoppingDistance);
					if (speedPercent < 0.01f) {
						followingPath = false;
					}
				}
			}

			// Move smoothly towards the next point
			transform.Translate(direction * speed * speedPercent * Time.deltaTime, Space.World);

			// Smooth rotation with -90 degree adjustment
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
			Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

			// Optimize rotation speed for smooth turning
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

			yield return null; // Wait until next frame
		}
	}

}
