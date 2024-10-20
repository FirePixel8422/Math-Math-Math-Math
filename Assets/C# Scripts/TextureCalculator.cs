using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public static class TextureCalculator
{
    public static void ScheduleUVGeneration(NativeArray<float2> uvs, int verticeCount, NativeArray<byte> cubeFacesActiveState, NativeArray<int> textureIndexs, int atlasSize)
    {
        GenerateBoxMappingUVsJob job = new GenerateBoxMappingUVsJob
        {
            verticeCount = verticeCount,
            cubeFacesActiveState = cubeFacesActiveState,
            textureIndexs = textureIndexs,
            atlasSize = atlasSize,
            uvs = uvs
        };

        JobHandle handle = job.Schedule();
        handle.Complete();
    }


    [BurstCompile]
    public struct GenerateBoxMappingUVsJob : IJob
    {
        [NoAlias][ReadOnly] public int verticeCount;

        [NativeDisableParallelForRestriction]
        [NoAlias][ReadOnly] public NativeArray<byte> cubeFacesActiveState;

        [NoAlias][ReadOnly] public NativeArray<int> textureIndexs;

        [NoAlias][ReadOnly] public int atlasSize;

        [NativeDisableParallelForRestriction]
        [NoAlias][WriteOnly] public NativeArray<float2> uvs; // Output UVs




        [BurstCompile]
        public void Execute()
        {
            // Calculate the size of each texture in the atlas
            float texelSize = 1.0f / atlasSize; // Assuming a square atlas
            float scaledTexelSize = texelSize / atlasSize;
            float halfScaledTexelSize = texelSize / atlasSize * 0.5f; 

            int vertexIndex = 0; // Keep track of the vertex index for UV assignment

            for (int cubeIndex = 0; cubeIndex < textureIndexs.Length; cubeIndex++)
            {
                // Calculate the offset in the atlas for the current cube
                int textureIdx = 2;// textureIndexs[cubeIndex];

                int row = textureIdx / atlasSize; // Which row in the atlas
                int col = textureIdx % atlasSize; // Which column in the atlas

                // Calculate the base UV offset for the cube
                float uOffset = col * texelSize - halfScaledTexelSize; // U offset
                float vOffset = row * texelSize - halfScaledTexelSize; // V offset

                // Assign UVs for each active face of the cube
                for (int faceIndex = 0; faceIndex < 6; faceIndex++)
                {
                    if (cubeFacesActiveState[cubeIndex * 6 + faceIndex] == 1) // Only assign UVs if the face is active
                    {
                        // Center the UVs and scale down while applying the offset
                        float newUOffset = uOffset + (texelSize / 2) - halfScaledTexelSize;
                        float newVOffset = vOffset + (texelSize / 2) - halfScaledTexelSize;

                        switch (faceIndex)
                        {
                            case 0: // back

                                uvs[vertexIndex + 0] = new float2(newUOffset, newVOffset);
                                uvs[vertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset);
                                uvs[vertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                                uvs[vertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);
                                break;

                            case 1: // front
                                newVOffset += scaledTexelSize * 2; // Move up by 2 tile for front

                                uvs[vertexIndex + 0] = new float2(newUOffset, newVOffset);
                                uvs[vertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset);
                                uvs[vertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                                uvs[vertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);

                                break;

                            case 2: // right
                                newUOffset += scaledTexelSize; // Move right by 1 tile for and
                                newVOffset += scaledTexelSize; // Move up by 1 tile for right

                                uvs[vertexIndex + 0] = new float2(newUOffset, newVOffset);
                                uvs[vertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset);
                                uvs[vertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                                uvs[vertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);
                                break;

                            case 3: // left
                                newUOffset -= scaledTexelSize; // Move left by 1 tile for and
                                newVOffset += scaledTexelSize; // Move up by 1 tile for right

                                uvs[vertexIndex + 0] = new float2(newUOffset, newVOffset);
                                uvs[vertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset);
                                uvs[vertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                                uvs[vertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);
                                break;

                            case 4: // top
                                newVOffset += scaledTexelSize; // Move up by 1 tile for top

                                uvs[vertexIndex + 0] = new float2(newUOffset, newVOffset);
                                uvs[vertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset);
                                uvs[vertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                                uvs[vertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);
                                break;

                            case 5: // bottom
                                newVOffset -= scaledTexelSize; // Move down by 1 tile for bottom

                                uvs[vertexIndex + 0] = new float2(newUOffset, newVOffset);
                                uvs[vertexIndex + 1] = new float2(newUOffset + scaledTexelSize, newVOffset);
                                uvs[vertexIndex + 2] = new float2(newUOffset + scaledTexelSize, newVOffset + scaledTexelSize);
                                uvs[vertexIndex + 3] = new float2(newUOffset, newVOffset + scaledTexelSize);
                                break;
                        }

                        vertexIndex += 4;
                    }
                }
            }
        }

    }

    [BurstCompile]
    public struct GenerateBoxMappingUVsJobParallel : IJobParallelFor
    {
        [ReadOnly] public int verticeCount;

        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<byte> cubeFacesActiveState;

        [ReadOnly] public NativeArray<int> textureIndexs;

        [ReadOnly] public int atlasSize;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float2> uvs; // Output UVs




        [BurstCompile]
        public void Execute(int cubeIndex)
        {
            // Calculate the size of each texture in the atlas
            float texelSize = 1.0f / atlasSize; // Assuming a square atlas

            int vertexIndex = cubeIndex * 24; // Keep track of the vertex index for UV assignment


            // Calculate the offset in the atlas for the current cube
            int textureIdx = textureIndexs[cubeIndex]; // Get the texture index for the current cube
            int row = textureIdx / atlasSize; // Which row in the atlas
            int col = textureIdx % atlasSize; // Which column in the atlas


            float uOffset = col * texelSize; // U offset
            float vOffset = row * texelSize; // V offset

            // Assign UVs for each active face of the cube
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                if (cubeFacesActiveState[cubeIndex * 6 + faceIndex] == 1) // Only assign UVs if the face is active
                {
                    switch (faceIndex)
                    {
                        case 0: // back
                            uvs[vertexIndex + 0] = new float2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new float2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new float2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new float2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4; // Move to the next face's vertices
                            break;

                        case 1: // front
                            uvs[vertexIndex + 0] = new float2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new float2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new float2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new float2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 2: // right
                            uvs[vertexIndex + 0] = new float2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new float2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new float2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new float2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 3: // left
                            uvs[vertexIndex + 0] = new float2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new float2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new float2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new float2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 4: // top
                            uvs[vertexIndex + 0] = new float2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new float2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new float2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new float2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 5: // bottom
                            uvs[vertexIndex + 0] = new float2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new float2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new float2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new float2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;
                    }
                }
            }
        }
    }


}