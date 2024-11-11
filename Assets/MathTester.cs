using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

[BurstCompile]
public class MathTester : MonoBehaviour
{
    public int cycles;

    public bool isStartPlayer;

    public int amountOfplayers;

    public int[] opnieuwGooiMarge;

    public int minResult;



    private void Start()
    {
        random = new Unity.Mathematics.Random((uint)Random.Range(0, 1000001));
    }




    [BurstCompile]
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            decimal wins = 0;
            for (int i = 0; i < cycles; i++)
            {
                wins += RollDie();
            }

            print("Winchance is: " + (wins / 2 / cycles * 100).ToString());
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            decimal wins = 0;
            for (int i = 0; i < cycles; i++)
            {
                int result = RandomRoll();

                if (result >= minResult)
                {
                    wins += 1;
                }
            }

            print("Winchance is: " + (wins / cycles * 100).ToString());
        }
    }


    [BurstCompile]
    private int RollDie()
    {
        int result = RandomRoll();
        int amountOfRolls = 1;

        int[] otherResults = new int[amountOfplayers];

        for (int i = 0; i < 2; i++)
        {
            if (result > opnieuwGooiMarge[i])
            {
                break;
            }

            result = RandomRoll();
            amountOfRolls += 1;
        }

        for (int i = 0; i < amountOfplayers; i++)
        {
            otherResults[i] = RandomRoll();

            for (int i2 = 0; i2 < amountOfRolls - 1; i2++)
            {
                if (otherResults[i] > result)
                {
                    break;
                }

                otherResults[i] = RandomRoll();
            }
        }


        for (int i = 0; i < amountOfplayers; i++)
        {
            if (isStartPlayer)
            {
                if (otherResults[i] > result)
                {
                    return 0;
                }
            }
            else
            {
                if (otherResults[0] > result)
                {
                    return 2;
                }
            }
        }

        for (int i = 0; i < amountOfplayers; i++)
        {
            if (isStartPlayer)
            {
                if (otherResults[i] == result)
                {
                    return 1;
                }
            }
            else
            {
                if (otherResults[0] == result)
                {
                    return 1;
                }
            }
        }

        if (isStartPlayer)
        {
            return 2;
        }
        else
        {
            return 0;
        }
    }




    private Unity.Mathematics.Random random;

    [BurstCompile]
    private int RandomRoll()
    {
        int result = random.NextInt(1, 7);
        int result2 = random.NextInt(1, 7);

        if (result > result2)
        {
            result = result * 10 + result2;
        }
        else
        {
            result = result2 * 10 + result;
        }

        return result;
    }
}
