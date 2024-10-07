using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;


[BurstCompile]
public class NativesTesting : MonoBehaviour
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
    public NativeArray<NativeArray<int>> testReturnInt;



    [BurstCompile]
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
                JobHandle.CompleteAll(jobHandles);

                completions += jobTotalAmount;
            }

            if (testReturnInt.IsCreated)
            {
                for (int i = 0; i < testReturnInt.Length; i++)
                {
                    for (int i2 = 0; i2 < testReturnInt[i].Length; i2++)
                    {
                        testInt += testReturnInt[i][i2];
                    }

                    testReturnInt[i].Dispose();
                }

                testReturnInt.Dispose();
            }


            testReturnInt = new NativeArray<NativeArray<int>>(jobGroupAmount, Allocator.Persistent);
            for (int i = 0; i < testReturnInt.Length; i++)
            {
                testReturnInt[i] = new NativeArray<int>((i + 1) == jobGroupAmount ? lastJobGroupAmount : jobsPerGroup, Allocator.Persistent);
            }


            jobHandles.Dispose();
            jobHandles = new NativeArray<JobHandle>(jobGroupAmount, Allocator.Persistent);

            for (int i = 0; i < jobGroupAmount; i++)
            {
                BurstTestJob burstJob = new BurstTestJob
                {
                    testReturnInt = testReturnInt[i],
                    debugMathComplexity = debugMathComplexity,
                };

                jobHandles[i] = burstJob.Schedule((i + 1) == jobGroupAmount ? lastJobGroupAmount : jobsPerGroup, batchSize); // Schedule the job
            }
        }


        elapsed += Time.deltaTime;

        if (elapsed >= timeCheck && completions > 0)
        {
            paused = true;

            elapsed = 0;

            CallPrint();

            completions = 0;
        }
    }

    private void CallPrint()
    {
        print($"{completions} tasks completed at a batch size of {batchSize}, calling {jobGroupAmount} job groups with {jobTotalAmount} jobs paralel at a time");
    }



    private void OnDestroy()
    {
        if (jobHandles.IsCreated)
        {
            JobHandle.CompleteAll(jobHandles);
            jobHandles.Dispose();
        }

        if (testReturnInt.IsCreated)
        {
            for (int i = 0; i < testReturnInt.Length; i++)
            {
                if (testReturnInt[i].IsCreated)
                {
                    testReturnInt[i].Dispose();
                }
            }

            testReturnInt.Dispose();
        }
    }
}

[BurstCompile]
public struct BurstTestJob : IJobParallelFor
{
    public NativeArray<int> testReturnInt;

    public int debugMathComplexity;


    public void Execute(int index)
    {
        int value1 = 0;

        // First heavy calculation
        for (int i = 0; i < debugMathComplexity; i++)
        {
            value1 += Mathf.RoundToInt(Mathf.Pow(Mathf.Sin(i * 0.1f), 2) * Mathf.Sqrt(i));
        }

        int value2 = 0;

        // Second heavy calculation that cancels out the first
        for (int i = 0; i < debugMathComplexity; i++)
        {
            value2 += Mathf.RoundToInt(Mathf.Pow(-Mathf.Sin(i * 0.1f), 2) * Mathf.Sqrt(i));
        }

        testReturnInt[index] += Mathf.Clamp(1 + value1 + value2, 0, 1);
    }
}
