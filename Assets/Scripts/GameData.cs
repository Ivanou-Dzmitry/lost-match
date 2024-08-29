using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


[Serializable]
public class SaveData
{
    public int levelToLoad;
    public bool[] isActive;
    public int[] highScore;
    public int[] stars;
    public int[] bonuses;
    public int credits;
    public bool soundToggle;
    public bool musicToggle;
    public float soundVolume;
    public float musicVolume;
}

public class GameData : MonoBehaviour
{
    public static GameData gameData;
    public SaveData saveData;
    public string fileName = "lm_player_saves.json";


    private void Awake()
    {
        if (gameData == null)
        {
            DontDestroyOnLoad(this.gameObject);
            gameData = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        LoadFromFile();
    }

    public void SaveToFile()
    {
        string savingData = JsonUtility.ToJson(gameData.saveData, true);
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(filePath, savingData);
    }

    public void LoadFromFile()
    {
        //check file
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(filePath))
        {
            string loadedData = File.ReadAllText(filePath);
            saveData = JsonUtility.FromJson<SaveData>(loadedData);
        }
        else
        {
            AddDefaultData();
        }
    }

    public void AddDefaultData()
    {
        saveData = new SaveData();

        //game data
        saveData.levelToLoad = 1;
        saveData.isActive = new bool[11];
        saveData.stars = new int[11];
        saveData.highScore = new int[11];
        saveData.isActive[0] = true; //turn on 1st level
        
        saveData.bonuses = new int[5];

        for (int i = 0; i < saveData.bonuses.Length; i++)
        {
            saveData.bonuses[i] = 0;
        }

        saveData.credits = 0;

        //settings
        saveData.soundToggle = true;
        saveData.musicToggle = true;
        saveData.soundVolume = 1.0f;
        saveData.musicVolume = 0.5f;
    }


    private void OnDisable()
    {
        SaveToFile();
    }

    private void OnApplicationQuit()
    {
        SaveToFile();
    }
}
