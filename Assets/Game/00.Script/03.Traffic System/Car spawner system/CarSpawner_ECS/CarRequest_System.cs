using Game._00.Script._00.Manager.Observer;
using Game._00.Script._03.Traffic_System.PathFinding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
    public partial class CarRequest_System: SystemBase, IObserver
    {
        private PathRequestManager _pathRequestManager;
        protected override void OnUpdate()
        {
            if (_pathRequestManager == null)
            {
                _pathRequestManager = PathRequestManager.Instance;
            }
        }

        public void OnNotified(object data, string flag)
        {
            if (data is not DemandCarRequest || flag != NotificationFlags.DEMAND_CAR)
            {
                return;
            }

            DemandCarRequest demandCarRequest = (DemandCarRequest)data;
            EntityManager.SetComponentData(demandCarRequest.CarEntity,
                new FollowPathData()
                {
                    CurrentIndex =  0,
                    WaypointsBlob = CreateWaypointsBlob(demandCarRequest.Waypoints)
                });
            EntityManager.SetComponentData(demandCarRequest.CarEntity,
                new State()
                {
                    Value = CarState.FollowingPath
                });
            
        }
        
        /// <summary>
        /// Convert Vector3[] waypoints to BlobAsset more optimized for ECS and Job system
        /// </summary>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        public BlobAssetReference<BlobArray<float3>> CreateWaypointsBlob(Vector3[] waypoints)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<BlobArray<float3>>();
                var waypointArray = builder.Allocate(ref root, waypoints.Length);

                for (int i = 0; i < waypoints.Length; i++)
                {
                    waypointArray[i] = new float3(waypoints[i].x, waypoints[i].y, waypoints[i].z);
                }

                return builder.CreateBlobAssetReference<BlobArray<float3>>(Allocator.Persistent);
            }
        }
    }
}