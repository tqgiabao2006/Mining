using Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS;
using Unity.Entities;
using Unity.Transforms;

[assembly: RegisterGenericComponentType(typeof(State))]
[assembly: RegisterGenericComponentType(typeof(Entity))]
[assembly: RegisterGenericComponentType(typeof(NextDestination))]
    
[assembly: RegisterGenericComponentType(typeof(Speed))]
[assembly: RegisterGenericComponentType(typeof(FollowPathData))]
[assembly: RegisterGenericComponentType(typeof(LocalTransform))]
    
[assembly: RegisterGenericComponentType(typeof(CanRun))]
[assembly: RegisterGenericComponentType(typeof(StopDistance))]
[assembly: RegisterGenericComponentType(typeof(ColliderBound))]
    
[assembly: RegisterGenericComponentType(typeof(ParkingLot))]
[assembly: RegisterGenericComponentType(typeof(IsParking))]
[assembly: RegisterGenericComponentType(typeof(ParkingData))]
namespace Game._00.Script._03.Traffic_System.Car_spawner_system.CarSpawner_ECS
{
    public readonly partial struct CarAspect: IAspect 
    {
        // Fundamental components
        public readonly RefRW<State> State;
        public readonly Entity Self;
        public readonly RefRW<NextDestination> NextDestination;

        // Follow path components
        public readonly RefRW<Speed> Speed;
        public readonly RefRW<FollowPathData> FollowPathData;
        public readonly RefRW<LocalTransform> LocalTransform;

        // Traffic simulation components
        public readonly RefRO<CanRun> CanRun;
        public readonly RefRO<StopDistance> StopDistance;
        public readonly RefRO<ColliderBound> ColliderBound;

        // Follow parking waypoints components
        public readonly RefRW<ParkingLot> ParkingLot;
        public readonly RefRW<IsParking> IsParking;
        public readonly RefRW<ParkingData> ParkingData;
        
        //Mining component
        public readonly RefRW<MiningTime> MiningTime;

        public bool CheckCanRun() => CanRun.ValueRO.Value;
    }
}