using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    private string gameLogFilePath;
    private DateTime startTime;
    public Dictionary<string, int> elementsCollected;
    public Dictionary<string, int> bombsCreated;
    public int score;
    public int levelNumber;
    public int col;
    public int row;
    //game state
    public bool retry;
    public bool interrupt;
    public bool win;
    //goals
    public int goal1;
    public int goal2;
    public int goal3;
    //moves
    public int moves;
    public bool buyMoves;
    //elements
    public int elem1;
    public int elem2;
    public int elem3;
    public int elem4;
    public int elem5;
    
    //boosters
    public int lineHB;
    public int lineVB;
    public int wrapB;
    public int colorB;

    //level id
    public string levelID;

    const string SYS_LOG_FILE_NAME = "lm_syslog.csv";

    //app log
    private string sysLogFilePath;
    const string GAME_LOG_FILE_NAME = "LM_GameSessionLog.csv";

    void Start()
    {
        //log for sys
        sysLogFilePath = Path.Combine(Application.persistentDataPath, SYS_LOG_FILE_NAME);

        if (!File.Exists(sysLogFilePath))
        {
            File.WriteAllText(sysLogFilePath, "Time, Message\n");
        }

        //game log
        gameLogFilePath = Path.Combine(Application.persistentDataPath, GAME_LOG_FILE_NAME);

        //set values
        startTime = DateTime.Now;
        elementsCollected = new Dictionary<string, int>();
        bombsCreated = new Dictionary<string, int>();
        score = 0;
        levelNumber = 0;
        col = 0;
        row = 0;
        retry = false;
        interrupt = false;
        win = false;
        goal1 = 0;
        goal2 = 0;
        goal3 = 0;
        moves = 0;
        buyMoves = false;
        elem1 = 0; elem2 = 0; elem3 = 0; elem4 = 0; elem5 = 0;

        lineHB = 0; lineVB = 0; wrapB = 0; colorB = 0; levelID = "";

        //add header of game log
        if (!File.Exists(gameLogFilePath))
        {
            File.WriteAllText(gameLogFilePath, "Level,Col,Row,Duration(sec),Score,Retry,Interrupted,Win, Goal1,Goal2,Goal3,Moves,BuyMoves,e1,e2,e3,e4,e5, lHor, lVert, wrap, clrB, lvlID \n");
            WriteSysLog("Game log created");
        }
        else
        {
            WriteSysLog("Game log OK");
        }
    }

    /// <summary>
    /// Write a line into the system log file.
    /// </summary>
    public void WriteSysLog(string message)
    {
        if(sysLogFilePath==null)            
            sysLogFilePath = Path.Combine(Application.persistentDataPath, SYS_LOG_FILE_NAME);

        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string line = $"{timeStamp}, {message}";
        File.AppendAllText(sysLogFilePath, line + Environment.NewLine);
    }

    public void EndSession()
    {
        DateTime endTime = DateTime.Now;
        TimeSpan duration = endTime - startTime;
        int secondsOnly = duration.Seconds;

        string elementsCollectedStr = string.Join(";", elementsCollected);
        string bombsCreatedStr = string.Join(";", bombsCreated);
       
        //{startTime},{endTime},{ elementsCollectedStr},{ bombsCreatedStr}
        string logEntry = $"{levelNumber}, {col}, {row}, {secondsOnly},{score},{retry},{interrupt},{win}, {goal1}, {goal2}, {goal3}, {moves},{buyMoves},{elem1},{elem2},{elem3},{elem4},{elem5}, {lineHB}, {lineVB}, {wrapB}, {colorB}, {levelID}";
        
        try
        {
            // Use FileStream with FileShare.ReadWrite to avoid file access conflicts
            using (FileStream fs = new FileStream(gameLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
            {
                writer.WriteLine(logEntry);
            }

            WriteSysLog($"Log file saved at: {gameLogFilePath}");
        }
        catch (IOException ex)
        {
            WriteSysLog($"Error writing to log file: {ex.Message}");
        }
    }
}
