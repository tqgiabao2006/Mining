using System;
using System.Collections.Generic;
using Game._00.Script._05._Manager;
using UnityEngine;

namespace Game._00.Script.NewPathFinding
{
    /// <summary>
    /// Working as a bridge from pathfinding, and unit base to try create new thread => optimize, decoupling
    /// </summary>
    public class PathRequestManager:MonoBehaviour
    {
        private PathFinding _pathFinding;
        private bool _isProcessingPath;
        private NewPathRequest _currentRequest;

        public void Initialize()
        {
            _pathFinding = GameManager.Instance.PathFinding;
        }

        public Vector3[] GetPathWaypoints(Vector3 startPos, Vector3 endPos)
        {
            NewPathRequest newPathRequest = new NewPathRequest(startPos, endPos);
            return _pathFinding.GetFuncFindPath()?.Invoke(newPathRequest);  
        }
    }

    public struct NewPathRequest
    {
        public Vector3 StartPos { get; }
        public Vector3 EndPos { get; }
        public NewPathRequest(Vector3 startPos, Vector3 endPos)
        {
            this.StartPos = startPos;
            this.EndPos = endPos;
        }
    }
}