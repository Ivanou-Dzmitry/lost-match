using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static BonusShop;

[Serializable]
public class TimerData
{
    public string savedTime;
}

public class TimeManager : MonoBehaviour
{
    public enum TimeState
    {
        Idle,
        Waiting
    }

    public TimeState timeState;


    //classes
    private GameData gameDataClass;

    private int waitingTime = 5; //time for bonus waiting

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

    public int CheckConditions()
    {
        // Check if conditions are met
        if (gameDataClass!= null && gameDataClass.saveData.bonuses[5] == 0 && gameDataClass.saveData.credits < gameDataClass.saveData.bonusesPrice[5])
        {
            timeState = TimeState.Waiting;
            return CheckElapsedTime();
        }
        
        return 0;
    }

    int CheckElapsedTime()
    {

        if (!string.IsNullOrEmpty(gameDataClass.saveData.savedTime))
        {
            DateTime savedTime = DateTime.Parse(gameDataClass.saveData.savedTime);
            TimeSpan elapsed = DateTime.Now - savedTime;

            int elapsedMinutes = (int)elapsed.TotalMinutes;
            int timeLeft = Math.Max(waitingTime - elapsedMinutes, 0);


            if (elapsed.TotalMinutes >= waitingTime)
            {
                PerformAction();
                SaveCurrentTime(); // Reset the time after the action
            }
            else
            {
                Debug.Log($"Not enough time has passed. Elapsed: {(int)elapsed.TotalMinutes} minutes.");                
            }

            return timeLeft; // Return elapsed minutes as an integer
        }
        else
        {
            SaveCurrentTime(); // Initialize saved time if it's missing            
            return 0;
        }        
    }

    void PerformAction()
    {
        //add life
        if(gameDataClass.saveData.bonuses[5] == 0)
            gameDataClass.saveData.bonuses[5] = 3;

        timeState = TimeState.Idle;        
    }
}
