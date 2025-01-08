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

        RestoreSavedTimers();

        InvokeRepeating(nameof(CheckTimers), 0f, 1f);
    }

    public void CreateTimer(string timerName, int durationInSeconds, Action onStart, Action onComplete)
    {
        if (!timers.ContainsKey(timerName))
        {
            DateTime endTime = DateTime.Now.AddSeconds(durationInSeconds);

            timers[timerName] = new Timer
            {
                StartTime = DateTime.Now,
                DurationInSeconds = durationInSeconds,
                OnStart = onStart, // Optional
                OnComplete = onComplete
            };

            // Save the end time instead of the start time
            if (timerName == "colorBuster")
                gameDataClass.saveData.colorBusterRecoveryTime = endTime.ToString();
            else if (timerName == "lineBuster")
                gameDataClass.saveData.lineBusterRecoveryTime = endTime.ToString();
            else if (timerName == "lifeRecovery")
                gameDataClass.saveData.lifeRecoveryTime = endTime.ToString();

            gameDataClass.SaveToFile(); // Persist changes
            Debug.Log($"Created timer {timerName} with duration {durationInSeconds} seconds.");
        }
        else
        {
            Debug.LogWarning($"Timer {timerName} already exists.");
        }
    }

    private void CheckTimers()
    {
        Debug.Log("Checking timers...");

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

            if (remainingSeconds > 0)
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
        {
            gameDataClass.saveData.colorBusterRecoveryTime = "";
            gameDataClass.saveData.bonuses[1] = 0;
        }            
        else if (timerName == "lineBuster")
        {
            gameDataClass.saveData.lineBusterRecoveryTime = "";
            gameDataClass.saveData.bonuses[11] = 0;
        }            
        else if (timerName == "lifeRecovery")
        {
            gameDataClass.saveData.lifeRecoveryTime = "";
        }            

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

    private void RestoreSavedTimers()
    {
        RestoreTimer("colorBuster", gameDataClass.saveData.colorBusterRecoveryTime, () => Debug.Log("Color Buster timer completed!"));
        RestoreTimer("lineBuster", gameDataClass.saveData.lineBusterRecoveryTime, () => Debug.Log("Line Buster timer completed!"));
        RestoreTimer("lifeRecovery", gameDataClass.saveData.lifeRecoveryTime, () => Debug.Log("Life Recovery timer completed!"));
    }

    private void RestoreTimer(string timerName, string savedEndTime, Action onComplete)
    {
        if (!string.IsNullOrEmpty(savedEndTime) && DateTime.TryParse(savedEndTime, out DateTime endTime))
        {
            TimeSpan remainingTime = endTime - DateTime.Now;

            if (remainingTime.TotalSeconds > 0)
            {
                timers[timerName] = new Timer
                {
                    StartTime = DateTime.Now, // The new start time for the timer
                    DurationInSeconds = (int)remainingTime.TotalSeconds,
                    OnStart = null, // Optional if you don't need OnStart here
                    OnComplete = onComplete
                };

                Debug.Log($"Restored timer {timerName} with {remainingTime.TotalSeconds} seconds remaining.");
            }
            else
            {
                // Timer already expired
                onComplete?.Invoke();
                ClearTimerStartTime(timerName);
                Debug.Log($"Timer {timerName} already expired.");
            }
        }
    }

}
