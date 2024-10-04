using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class NativesTesting : MonoBehaviour
{
    public int strength;
    public bool useBurst;

    private void Update()
    {
        Stopwatch sw = Stopwatch.StartNew();

        if (useBurst)
        {
            BurstTestJob burstJob = new BurstTestJob
            {
                strength = strength
            };

            // Schedule the job in parallel over the number of iterations
            JobHandle handle = burstJob.Schedule(strength, 12); // 64 is the batch size for splitting the job across threads
            handle.Complete();  // Wait for job completion

            print(sw.ElapsedMilliseconds + "ms");
        }
        else
        {
            BurstLessNativesTesting();

            print(sw.ElapsedMilliseconds + "ms");
        }
    }

    public void BurstLessNativesTesting()
    {
        float power = 1;
        for (int i = 0; i < strength; i++)
        {
            power *= Mathf.Sqrt(power) + Mathf.Sin(i);
            power /= Mathf.Sqrt(power) + Mathf.Cos(i);
        }
    }

    [BurstCompile]
    public struct BurstTestJob : IJobParallelFor
    {
        public int strength;

        public void Execute(int index)
        {
            float power = 1;
            for (int i = 0; i < strength; i++)
            {
                //power *= Mathf.Sqrt(power) + Mathf.Sin(i);
                power /= Mathf.Sqrt(power) + Mathf.Cos(i);
                power = 2 * power;
            }
        }
    }
}
