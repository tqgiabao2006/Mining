// using System.Collections;
// using Game._00.Script._05._Manager;
// using Game._00.Script.NewPathFinding;
// using UnityEngine;
//
// namespace Game._00.Script.Demos
// {
//     public abstract class UnitBase : MonoBehaviour
//     {
//         public float speed = 5f;
//         private PathRequestManager _pathRequestManager;
//         public void FollowPath(Vector3 startPos, Vector3 endPos)
//         {
//             StartCoroutine(ProcessPath(startPos, endPos));
//         }
//         private IEnumerator ProcessPath(Vector3 startPos, Vector3 endPos)
//         {
//             yield return new WaitForSeconds(0.05f);
//             _pathRequestManager = GameManager.Instance.PathRequestManager;
//             Vector3[] waypoints =_pathRequestManager.GetPathWaypoints(startPos, endPos);
//             // Check for null or empty waypoints
//             if (waypoints == null || waypoints.Length == 0)
//             {
//                 yield break;
//             }
//
//             int curIndex = 0;
//             while (curIndex < waypoints.Length)
//             {
//                 Vector3 targetWaypoint = waypoints[curIndex];
//                 while (Vector3.Distance(transform.position, targetWaypoint) > 0.1f)
//                 {
//                     Vector3 buildingDirection = (targetWaypoint - transform.position).normalized;
//                     transform.Translate(buildingDirection * (speed * Time.deltaTime), Space.World);
//                     yield return new WaitForFixedUpdate();                
//                 }
//
//                 curIndex++;
//             }
//
//         }
//     }
// }