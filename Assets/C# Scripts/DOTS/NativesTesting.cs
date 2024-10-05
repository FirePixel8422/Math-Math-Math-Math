using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.Jobs;

public class NativesTesting : MonoBehaviour
{
    public int strength;

    public int jobsPerFrame = 30;
    private int debugPrevJobsPerFrame;
    public int batchSize;

    public int completions;
    public float elapsed;

    public float timeCheck;

    public bool paused;


    private NativeArray<JobHandle> jobHandles;


    private void OnValidate()
    {
        if (jobsPerFrame != debugPrevJobsPerFrame)
        {
            debugPrevJobsPerFrame = jobsPerFrame;
        }
    }


    private void Start()
    {
        debugPrevJobsPerFrame = jobsPerFrame;
    }

    private void Update()
    {
        if (paused)
        {
            return;
        }

        int jobsLeft = jobHandles.Length;
        for (int i = 0; i < jobHandles.Length; i++)
        {
            if (jobHandles[i].IsCompleted)
            {
                completions += 1;
                jobsLeft -= 1;
            }
        }

        if (jobsLeft == 0 && (completions > 0 || paused == false))
        {
            if (jobHandles.Length != 0)
            {
                JobHandle.CompleteAll(jobHandles);
            }

            jobHandles.Dispose();

            jobHandles = new NativeArray<JobHandle>(jobsPerFrame, Allocator.Persistent);


            for (int i = 0; i < jobsPerFrame; i++)
            {
                var burstJob = new BurstTestJob { strength = strength };
                jobHandles[i] = burstJob.Schedule(strength, batchSize); // Schedule the job
            }
        }

        
        elapsed += Time.deltaTime;

        if (elapsed >= timeCheck)
        {
            paused = true;

            elapsed -= timeCheck;

            print($"{completions} tasks completed at a batch size of {batchSize}, with {jobsPerFrame} jobs calls at a time");

            completions = 0;
        }
    }


    [BurstCompile]
    public struct BurstTestJob : IJobParallelFor
    {
        public int strength;

        public void Execute(int index)
        {
            Int32 value1 = 0;

            // First heavy calculation
            for (int i = 0; i < strength; i++)
            {
                value1 += Mathf.RoundToInt(Mathf.Pow(Mathf.Sin(i * 0.1f), 2) * Mathf.Sqrt(i));
            }

            Int32 value2 = 0;

            // Second heavy calculation that cancels out the first
            for (int i = 0; i < strength; i++)
            {
                value2 += Mathf.RoundToInt(Mathf.Pow(Mathf.Sin(i * 0.1f + Mathf.PI), 2) * Mathf.Sqrt(i));
            }
        }
    }
}
