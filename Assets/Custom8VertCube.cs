using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;



[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Custom8VertCube : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(DelayedStart), .5f);  
    }

    private void DelayedStart()
    {
        NativeArray<float3> vertices = new NativeArray<float3>(8, Allocator.Temp)
        {
            [0] = new float3(-0.5f, -0.5f, -0.5f),
            [1] = new float3(0.5f, -0.5f, -0.5f),
            [2] = new float3(0.5f, 0.5f, -0.5f), 
            [3] = new float3(-0.5f, 0.5f, -0.5f), 
            [4] = new float3(-0.5f, -0.5f, 0.5f), 
            [5] = new float3(0.5f, -0.5f, 0.5f),  
            [6] = new float3(0.5f, 0.5f, 0.5f),  
            [7] = new float3(-0.5f, 0.5f, 0.5f),  
        };

        // Define triangles (indices into the vertices array)
        NativeArray<int> triangles = new NativeArray<int>(36, Allocator.Temp)
        {
            // Front face
            [0] = 0,
            [1] = 2,
            [2] = 1,
            [3] = 0,
            [4] = 3,
            [5] = 2,

            // Back face
            [6] = 5,
            [7] = 6,
            [8] = 4,
            [9] = 4,
            [10] = 6,
            [11] = 7,

            // Left face
            [12] = 4,
            [13] = 7,
            [14] = 0,
            [15] = 0,
            [16] = 7,
            [17] = 3,

            // Right face
            [18] = 1,
            [19] = 2,
            [20] = 5,
            [21] = 5,
            [22] = 2,
            [23] = 6,

            // Top face
            [24] = 3,
            [25] = 7,
            [26] = 2,
            [27] = 2,
            [28] = 7,
            [29] = 6,

            // Bottom face
            [30] = 4,
            [31] = 0,
            [32] = 1,
            [33] = 4,
            [34] = 1,
            [35] = 5
        };

        // Define UVs for each vertex (mapping to the texture)
        NativeArray<float4> uvs = new NativeArray<float4>(8, Allocator.Temp)
        {
            [0] = new float4(0, 0, 0, 0),
            [1] = new float4(1, 0, 1, 0),
            [2] = new float4(1, 1, 1, 0),
            [3] = new float4(0, 1, 0, 0),
            [4] = new float4(1, 0, 0, 1),
            [5] = new float4(0, 0, 1, 1),
            [6] = new float4(0, 1, 1, 1),
            [7] = new float4(1, 1, 0, 1),
        };

        // Create the mesh
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        // Assign data to the mesh
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetUVs(0, uvs);

        // Assign mesh to the MeshFilter
        GetComponent<MeshFilter>().mesh = mesh;

        // Dispose of NativeArrays
        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
    }
}
