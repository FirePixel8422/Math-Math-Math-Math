using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public struct TextureCalculator
{
    public static void ScheduleUVGeneration(NativeArray<float4> uvs, NativeArray<float2> textureData, NativeArray<byte> cubeFacesActiveState, NativeArray<int> textureIndexs, int atlasSize)
    {
        CalculateUvsJobParallel job = new CalculateUvsJobParallel
        {
            cubeFacesActiveState = cubeFacesActiveState,
            textureIndexs = textureIndexs,

            uvs = uvs,
            textureData = textureData,
        };

        JobHandle handle = job.Schedule(textureIndexs.Length, textureIndexs.Length);
        handle.Complete();
    }


    [BurstCompile]
    public struct CalculateUvsJobParallel : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [NoAlias][ReadOnly] public NativeArray<byte> cubeFacesActiveState;

        [NoAlias]/*[ReadOnly]*/ public NativeArray<int> textureIndexs;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float4> uvs; // Output UVs

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float2> textureData; // Output UVs

        [NoAlias] private int cVertexIndex;




        [BurstCompile]
        public void Execute(int cubeIndex)
        {
            int addedVertices = 0;

            // Assign UVs for each active face of the cube
            for (int cFaceIndex = 0; cFaceIndex < 6; cFaceIndex++)
            {

                if (cubeFacesActiveState[cubeIndex * 6 + cFaceIndex] == 1) // Only assign UVs if the face is active
                {
                    textureIndexs[cubeIndex] = 1;

                    //float4[] targetUvs = new float4[8];
                    //targetUvs[0] = new float4(0, 0, 0, 0);
                    //targetUvs[1] = new float4(1, 0, 1, 0);
                    //targetUvs[2] = new float4(1, 1, 1, 0);
                    //targetUvs[3] = new float4(0, 1, 0, 0);
                    //targetUvs[4] = new float4(1, 0, 0, 1);
                    //targetUvs[5] = new float4(0, 0, 1, 1);
                    //targetUvs[6] = new float4(0, 1, 1, 1);
                    //targetUvs[7] = new float4(1, 1, 0, 1);


                    switch (cFaceIndex)
                    {
                        case 0: // back

                            uvs[cVertexIndex + 0] = new float4(0, 0, 0, 0);
                            uvs[cVertexIndex + 1] = new float4(1, 0, 1, 0);
                            uvs[cVertexIndex + 2] = new float4(1, 1, 1, 0);
                            uvs[cVertexIndex + 3] = new float4(0, 1, 0, 0);
                            
                            addedVertices = 4;
                            
                            break;

                        case 1: // front

                            uvs[cVertexIndex + 4] = new float4(1, 0, 1, 0);
                            uvs[cVertexIndex + 5] = new float4(0, 0, 1, 1);
                            uvs[cVertexIndex + 6] = new float4(0, 1, 1, 1);
                            uvs[cVertexIndex + 7] = new float4(1, 1, 0, 1);

                            addedVertices = 8;

                            break;

                        case 2: // right

                            uvs[cVertexIndex + 1] = new float4(1, 0, 1, 0);
                            uvs[cVertexIndex + 2] = new float4(1, 1, 1, 0);
                            uvs[cVertexIndex + 5] = new float4(0, 0, 1, 1);
                            uvs[cVertexIndex + 6] = new float4(0, 1, 1, 1);

                            addedVertices = 7;

                            break;

                        case 3: // left

                            uvs[cVertexIndex + 0] = new float4(0, 0, 0, 0);
                            uvs[cVertexIndex + 3] = new float4(0, 1, 0, 0);
                            uvs[cVertexIndex + 4] = new float4(1, 0, 0, 1);
                            uvs[cVertexIndex + 7] = new float4(1, 1, 0, 1);

                            addedVertices = 8;

                            break;

                        case 4: // top

                            uvs[cVertexIndex + 2] = new float4(1, 1, 1, 0);
                            uvs[cVertexIndex + 3] = new float4(0, 1, 0, 0);
                            uvs[cVertexIndex + 6] = new float4(0, 1, 1, 1);
                            uvs[cVertexIndex + 7] = new float4(1, 1, 0, 1);

                            addedVertices = 8;

                            break;


                        case 5: // bottom

                            uvs[cVertexIndex + 0] = new float4(0, 0, 0, 0);
                            uvs[cVertexIndex + 1] = new float4(1, 0, 1, 0);
                            uvs[cVertexIndex + 4] = new float4(1, 0, 0, 1);
                            uvs[cVertexIndex + 5] = new float4(0, 0, 1, 1);

                            addedVertices = 6;

                            break;
                    }
                }
            }

            for (int i = 0; i < addedVertices; i++)
            {
                textureData[cVertexIndex + i] = new float2(textureIndexs[cubeIndex], 0);
            }

            Interlocked.Add(ref cVertexIndex, addedVertices);
        }
    }
}