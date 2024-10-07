using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MeshCalculatorJob : MonoBehaviour
{
    public int debugMathComplexity;

    public int jobGroupAmount;
    public int jobTotalAmount;

    public int batchSize;

    public int completions;
    public float elapsed;

    public float timeCheck;

    public bool paused;


    private NativeArray<JobHandle> jobHandles;

    public int testInt;
    public NativeList<NativeList<float3>> vertices;
    public NativeList<NativeList<float3>> triangles;



    [BurstCompile]
    public void CallGenerateMeshJob(NativeList<float3> blockPositions)
    {
        int jobsLeft = jobHandles.Length;
        for (int i = 0; i < jobHandles.Length; i++)
        {
            if (jobHandles[i].IsCompleted)
            {
                jobsLeft -= 1;
            }
        }


        //split load over jobGroupAmount and add 1 for leftover jobs
        int jobsPerGroup = jobTotalAmount / jobGroupAmount;
        int lastJobGroupAmount = jobTotalAmount % jobGroupAmount + jobsPerGroup;


        if (jobsLeft == 0 && (completions > 0 || paused == false))
        {
            if (jobHandles.Length != 0)
            {

                completions += jobTotalAmount;
            }


            vertices = new NativeList<NativeList<float3>>(jobGroupAmount, Allocator.Persistent);
            triangles = new NativeList<NativeList<float3>>(jobGroupAmount, Allocator.Persistent);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new NativeList<float3>((i + 1) == jobGroupAmount ? lastJobGroupAmount : jobsPerGroup, Allocator.Persistent);
                triangles[i] = new NativeList<float3>((i + 1) == jobGroupAmount ? lastJobGroupAmount : jobsPerGroup, Allocator.Persistent);
            }


            jobHandles.Dispose();
            jobHandles = new NativeArray<JobHandle>(jobGroupAmount, Allocator.Persistent);

            for (int i = 0; i < jobGroupAmount; i++)
            {
                GenerateMeshJob burstJob = new GenerateMeshJob
                {
                    vertices = vertices[i],
                };

                jobHandles[i] = burstJob.Schedule((i + 1) == jobGroupAmount ? lastJobGroupAmount : jobsPerGroup, batchSize); // Schedule the job
            }

            JobHandle.CompleteAll(jobHandles);
        }
    }

    private void CallPrint()
    {
        print($"{completions} tasks completed at a batch size of {batchSize}, calling {jobGroupAmount} job groups with {jobTotalAmount} jobs paralel at a time");
    }
}

[BurstCompile]
public struct GenerateMeshJob : IJobParallelFor
{
    public NativeList<float3> vertices;
    public NativeList<float3> triangles;


    public void Execute(int index)
    {
        
    }
}
