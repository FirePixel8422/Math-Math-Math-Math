//using System;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;

//[BurstCompile]
//partial class CubeMoverSystem : SystemBase
//{
//    protected override void OnCreate()
//    {
//        RequireForUpdate<MoveSpeedComponent>();
//    }

//    [BurstCompile]
//    protected override void OnUpdate()
//    {
//        // Step 1: Gather the data into NativeArrays
//        var query = SystemAPI.QueryBuilder()
//            .WithAll<LocalTransform, MoveSpeedComponent>()
//            .Build();

//        // Get the count of entities that match the query
//        int entityCount = query.CalculateEntityCount();

//        // Create NativeArrays to hold the data for the job
//        NativeArray<LocalTransform> transforms = new NativeArray<LocalTransform>(entityCount, Allocator.TempJob);
//        NativeArray<MoveSpeedComponent> moveSpeeds = new NativeArray<MoveSpeedComponent>(entityCount, Allocator.TempJob);

//        // Fill the NativeArrays with component data
//        int index = 0;
//        foreach (var (transform, moveSpeedComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeedComponent>>())
//        {
//            transforms[index] = transform.ValueRO;
//            moveSpeeds[index] = moveSpeedComponent.ValueRO;
//            index++;
//        }

//        // Step 2: Schedule the job
//        UPDATEPOS job = new UPDATEPOS
//        {
//            Transforms = transforms,
//            MoveSpeeds = moveSpeeds,
//            time = (float)SystemAPI.Time.ElapsedTime,
//        };

//        JobHandle handle = job.Schedule(entityCount, 64); // Schedule job with a batch size of 64
//        handle.Complete(); // Complete the job

//        // Step 3: Write the results back to the entities
//        index = 0;
//        foreach (var (transform, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeedComponent>>())
//        {
//            transform.ValueRW = job.Transforms[index];
//            index++;
//        }

//        // Dispose the NativeArrays
//        transforms.Dispose();
//        moveSpeeds.Dispose();
//    }

//    [BurstCompile]
//    public struct UPDATEPOS : IJobParallelFor
//    {
//        [NativeDisableParallelForRestriction]
//        public NativeArray<LocalTransform> Transforms;

//        [NativeDisableParallelForRestriction]
//        public NativeArray<MoveSpeedComponent> MoveSpeeds;

//        public float time;

//        public void Execute(int index)
//        {
//            // Update the position based on time and move speed
//            LocalTransform transform = Transforms[index];

//            transform.Position.y = math.sin(time + MoveSpeeds[index].value);

//            // Store the result back into the NativeArray
//            Transforms[index] = transform;
//        }
//    }
//}
