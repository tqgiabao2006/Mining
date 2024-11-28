using System.Collections;
using Game._00.Script._05._Manager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game._00.Script.NewPathFinding
{
    public abstract class NewUnitBase : MonoBehaviour
    {
        public float speed = 5f;
        private NewPathRequestManager _pathRequestManager;
        public void FollowPath(Vector3 startPos, Vector3 endPos)
        {
            StartCoroutine(ProcessPath(startPos, endPos));
        }
        private IEnumerator ProcessPath(Vector3 startPos, Vector3 endPos)
        {
            yield return new WaitForSeconds(0.05f);
            _pathRequestManager = GameManager.Instance.NewPathRequestManager;
            Vector3[] waypoints =_pathRequestManager.GetPathWaypoints(startPos, endPos);
            // Check for null or empty waypoints
            if (waypoints == null || waypoints.Length == 0)
            {
                yield break;
            }

            int curIndex = 0;
            while (curIndex < waypoints.Length)
            {
                Vector3 targetWaypoint = waypoints[curIndex];
                while (Vector3.Distance(transform.position, targetWaypoint) > 0.1f)
                {
                    Vector3 direction = (targetWaypoint - transform.position).normalized;
                    transform.Translate(direction * (speed * Time.deltaTime), Space.World);
                    yield return new WaitForFixedUpdate();                
                }

                curIndex++;
            }

        }
    }
}