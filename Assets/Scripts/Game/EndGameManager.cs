using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GameType
{
    Moves,
    Time
}

[System.Serializable]
public class EndGameRequriments
{
    public GameType gameType;

    public int counterValue;
}

public class EndGameManager : MonoBehaviour
{
    public EndGameRequriments EndGameReqClass;

    //class
    private GameBoard gameBoardClass;

    //panels
    public GameObject winPanel;
    public GameObject tryPanel;

    public TMP_Text counter;
    public int curCounterVal;

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        SetGameType();
        SetupGame();
    }

    public void SetGameType()
    {
        if (gameBoardClass != null)
        {
            if (gameBoardClass.level < gameBoardClass.worldClass.levels.Length)
            {
                if (gameBoardClass.worldClass.levels[gameBoardClass.level] != null)
                {
                    EndGameReqClass = gameBoardClass.worldClass.levels[gameBoardClass.level].endGameRequrimentsForLevel;
                }
            }
        }
    }

    void SetupGame()
    {
        curCounterVal = EndGameReqClass.counterValue;

        counter.text = "" + curCounterVal;
    }

    public void DecreaseCounterVal()
    {
        if (gameBoardClass.currentState != GameState.pause)
        {
            curCounterVal--;
            counter.text = "" + curCounterVal;

            //for end game
            if (curCounterVal <= 0)
            {
                LoseGame();
            }
        }
    }

    public void WinGame()
    {
        winPanel.SetActive(true);
        gameBoardClass.currentState = GameState.win;

        curCounterVal = 0;
        counter.text = "" + curCounterVal;
    }

    public void LoseGame()
    {
        tryPanel.SetActive(true);

        gameBoardClass.currentState = GameState.lose;
        curCounterVal = 0;
        counter.text = "" + curCounterVal;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
