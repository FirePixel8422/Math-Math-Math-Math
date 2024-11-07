using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameSaveLoadFunctions : MonoBehaviour
{
    public static GameSaveLoadFunctions Instance;
    private void Awake()
    {
        Instance = this;

        GameSaveData data = SaveAndLoadGame.LoadInfo();
        if (data != null)
        {
            LoadDataFromFile(data);
        }
    }

    public GameSaveData saveData;
    public AudioMixer audioMixer;


    public void LoadDataFromFile(GameSaveData data)
    {
        saveData.mainVolume = data.mainVolume;
        saveData.sfxVolume = data.sfxVolume;
        saveData.musicVolume = data.musicVolume;

        saveData.rWidth = data.rWidth;
        saveData.rHeight = data.rHeight;
        saveData.fullScreen = data.fullScreen;
    }

    public void SaveVolume(float mainVolume, float sfxVolume, float musicVolume)
    {
        saveData.mainVolume = mainVolume;
        saveData.sfxVolume = sfxVolume;
        saveData.musicVolume = musicVolume;
    }

    public void SaveScreenData(int width, int height, bool fullScreen)
    {
        saveData.rWidth = width;
        saveData.rHeight = height;
        saveData.fullScreen = fullScreen;
    }

    private void OnDestroy()
    {
        SaveDataToFile();
    }

    public void SaveDataToFile()
    {
        SaveAndLoadGame.SaveInfo(this);
    }
}
