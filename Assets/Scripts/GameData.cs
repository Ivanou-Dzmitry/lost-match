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
    public string lifeRecoveryTime;
    public string colorBusterRecoveryTime;
    public string lineBusterRecoveryTime;
}

public class GameData : MonoBehaviour
{
    public static GameData gameData;
    public SaveData saveData;
    public string fileName = "lm_player_saves.json";

    private int bonusCount = 12;
    private int levelsCount = 21;


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

        //game data levels stuff
        saveData.levelToLoad = 1;
        saveData.isActive = new bool[levelsCount];
        saveData.stars = new int[levelsCount];
        saveData.highScore = new int[levelsCount];

        //turn on 1st level
        saveData.isActive[0] = true; 
        
        //buster type
        saveData.bonuses = new int[bonusCount];

        for (int i = 0; i < saveData.bonuses.Length; i++)
        {
            saveData.bonuses[i] = 0;
        }

        //buster price
        saveData.bonusesPrice = new int[bonusCount];

        for (int i = 0; i < saveData.bonusesPrice.Length; i++)
        {
            saveData.bonusesPrice[i] = 0;
        }

        //maximus busters count
        saveData.maxBonusCount = new int[bonusCount];

        for (int i = 0; i < saveData.maxBonusCount.Length; i++)
        {
            saveData.maxBonusCount[i] = 0;
        }

        
        //set prices
        saveData.bonusesPrice[0] = 150; //refresh
        saveData.bonusesPrice[1] = 1500; //color buster
        saveData.bonusesPrice[2] = 350; //wrap
        saveData.bonusesPrice[3] = 250; //line
        saveData.bonusesPrice[4] = 250; //line

        //for game
        saveData.bonusesPrice[5] = 100; //energy battery 1
        saveData.bonusesPrice[6] = 160; //move 1

        //bundle lives
        saveData.bonusesPrice[7] = 150; //energy battery 2
        saveData.bonusesPrice[8] = 250; //energy battery 3

        //bundle moves
        saveData.bonusesPrice[9] = 270; //move x
        saveData.bonusesPrice[10] = 430; //move xx

        saveData.bonusesPrice[11] = 1300; //line buster


        //set MAX COUNT
        saveData.maxBonusCount[0] = 2; //refresh
        saveData.maxBonusCount[1] = 1; //color
        saveData.maxBonusCount[2] = 3; //wrap
        saveData.maxBonusCount[3] = 3; //line
        saveData.maxBonusCount[4] = 3; //line

        saveData.maxBonusCount[5] = 5; //energy battery 1
        saveData.maxBonusCount[6] = 20; //move 1

        //bundle lives
        saveData.maxBonusCount[7] = 2; //energy battery 1
        saveData.maxBonusCount[8] = 1; //energy battery 1

        //bundle moves
        saveData.maxBonusCount[9] = 1; //move x
        saveData.maxBonusCount[10] = 1; //move xx

        saveData.maxBonusCount[11] = 1; //line buster


        //start=0
        saveData.credits = 10000;

        //saveData.lives = 3;
        saveData.bonuses[5] = 5; //set lives bonus #5

        //sound and music settings
        saveData.soundToggle = true;
        saveData.musicToggle = true;
        saveData.soundVolume = 1.0f;
        saveData.musicVolume = 0.5f;
        
        //time for battery
        saveData.lifeRecoveryTime = "";
        saveData.colorBusterRecoveryTime = "";
        saveData.lineBusterRecoveryTime = "";
    }

    private void OnDisable()
    {
        SaveToFile();
    }

    private void OnApplicationQuit()
    {
        SaveToFile();
    }

    public void DebugAddCredits()
    {
        saveData.credits = 10000;
        SaveToFile();
    }

    public void DebugZeroCredit()
    {
        saveData.credits = 13;
        SaveToFile();
    }

    public void DebugZeroLife()
    {
        saveData.bonuses[5] = 1; //set lives bonus 0
        SaveToFile();
    }


    public void DebugOpenLevels()
    {
        for(int i = 0; i< saveData.isActive.Length; i++)
        {
            saveData.isActive[i] = true;
        }
        SaveToFile();
    }

}
