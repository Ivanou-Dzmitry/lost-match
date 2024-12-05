using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    //class
    private GameBoard gameBoardClass;
    private GameData gameDataClass;
    public EndGameRequriments EndGameReqClass;
    public ScoreManager scoreManagerClass;

    //panels
    public GameObject winPanel;
    public GameObject tryPanel;

    public TMP_Text counter;
    public int curCounterVal;

    [Header("Win Panel")]
    public TMP_Text levelNumber;
    public TMP_Text creditsCount;
    public Image[] levelStars;
    public Sprite[] levelStarsSpite;
    public ParticleSystem[] starsPart;

    [Header("Congrat")]
    public ParticleSystem[] congratPart;

    [Header("Lose Panel")]
    public TMP_Text levelNumberLose;    



    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        scoreManagerClass = GameObject.FindWithTag("ScoreManager").GetComponent<ScoreManager>();

        SetGameType();
        SetupGame();

        for (int i = 0; i < levelStars.Length; i++)
        {
            levelStars[i].sprite = levelStarsSpite[1];
        }
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

        levelNumber.text = "LEVEL " + (gameBoardClass.level + 1);

        //credits. saved if close, but not save if retry
        int currentCreditsCount = gameDataClass.saveData.credits + scoreManagerClass.tempScore;

        creditsCount.text = "Credits: " + currentCreditsCount;

        //turn on stars
        for (int i = 0; i < scoreManagerClass.numberStars; i++)
        {
            levelStars[i].sprite = levelStarsSpite[0];
            starsPart[i].Play();
        }

        for(int i = 0; i< congratPart.Length; i++)
        {
            congratPart[i].Play();
        }
        


    }

    public void LoseGame()
    {
        //lives
        int currentLives = gameDataClass.saveData.bonuses[5];
        currentLives = currentLives - 1;
        gameDataClass.saveData.bonuses[5] = currentLives;

        tryPanel.SetActive(true);

        gameBoardClass.currentState = GameState.lose;
        curCounterVal = 0;
        counter.text = "" + curCounterVal;
    }


    public void PlayNext()
    {
        gameDataClass.saveData.levelToLoad = (gameBoardClass.level + 1);
        gameDataClass.SaveToFile();

        SceneManager.LoadScene("GameBoard");

        SaveCredits();
    }

    public void RetryLevel()
    {
        gameDataClass.saveData.levelToLoad = (gameBoardClass.level);
        gameDataClass.SaveToFile();

        SceneManager.LoadScene("GameBoard");        
    }

    public void SaveCredits()
    {
        //add colected credits
        gameDataClass.saveData.credits += scoreManagerClass.tempScore;
        gameDataClass.SaveToFile();
    }

}
