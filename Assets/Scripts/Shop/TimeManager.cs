using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public static TimeManager timeManger;

    //classes
    private GameData gameDataClass;
    private BonusShop bonusShopClass;
    private DateTime recoveryEndTime;

    private float waitingTime = 5.0f; //time for bonus waiting

    public bool addLifeBonus;

    private void Awake()
    {
        if (timeManger == null)
        {
            DontDestroyOnLoad(this.gameObject);
            timeManger = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();

        InvokeRepeating(nameof(CheckFreeLifeConditions), 0f, 1f);
    }

    public string CheckFreeLifeConditions()
    {
        // Check if conditions are met
        if (gameDataClass != null)
        {
            bool fundsForBooster5 = gameDataClass.saveData.credits < gameDataClass.saveData.bonusesPrice[5];
            int buster5Count = gameDataClass.saveData.bonuses[5];

            if (buster5Count == 0 && fundsForBooster5)
            {
                //save time
                if (timeState == TimeState.Idle && gameDataClass.saveData.lifeRecoveryTime == "")
                {
                    gameDataClass.saveData.lifeRecoveryTime = DateTime.Now.ToString();
                }
                                    
                timeState = TimeState.Waiting;                
                return CheckElapsedTime();
            }
        }

        return "0";
    }

    string CheckElapsedTime()
    {
        string time = gameDataClass.saveData.lifeRecoveryTime;
        
        if (!string.IsNullOrEmpty(time))
        {            
            DateTime savedTime = DateTime.Parse(gameDataClass.saveData.lifeRecoveryTime);
            TimeSpan elapsed = DateTime.Now - savedTime;

            TimeSpan totalWaitDuration = TimeSpan.FromMinutes(waitingTime);
            TimeSpan remainingTime = totalWaitDuration - elapsed;

            string formattedTime = $"{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}";            

            if (elapsed.TotalMinutes >= waitingTime)
            {
                PerformAction();
            }
            else
            {
                Debug.Log($"Time LIFE left: {formattedTime}");
            }

            return formattedTime; // Return elapsed minutes as an integer
        }
        else
        {
            gameDataClass.saveData.lifeRecoveryTime = "";
            return "0";
        }        
    }

    void PerformAction()
    {
        //add life
        if(gameDataClass.saveData.bonuses[5] == 0)
            gameDataClass.saveData.bonuses[5] = 3;

        //set time state
        timeState = TimeState.Idle;
        
        //zero time
        gameDataClass.saveData.lifeRecoveryTime = "";
        gameDataClass.SaveToFile();
    }


}
