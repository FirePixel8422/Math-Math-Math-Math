using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public float mainVolume;
    public float sfxVolume;
    public float musicVolume;

    public int rWidth;
    public int rHeight;
    public bool fullScreen;

    public GameSaveData(GameSaveLoadFunctions p)
    {
        mainVolume = p.saveData.mainVolume;
        sfxVolume = p.saveData.sfxVolume;
        musicVolume = p.saveData.musicVolume;

        rWidth = p.saveData.rWidth;
        rHeight = p.saveData.rHeight;
        fullScreen = p.saveData.fullScreen;
    }
}
