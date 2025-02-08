// using System.Collections;
// using System.Collections.Generic;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Physics.Systems;
// using Unity.Rendering;
// using Unity.Transforms;
// using UnityEngine;
// using RaycastHit = Unity.Physics.RaycastHit;
//
// public class Test_ECS_RayCast : MonoBehaviour
// {
//     private class Baker : Baker<Test_ECS_RayCast>
//     {
//         public override void Bake(Test_ECS_RayCast authoring)
//         {
//             Entity entity = GetEntity(TransformUsageFlags.Renderable);
//             AddComponent(entity, new RayCastDirection()
//             {
//                 direction = new float3(1, 0,0 )
//
//             });
//         }
//     }
// }
//
// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
// partial struct RayCastTest : ISystem
// {
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<RayCastDirection>();
//         state.RequireForUpdate<LocalTransform>();
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         foreach ((RayCastDirection direction, LocalTransform transform) in SystemAPI
//                      .Query<RayCastDirection, LocalTransform>())
//         {
//             PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
//             RaycastInput input = new RaycastInput()
//             {
//                 Start = new float3(transform.Position.x + 2, transform.Position.y, 0),
//                 End = transform.Position + new float3(5,0,0),
//                 Filter = CollisionFilter.Default
//             };
//
//             Debug.DrawLine(new float3(transform.Position.x + 2, transform.Position.y, 0), transform.Position + new float3(5,0,0));
//             if (physicsWorld.CastRay(input, out RaycastHit hit))
//             {
//                 Debug.Log(hit.Position);
//             }
//             
//            
//         }
//       
//     }
// }
//
// public struct RayCastDirection:IComponentData
// {
//     public float3 direction;
// }
