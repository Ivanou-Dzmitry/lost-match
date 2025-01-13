using System.Diagnostics.CodeAnalysis;
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
    private ScoreManager scoreManagerClass;
    private SoundManager soundManagerClass;
    private BonusShop bonusShopClass;

    //panels
    public GameObject winPanel;
    public GameObject tryPanel;

    //confirm
    public GameObject confirmPanel;
    private LevelGoals levelGoalsClass;    

    [Header("Alarm")]
    public TMP_Text movesCounter;
    public int curCounterVal;
    public Image movesAlarm;
    public Animator animatorAlarm; //animator
    public AudioClip levelMusic;

    [Header("Moves Shop")]


    [Header("Win Panel")]
    public TMP_Text levelNumber;
    public TMP_Text creditsCount;
    public Image[] levelStars;
    public Sprite[] levelStarsSpite;
    public ParticleSystem[] starsPart;
    public AudioClip winMusic;

    [Header("Congrat")]
    public ParticleSystem[] congratPart;

    [Header("Lose Panel")]
    public TMP_Text levelNumberLose;
    public TMP_Text creditsCountLose;
    public Button retryLooseButton;
    public AudioClip loseMusic;

    public int finalLevelNumber = 50; //!Important

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        levelGoalsClass = GameObject.FindWithTag("LevelGoals").GetComponent<LevelGoals>();
        scoreManagerClass = GameObject.FindWithTag("ScoreManager").GetComponent<ScoreManager>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();        

        SetGameType();
        SetupGame();

        for (int i = 0; i < levelStars.Length; i++)
        {
            levelStars[i].sprite = levelStarsSpite[1];
        }

        finalLevelNumber = gameDataClass.saveData.isActive.Length;
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

        movesCounter.text = "" + curCounterVal;

        AlarmAnimation(curCounterVal, true);
    }

    public void DecreaseCounterVal()
    {
        if (gameBoardClass.currentState != GameState.pause)
        {
            curCounterVal--;

            //avoid - values in counter
            if(curCounterVal >= 0)
            {
                movesCounter.text = "" + curCounterVal;
                AlarmAnimation(curCounterVal, true);
            }
            else
            {
                movesCounter.text = "0";
                AlarmAnimation(curCounterVal, false);
            }                
            
        }
    }

    private void AlarmAnimation(int movesValue, bool value)
    {
        animatorAlarm.enabled = value;

        //turn on alarm
        if (movesValue <= 5 && movesValue!=0)
        {
            movesAlarm.enabled = true;
            animatorAlarm.SetTrigger("PlayAnimation");            
        }
        else
        {
            movesAlarm.enabled = false;
        }

        //speed
        if(movesValue > 3)
        {
            animatorAlarm.speed = 1;
        }
        else
        { 
            animatorAlarm.speed = 2;
        }
    }

    public void WinGame()
    {
        if(winPanel.activeSelf == false)
            winPanel.SetActive(true);

        //stop animation finalTextPanel.activeSelf
        AlarmAnimation(curCounterVal, false);

        movesCounter.text = "" + curCounterVal;

        levelNumber.text = "LEVEL " + (gameBoardClass.level + 1);

        //credits. saved if close, but not save if retry
        int currentCreditsCount = gameDataClass.saveData.credits + scoreManagerClass.score;

        creditsCount.text = "Credits: " + currentCreditsCount;

        //turn on stars
        for (int i = 0; i < scoreManagerClass.numberStars; i++)
        {
            levelStars[i].sprite = levelStarsSpite[0];
            starsPart[i].Play();
        }

        //particles
        for (int i = 0; i< congratPart.Length; i++)
        {
            congratPart[i].Play();
        }

        //music
        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(winMusic);
        }

    }

    public void LoseGame()
    {
        //open moves shop
        if (tryPanel.activeSelf == false)
        {
            bonusShopClass.IntToShopType(2);
        }           

        levelNumberLose.text = "LEVEL " + (gameBoardClass.level + 1);

        //stop animation
        AlarmAnimation(curCounterVal, false);

        int currentCreditsCount = scoreManagerClass.score;
        creditsCountLose.text = "You collect " + currentCreditsCount;

        //zero moves
        curCounterVal = 0;
        gameDataClass.saveData.bonuses[6] = 0;        
        movesCounter.text = "" + curCounterVal;

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(loseMusic);
        }
    }

    public void ReduceLives()
    {
        int livesCount = gameDataClass.saveData.bonuses[5];
      
        if (livesCount > 0 && gameDataClass != null)
        {
            livesCount -= 1;
            gameDataClass.saveData.bonuses[5] = livesCount; //minus 1 life
            gameDataClass.SaveToFile();
        }
    }


    public void PlayNext()
    {
        int nextLevelNumber = gameBoardClass.level + 1;

       
        //get moves
        if (nextLevelNumber < finalLevelNumber)
        {
            winPanel.SetActive(false);

            LevelConfirmPanel lCP = confirmPanel.GetComponent<LevelConfirmPanel>();

            lCP.level = nextLevelNumber + 1;
            lCP.levelToLoad = nextLevelNumber;

            levelGoalsClass.GetGoals(nextLevelNumber);

            confirmPanel.SetActive(true);

            SaveCredits();          
        }
        else
        {
            SaveCredits();
            SceneManager.LoadScene("Levels");
        }
    }

    public void RetryLevel()
    {
        if (gameDataClass.saveData.bonuses[5] > 0)
        {
            gameDataClass.saveData.levelToLoad = (gameBoardClass.level);
            gameDataClass.SaveToFile();

            SceneManager.LoadScene("GameBoard");
        }
    }

    //save credit only if press Next or go to levels screen
    public void SaveCredits()
    {
        //add colected credits if Close or Next
        gameDataClass.saveData.credits += scoreManagerClass.score;
        gameDataClass.SaveToFile();
    }

    public void BuyMoves()
    {
        curCounterVal = gameDataClass.saveData.bonuses[6];
        movesCounter.text = "" + gameDataClass.saveData.bonuses[6];
        gameBoardClass.currentState = GameState.move;
        soundManagerClass.PlayMusic(levelMusic);

        AlarmAnimation(curCounterVal, true);
    }


/*    public void QuitAndLooseLife()
    {
        if (gameDataClass.saveData.bonuses[5] > 0)
        {
            gameDataClass.saveData.bonuses[5] -= 1; //minus 1 life
            gameDataClass.SaveToFile();
        }
    }*/

}
