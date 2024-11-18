using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Numerics;
using Game._00.Script._05._Manager;

public class PathRequestManager : MonoBehaviour
{
	//Testing
	public Transform target;
	
	private Queue<PathResult> results = new Queue<PathResult>();
	private GameManager _gameManager;
    private PathFinding _pathFinding;
	private void Start()
	{
		_gameManager = GameManager.Instance;
		_pathFinding = _gameManager.PathFinding;
	}

	void Update()
	{
		ProcessPathRequest(target);
	}

	/// <summary>
	/// Call in update
	/// </summary>
	/// <param name="target"></param>
	public void ProcessPathRequest(Transform target)
	{
		if (results.Count > 0)
		{
			int itemsInQueue = results.Count;
			lock (results)
			{
				for (int i = 0; i < itemsInQueue; i++)
				{
					PathResult result = results.Dequeue();
					result.callBack(result.path, result.success, target);
				}
			}
		}
	}

	public void RequestPath(PathRequest request)
	{
		ThreadStart threadStart = delegate { _pathFinding.FindPath(request, FinishedProcessingPath); };
		threadStart.Invoke();
	}

	public void FinishedProcessingPath(PathResult result)
	{
		lock (results)
		{
			results.Enqueue(result);
		}
	}
}


public struct PathResult
{
	public UnityEngine.Vector2[] path;
	public bool success;
	public Action<UnityEngine.Vector2[], bool,Transform> callBack;

	public PathResult(UnityEngine.Vector2[] path, bool success, Action<UnityEngine.Vector2[], bool, Transform> callBack)
	{
		this.path = path;
		this.success = success;
		this.callBack = callBack;
	}
}
public struct PathRequest 
{
	public UnityEngine.Vector2 pathStart;
	public UnityEngine.Vector2 pathEnd;
	public Action<UnityEngine.Vector2[], bool, Transform> callback;

	public PathRequest(UnityEngine.Vector2 _start, UnityEngine.Vector2 _end, Action<UnityEngine.Vector2[], bool, Transform> _callback) 
	{
		pathStart = _start;
		pathEnd = _end;
		callback = _callback;
	}
}