using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MoveControllerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        //the ForEach Loop/Function is NOT on the main thread, Time.deltaTime has to be accesed from the main thread.
        float deltaTime = UnityEngine.Time.deltaTime;

        Entities.ForEach((ref LocalTransform transform, ref MoveSpeedComponent moveSpeedComponent) =>
        {
            transform.Position += new float3(1 * moveSpeedComponent.moveSpeed.x, 0, 1 * moveSpeedComponent.moveSpeed.y) * deltaTime;
        }).Schedule();
    }
}