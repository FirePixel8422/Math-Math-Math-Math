using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class TextureCalculator
{
    public async static Task GenerateBoxMappingUVs(Mesh mesh, CubeFace[] activeFacesPerCube, int[] textureIndex, int atlasSize)
    {
        await Task.Delay(0);

        // Get the vertices of the mesh
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];

        // Calculate the size of each texture in the atlas
        float texelSize = 1.0f / atlasSize; // Assuming a square atlas

        int vertexIndex = 0; // Keep track of the vertex index for UV assignment
        for (int cubeIndex = 0; cubeIndex < activeFacesPerCube.Length; cubeIndex++)
        {
            bool[] activeFaces = activeFacesPerCube[cubeIndex].activeFaces;

            // Calculate the offset in the atlas for the current cube
            int textureIdx = textureIndex[cubeIndex]; // Get the texture index for the current cube
            int row = textureIdx / atlasSize; // Which row in the atlas
            int col = textureIdx % atlasSize; // Which column in the atlas


            float uOffset = col * texelSize; // U offset
            float vOffset = row * texelSize; // V offset

            // Assign UVs for each active face of the cube
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                if (activeFaces[faceIndex]) // Only assign UVs if the face is active
                {
                    switch (faceIndex)
                    {
                        case 0: // back
                            uvs[vertexIndex + 0] = new Vector2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new Vector2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new Vector2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new Vector2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4; // Move to the next face's vertices
                            break;

                        case 1: // front
                            uvs[vertexIndex + 0] = new Vector2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new Vector2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new Vector2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new Vector2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 2: // right
                            uvs[vertexIndex + 0] = new Vector2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new Vector2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new Vector2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new Vector2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 3: // left
                            uvs[vertexIndex + 0] = new Vector2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new Vector2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new Vector2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new Vector2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 4: // top
                            uvs[vertexIndex + 0] = new Vector2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new Vector2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new Vector2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new Vector2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;

                        case 5: // bottom
                            uvs[vertexIndex + 0] = new Vector2(uOffset, vOffset); // Bottom left
                            uvs[vertexIndex + 1] = new Vector2(uOffset + texelSize, vOffset); // Bottom right
                            uvs[vertexIndex + 2] = new Vector2(uOffset + texelSize, vOffset + texelSize); // Top right
                            uvs[vertexIndex + 3] = new Vector2(uOffset, vOffset + texelSize); // Top left
                            vertexIndex += 4;
                            break;
                    }
                }
            }
        }

        // Assign UVs to the mesh
        mesh.uv = uvs;
    }

}
