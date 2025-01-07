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
    public class Timer
    {
        public DateTime StartTime { get; set; }
        public int DurationInSeconds { get; set; }
        public Action OnStart { get; set; }
        public Action OnComplete { get; set; }
    }

    private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();

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

    //private float waitingTime = 5.0f; //time for bonus waiting

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

        InvokeRepeating(nameof(CheckTimers), 0f, 1f);
    }

    public void CreateTimer(string timerName, int durationInSeconds, Action onStart, Action onComplete)
    {
        // Check if a timer with this name already exists
        if (timers.ContainsKey(timerName))
        {
            Debug.LogWarning($"Timer {timerName} already exists!");
            return;
        }

        DateTime startTime;

        // Check saved time in gameDataClass.saveData
        string savedTime = GetSavedTimerTime(timerName);
        if (!string.IsNullOrEmpty(savedTime) && DateTime.TryParse(savedTime, out startTime))
        {
            Debug.Log($"Restoring timer {timerName} with saved start time: {startTime}");
        }
        else
        {
            // Start a new timer if no valid saved time
            startTime = DateTime.Now;
            if(gameDataClass != null && timerName != null)
                SaveTimerStartTime(timerName, startTime);
            Debug.Log($"Creating new timer {timerName} starting at: {startTime}");
        }

        // Create and store the timer
        Timer timer = new Timer
        {
            StartTime = startTime,
            DurationInSeconds = durationInSeconds,
            OnStart = onStart,
            OnComplete = onComplete
        };

        timers[timerName] = timer;

        // Execute OnStart action if it's a new timer
        if (savedTime == "")
        {
            timer.OnStart?.Invoke();
        }
    }

    private void CheckTimers()
    {
        List<string> completedTimers = new List<string>();

        foreach (var timerEntry in timers)
        {
            string timerName = timerEntry.Key;
            Timer timer = timerEntry.Value;

            TimeSpan elapsed = DateTime.Now - timer.StartTime;
            int remainingSeconds = timer.DurationInSeconds - (int)elapsed.TotalSeconds;

            if (remainingSeconds <= 0)
            {
                timer.OnComplete?.Invoke();
                completedTimers.Add(timerName);
            }
        }

        foreach (string timerName in completedTimers)
        {
            timers.Remove(timerName);
            if (gameDataClass != null && timerName != null)
                ClearTimerStartTime(timerName);
            Debug.Log($"Timer {timerName} completed and removed.");
        }
    }

    public string GetRemainingTime(string timerName)
    {
        if (timers.ContainsKey(timerName))
        {
            Timer timer = timers[timerName];
            TimeSpan elapsed = DateTime.Now - timer.StartTime;
            int remainingSeconds = Math.Max(timer.DurationInSeconds - (int)elapsed.TotalSeconds, 0);

            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;

            return $"{minutes:D2}:{seconds:D2}";
        }

        return ""; // Default value if timer doesn't exist
    }

    private void SaveTimerStartTime(string timerName, DateTime startTime)
    {
        string formattedTime = startTime.ToString("o"); // ISO 8601 format for easy parsing later

        if (timerName == "colorBuster")
            gameDataClass.saveData.colorBusterRecoveryTime = formattedTime;
        else if (timerName == "lineBuster")
            gameDataClass.saveData.lineBusterRecoveryTime = formattedTime;
        else if (timerName == "lifeRecovery")
            gameDataClass.saveData.lifeRecoveryTime = formattedTime;

        // Save data to file or persistent storage
        gameDataClass.SaveToFile();
    }

    private void ClearTimerStartTime(string timerName)
    {
        if (timerName == "colorBuster")
            gameDataClass.saveData.colorBusterRecoveryTime = "";
        else if (timerName == "lineBuster")
            gameDataClass.saveData.lineBusterRecoveryTime = "";
        else if (timerName == "lifeRecovery")
            gameDataClass.saveData.lifeRecoveryTime = "";

        // Save data to file or persistent storage
        gameDataClass.SaveToFile();
    }

    private string GetSavedTimerTime(string timerName)
    {
        if (timerName == "colorBuster")
            return gameDataClass.saveData.colorBusterRecoveryTime;
        else if (timerName == "lineBuster")
            return gameDataClass.saveData.lineBusterRecoveryTime;
        else if (timerName == "lifeRecovery")
            return gameDataClass.saveData.lifeRecoveryTime;

        return ""; // Default for unknown timer names
    }

    /*    public string CheckFreeLifeConditions()
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
        }*/


}
