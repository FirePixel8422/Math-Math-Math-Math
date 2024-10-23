using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public static class TextureCalculator
{
    public static void ScheduleUVGeneration(NativeArray<float2> uvs, NativeArray<byte> cubeFacesActiveState, NativeArray<int> textureIndexs, int atlasSize)
    {
        GenerateBoxMappingUVsJobParallel job = new GenerateBoxMappingUVsJobParallel
        {
            cubeFacesActiveState = cubeFacesActiveState,
            textureIndexs = textureIndexs,
            atlasSize = atlasSize,
            uvs = uvs
        };

        JobHandle handle = job.Schedule(textureIndexs.Length, textureIndexs.Length);
        handle.Complete();
    }


    [BurstCompile]
    public struct GenerateBoxMappingUVsJobParallel : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [NoAlias][ReadOnly] public NativeArray<byte> cubeFacesActiveState;

        [NoAlias][ReadOnly] public NativeArray<int> textureIndexs;

        [NoAlias][ReadOnly] public int atlasSize;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float2> uvs; // Output UVs

        [NoAlias] private int cVertexIndex;




        [BurstCompile]
        public void Execute(int cubeIndex)
        {
            // Calculate the size of each texture in the atlas
            float texelSize = 1.0f / atlasSize; // Assuming a square atlas
            float scaledTexelSize = texelSize / atlasSize;
            float halfScaledTexelSize = texelSize / atlasSize * 0.5f;



            // Calculate the offset in the atlas for the current cube
            int textureIdx = 5;                                                                           // textureIndexs[cubeIndex];

            int row = textureIdx / atlasSize; // Which row in the atlas
            int col = textureIdx % atlasSize; // Which column in the atlas

            // Calculate the base UV offset for the cube
            float uOffset = col * texelSize - halfScaledTexelSize; // U offset
            float vOffset = row * texelSize - halfScaledTexelSize; // V offset

            int addedVertices = 0;

            // Assign UVs for each active face of the cube
            for (int cFaceIndex = 0; cFaceIndex < 6; cFaceIndex++)
            {

                if (cubeFacesActiveState[cubeIndex * 6 + cFaceIndex] == 1) // Only assign UVs if the face is active
                {
                    // Center the UVs and scale down while applying the offset
                    float newUOffset = uOffset + (texelSize * 0.5f) - halfScaledTexelSize;
                    float newVOffset = vOffset + (texelSize * 0.5f) - halfScaledTexelSize;

                    switch (cFaceIndex)
                    {
                        case 0: // back

                            uvs[cVertexIndex + 0] = new float2(newUOffset, newVOffset);
                            uvs[cVertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset);
                            uvs[cVertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                            uvs[cVertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);

                            addedVertices = 4;

                            break;

                        case 1: // front
                            newVOffset += scaledTexelSize * 2; // Move up by 2 tile for front

                            uvs[cVertexIndex + 4] = new float2(newUOffset, newVOffset);
                            uvs[cVertexIndex + 5] = new float2(newUOffset + scaledTexelSize, newVOffset);
                            uvs[cVertexIndex + 6] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                            uvs[cVertexIndex + 7] = new float2(newUOffset, newVOffset + scaledTexelSize);

                            addedVertices = 8;

                            break;

                        case 2: // right
                            newUOffset += scaledTexelSize; // Move right by 1 tile for and
                            newVOffset += scaledTexelSize; // Move up by 1 tile for right

                            uvs[cVertexIndex + 1] = new float2(newUOffset, newVOffset);
                            uvs[cVertexIndex + 6] = new float2(newUOffset + scaledTexelSize, newVOffset);
                            uvs[cVertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                            uvs[cVertexIndex + 5] = new float2(newUOffset, newVOffset + scaledTexelSize);

                            addedVertices = 7;

                            break;

                        case 3: // left
                            newUOffset -= scaledTexelSize; // Move left by 1 tile for and
                            newVOffset += scaledTexelSize; // Move up by 1 tile for right

                            uvs[cVertexIndex + 0] = new float2(newUOffset, newVOffset);
                            uvs[cVertexIndex + 7] = new float2(newUOffset + scaledTexelSize, newVOffset);
                            uvs[cVertexIndex + 4] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                            uvs[cVertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);

                            addedVertices = 8;

                            break;

                        case 4: // top
                            newVOffset += scaledTexelSize; // Move up by 1 tile for top

                            uvs[cVertexIndex + 3] = new float2(newUOffset, newVOffset);
                            uvs[cVertexIndex + 6] = new float2(newUOffset + scaledTexelSize, newVOffset);
                            uvs[cVertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                            uvs[cVertexIndex + 7] = new float2(newUOffset, newVOffset + scaledTexelSize);

                            addedVertices = 8;

                            break;

                        case 5: // bottom
                            newVOffset -= scaledTexelSize; // Move down by 1 tile for bottom

                            uvs[cVertexIndex + 0] = new float2(newUOffset, newVOffset);
                            uvs[cVertexIndex + 5] = new float2(newUOffset + scaledTexelSize, newVOffset);
                            uvs[cVertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                            uvs[cVertexIndex + 4] = new float2(newUOffset, newVOffset + scaledTexelSize);

                            addedVertices = 6;

                            break;
                    }
                }
            }

            Interlocked.Add(ref cVertexIndex, addedVertices);
        }
    }
}