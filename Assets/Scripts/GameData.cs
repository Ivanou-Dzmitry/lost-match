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

    [Header("Credits")]
    public int credits;

    [Header("Volume control")]
    public bool soundToggle;
    public bool musicToggle;
    public float soundVolume;
    public float musicVolume;

    [Header("Clip control")]
    public int currentPlayingClipIndex;

    [Header("Timers")]
    public string lifeRecoveryTime;
    public string colorBusterRecoveryTime;
    public string lineBusterRecoveryTime;
}

public class GameData : MonoBehaviour
{
    public static GameData gameData;
    public WorldManager worldManager;
    public SaveData saveData;    

    private int bonusCount = 12;
    private int levelsCount; //important

    //booster prices
    private const int refreshBoosterPrice = 250;
    private const int colorTimeBoosterPrice = 1500;
    private const int lineTimeBoosterPrice = 1300;
    private const int wrapBoosterPrice = 350;
    private const int columnBoosterPrice = 400;
    private const int rowBoosterPrice = 400;

    //energy
    private const int energy01BoosterPrice = 200;
    private const int energy02BoosterPrice = 350;
    private const int energy03BoosterPrice = 550;

    //moves
    private const int moves01BoosterPrice = 200;
    private const int moves02BoosterPrice = 350;
    private const int moves03BoosterPrice = 550;

    public const string PLAYER_SAVES = "lm_player_saves.json";

    private GameLog logClass;
    private const string className = "GameData:";

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

        //check world manager
        if (worldManager == null)
        {
            worldManager = FindObjectOfType<WorldManager>();

            if (worldManager == null)
            {
                logClass.WriteSysLog($"{className}WorldManager not found!");                
                return;
            }
        }

        //get levels count
        levelsCount = worldManager.GetTotalLevelsCount();

        //load data from file
        LoadFromFile();
    }

    private void Start()
    {
        logClass = GameObject.FindWithTag("Log").GetComponent<GameLog>();

        if (logClass == null)
        {
            Debug.LogError($"{className}ERR: logClass null");
        }
    }

    public void SaveToFile()
    {       
        //check game data and save data
        if (gameData == null || gameData.saveData == null)
        {
            if(logClass != null)
                logClass.WriteSysLog($"{className} gameData or gameData.saveData is null. Cannot save to file.");
            return;
        }

        try
        {
            string savingData = JsonUtility.ToJson(gameData.saveData, true);
            string filePath = Path.Combine(Application.persistentDataPath, PLAYER_SAVES);

            File.WriteAllText(filePath, savingData);            
        }
        catch (Exception ex)
        {
            logClass.WriteSysLog($"{className}Error while saving game data: " + ex.Message);
        }
    }

    public void LoadFromFile()
    {
        //check file
        string filePath = Path.Combine(Application.persistentDataPath, PLAYER_SAVES);

        if (File.Exists(filePath))
        {
            string loadedData = File.ReadAllText(filePath);
            saveData = JsonUtility.FromJson<SaveData>(loadedData);

            // Fix any size mismatch
            PatchSavedData();
        }
        else
        {
            //default values
            AddDefaultData();
            logClass.WriteSysLog($"{className}Default data was added");
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
        saveData.bonusesPrice[0] = refreshBoosterPrice; //refresh
        saveData.bonusesPrice[1] = colorTimeBoosterPrice; //color buster TIME
        saveData.bonusesPrice[2] = wrapBoosterPrice; //wrap
        saveData.bonusesPrice[3] = columnBoosterPrice; //line column
        saveData.bonusesPrice[4] = rowBoosterPrice; //line row

        //for game
        saveData.bonusesPrice[5] = energy01BoosterPrice; //energy battery 1
        saveData.bonusesPrice[6] = moves01BoosterPrice; //move 1

        //bundle lives
        saveData.bonusesPrice[7] = energy02BoosterPrice; //energy battery 2
        saveData.bonusesPrice[8] = energy03BoosterPrice; //energy battery 3

        //bundle moves
        saveData.bonusesPrice[9] = moves02BoosterPrice; //move x
        saveData.bonusesPrice[10] = moves03BoosterPrice; //move xx

        saveData.bonusesPrice[11] = lineTimeBoosterPrice; //line buster TIME


        //set MAX COUNT
        saveData.maxBonusCount[0] = 2; //refresh
        saveData.maxBonusCount[1] = 1; //color booster TIME
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

        saveData.maxBonusCount[11] = 1; //line booster TIME


        //start=0
        saveData.credits = 0;

        //saveData.lives = 3;
        saveData.bonuses[5] = 5; //set lives bonus #5

        //sound and music settings
        saveData.soundToggle = true;
        saveData.musicToggle = true;
        saveData.soundVolume = 1.0f;
        saveData.musicVolume = 0.5f;

        saveData.currentPlayingClipIndex = 0;
        
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

    private void PatchSavedData()
    {
        int currentCount = levelsCount;

        // Patch isActive
        if (saveData.isActive.Length < currentCount)
        {
            bool[] newArray = new bool[currentCount];
            saveData.isActive.CopyTo(newArray, 0);
            saveData.isActive = newArray;            
        }

        // Patch stars
        if (saveData.stars.Length < currentCount)
        {
            int[] newArray = new int[currentCount];
            saveData.stars.CopyTo(newArray, 0);
            saveData.stars = newArray;
        }

        // Patch highScore
        if (saveData.highScore.Length < currentCount)
        {
            int[] newArray = new int[currentCount];
            saveData.highScore.CopyTo(newArray, 0);
            saveData.highScore = newArray;
        }

        //booster price values
        var boosterData = new (int price, string desc)[]
        {
            (refreshBoosterPrice, "refresh"),
            (colorTimeBoosterPrice, "color booster TIME"),
            (wrapBoosterPrice, "wrap"),
            (columnBoosterPrice, "line column"),
            (rowBoosterPrice, "line row"),
            (energy01BoosterPrice, "energy battery 1"),
            (moves01BoosterPrice, "move 1"),
            (energy02BoosterPrice, "energy battery 2"),
            (energy03BoosterPrice, "energy battery 3"),
            (moves02BoosterPrice, "move x"),
            (moves03BoosterPrice, "move xx"),
            (lineTimeBoosterPrice, "line booster TIME")
        };

        for (int i = 0; i < boosterData.Length; i++)
        {
            if (saveData.bonusesPrice[i] != boosterData[i].price)
                saveData.bonusesPrice[i] = boosterData[i].price; // boosterData[i].desc
        }

        SaveToFile();
    }

}
