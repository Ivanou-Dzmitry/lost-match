using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    private string filePath;
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

    void Start()
    {
        string fileName = "LM_GameSessionLog.csv";
        //filePath = Path.Combine(Environment.CurrentDirectory, fileName);

        filePath = Path.Combine(Application.persistentDataPath, fileName);


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

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Level,Col,Row,Duration(sec),Score,Retry,Interrupted,Win, Goal1,Goal2,Goal3,Moves,BuyMoves,e1,e2,e3,e4,e5\n");
        }
    }

    public void EndSession()
    {
        DateTime endTime = DateTime.Now;
        TimeSpan duration = endTime - startTime;
        int secondsOnly = duration.Seconds;

        string elementsCollectedStr = string.Join(";", elementsCollected);
        string bombsCreatedStr = string.Join(";", bombsCreated);

        //Debug.Log("F:" + duration);

        //{startTime},{endTime},{ elementsCollectedStr},{ bombsCreatedStr}
        string logEntry = $"{levelNumber}, {col}, {row}, {secondsOnly},{score},{retry},{interrupt},{win}, {goal1}, {goal2}, {goal3}, {moves},{buyMoves},{elem1},{elem2},{elem3},{elem4},{elem5}";
        
        try
        {
            // Use FileStream with FileShare.ReadWrite to avoid file access conflicts
            using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
            {
                writer.WriteLine(logEntry);
            }

            Debug.Log($"Log file saved at: {filePath}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"Error writing to log file: {ex.Message}");
            // Optionally, implement a retry logic or handle the error appropriately.
        }
    }


}
