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
    public int[] maxBonusCount;
    public int[] bonusesPrice;
    public int credits;
    //public int lives;
    public bool soundToggle;
    public bool musicToggle;
    public float soundVolume;
    public float musicVolume;
    public string savedTime;
}

public class GameData : MonoBehaviour
{
    public static GameData gameData;
    public SaveData saveData;
    public string fileName = "lm_player_saves.json";

    public int bonusCount = 7;


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
        
        saveData.bonuses = new int[bonusCount];

        for (int i = 0; i < saveData.bonuses.Length; i++)
        {
            saveData.bonuses[i] = 0;
        }

        saveData.bonusesPrice = new int[bonusCount];

        for (int i = 0; i < saveData.bonusesPrice.Length; i++)
        {
            saveData.bonusesPrice[i] = 0;
        }

        saveData.maxBonusCount = new int[bonusCount];

        for (int i = 0; i < saveData.maxBonusCount.Length; i++)
        {
            saveData.maxBonusCount[i] = 0;
        }

        Debug.Log(saveData.bonusesPrice.Length);

        //set prices
        saveData.bonusesPrice[0] = 15; //refresh
        saveData.bonusesPrice[1] = 50; //color
        saveData.bonusesPrice[2] = 35; //wrap
        saveData.bonusesPrice[3] = 25; //line
        saveData.bonusesPrice[4] = 25; //line
        saveData.bonusesPrice[5] = 100; //energy battery
        saveData.bonusesPrice[6] = 100; //move

        //set max count
        saveData.maxBonusCount[0] = 3;
        saveData.maxBonusCount[1] = 5;
        saveData.maxBonusCount[2] = 5;
        saveData.maxBonusCount[3] = 10;
        saveData.maxBonusCount[4] = 10;
        saveData.maxBonusCount[5] = 3;
        saveData.maxBonusCount[6] = 20;


        //debug
        saveData.credits = 6000;

        //saveData.lives = 3;
        saveData.bonuses[5] = 3; //set lives bonus #5

        //settings
        saveData.soundToggle = true;
        saveData.musicToggle = true;
        saveData.soundVolume = 1.0f;
        saveData.musicVolume = 0.5f;

        saveData.savedTime = "";
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
