using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class TimerData
{
    public string savedTime;
}

public class TimeManager : MonoBehaviour
{
    //classes
    private GameData gameDataClass;

    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        InvokeRepeating(nameof(CheckConditions), 0f, 60f);
    }

    void SaveCurrentTime()
    {
        gameDataClass.saveData.savedTime = DateTime.Now.ToString("o"); // ISO 8601 format
        gameDataClass.SaveToFile();
    }

    void CheckConditions()
    {
        // Check if conditions are met
        if (gameDataClass.saveData.bonuses[5] == 0 && gameDataClass.saveData.credits < gameDataClass.saveData.bonusesPrice[5])
        {
            CheckElapsedTime();
        }
    }

    void CheckElapsedTime()
    {
        if (!string.IsNullOrEmpty(gameDataClass.saveData.savedTime))
        {
            DateTime savedTime = DateTime.Parse(gameDataClass.saveData.savedTime);
            TimeSpan elapsed = DateTime.Now - savedTime;

            if (elapsed.TotalMinutes >= 30)
            {
                PerformAction();
                SaveCurrentTime(); // Reset the time after the action
            }
            else
            {
                Debug.Log($"Not enough time has passed. Elapsed: {(int)elapsed.TotalMinutes} minutes.");
            }
        }
        else
        {
            SaveCurrentTime(); // Initialize saved time if it's missing
        }
    }

    void PerformAction()
    {
        if(gameDataClass.saveData.bonuses[5] == 0)
            gameDataClass.saveData.bonuses[5] = 1;

        gameDataClass.SaveToFile();
    }
}
