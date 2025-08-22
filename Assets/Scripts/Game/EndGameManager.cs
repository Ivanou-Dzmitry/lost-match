using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

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
    private GoalManager goalManagerClass;

    //panels
    public GameObject winPanel;
    public GameObject tryPanel;

    //confirm
    public GameObject confirmPanel;
    private LevelGoals levelGoalsClass;

    public GameObject nextButton;

    [Header("Alarm")]
    public TMP_Text movesCounter;
    public int curCounterVal;
    public Image movesAlarm;
    public Animator animatorAlarm; //animator
    public AudioClip levelMusic;

    [Header("Win Panel")]
    public TMP_Text levelNumber;
    
    public TMP_Text creditsCount;
    public TMP_Text starBonusCountText;
    public TMP_Text totalCredits;

    public Image[] levelStars;
    public Sprite[] levelStarsSpite;
    public ParticleSystem[] starsPart;
    public AudioClip winMusic;

    [Header("Congrat")]
    public ParticleSystem[] congratPart;

    [Header("Lose Panel")]
    public Button retryLooseButton;
    public AudioClip loseMusic;

    public int finalLevelNumber; //!Important

    private bool thisRetry = false;
    private bool thisInterrupt = false;
    private bool thisWin = false;
    private bool thisBuyMoves = false;

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        levelGoalsClass = GameObject.FindWithTag("LevelGoals").GetComponent<LevelGoals>();
        scoreManagerClass = GameObject.FindWithTag("ScoreManager").GetComponent<ScoreManager>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        goalManagerClass = GameObject.FindWithTag("GoalManager").GetComponent<GoalManager>();

        SetGameType();

        SetupGame();

        //assign stars
        for (int i = 0; i < levelStars.Length; i++)
        {
            levelStars[i].sprite = levelStarsSpite[1];
        }

        //get count
        finalLevelNumber = gameBoardClass.totalLevels;

        //hide next button on last level
        if (gameBoardClass.loadedLevel == finalLevelNumber)
        {
            nextButton.SetActive(false); // Hide button
        }
        else
        {
            nextButton.SetActive(true);  // Show button
        }
    }

    public void SetGameType()
    {
        if (gameBoardClass != null && gameBoardClass.worldManager != null && gameBoardClass.worldClass != null && gameBoardClass.level != null)
        {

            EndGameReqClass = gameBoardClass.level.endGameRequrimentsForLevel;

            //check description
            if (EndGameReqClass.counterValue == 0)
            {
                Debug.LogWarning("Counter Value = 0. Set Counter Value for level!");
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
        //show panel
        if (winPanel.activeSelf == false)
            winPanel.SetActive(true);

        //stop animation finalTextPanel.activeSelf
        AlarmAnimation(curCounterVal, false);

        movesCounter.text = "" + curCounterVal;

        //level number
        levelNumber.text = "LEVEL " + (gameBoardClass.loadedLevel);

        //show stars binus
        // Show star bonus if 1-3 stars are earned
        int stars = scoreManagerClass.numberStars;

        //for save
        int levelInList = Mathf.Max(0, gameBoardClass.loadedLevel - 1);
        
        //get star record
        int currentStarsCount = gameDataClass.saveData.stars[levelInList];

        //set star record
        if (stars > currentStarsCount)
        {
            gameDataClass.saveData.stars[levelInList] = stars;

            if (gameDataClass != null)
                gameDataClass.SaveToFile();
        }

        int bonus = 0;

        //collect star bonus
        if (stars >= 1 && stars <= 3 && starBonusCountText != null)
        {
            bonus = (gameBoardClass.scoreGoals[stars - 1])/3; //bonus logic
            scoreManagerClass.score += bonus;            
        }

        //credits. saved if close, but not save if retry
        int currentCreditsCount = scoreManagerClass.score - bonus;

        //show earned credits
        creditsCount.text = $"{currentCreditsCount}";
        starBonusCountText.text = $"{bonus}";
        
        //total
        if(totalCredits != null)
            totalCredits.text = $"{scoreManagerClass.score}";

        //get current hi score
        int hiScore = gameDataClass.saveData.highScore[levelInList];

        //set hi score
        if (scoreManagerClass.score > hiScore)
        {
            gameDataClass.saveData.highScore[levelInList] = scoreManagerClass.score;

            if (gameDataClass != null)
                gameDataClass.SaveToFile();
        }

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

        thisWin = true;
    }

    public void LoseGame()
    {
        //open moves shop
        if (tryPanel.activeSelf == false)
        {
            bonusShopClass.IntToShopType(2);
        }           

        //stop animation
        AlarmAnimation(curCounterVal, false);

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

        thisInterrupt = true;
        thisWin = false;
        //log
        Log();
    }


    public void PlayNext()
    {                
        int nextLevelNumber = gameBoardClass.loadedLevel + 1;

        thisWin = true;

        //get moves
        if (nextLevelNumber < finalLevelNumber)
        {
            winPanel.SetActive(false);

            LevelConfirmPanel lCP = confirmPanel.GetComponent<LevelConfirmPanel>();

            lCP.level = nextLevelNumber;
            lCP.levelToLoad = nextLevelNumber;

            levelGoalsClass.GetGoals(nextLevelNumber);

            confirmPanel.SetActive(true);

            SaveCredits();
            Log();
        }
        else
        {
            SaveCredits();
            Log();
            SceneManager.LoadScene("Levels");
        }
    }

    public void RetryLevel()
    {
        //if enought lifes - reload
        if (gameDataClass.saveData.bonuses[5] > 0)
        {
            gameDataClass.saveData.levelToLoad = (gameBoardClass.loadedLevel);
            gameDataClass.SaveToFile();
            
            //log
            thisRetry = true;
            thisWin = true;
            Log();

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

        thisBuyMoves = true;
    }

    public void Log()
    {
        //lvl
        gameBoardClass.log.levelNumber = gameBoardClass.loadedLevel;

        //size
        gameBoardClass.log.col = gameBoardClass.column;
        gameBoardClass.log.row = gameBoardClass.row;

        //credits
        gameBoardClass.log.score = scoreManagerClass.score;

        gameBoardClass.log.retry = thisRetry;
        gameBoardClass.log.interrupt = thisInterrupt;
        gameBoardClass.log.win = thisWin;

        int goalsCount = goalManagerClass.levelGoals.Length;

        try
        {
            gameBoardClass.log.goal1 = goalManagerClass.levelGoals[0].numberCollectedGoals;

            if (goalsCount > 1)
                gameBoardClass.log.goal2 = goalManagerClass.levelGoals[1].numberCollectedGoals;

            if (goalsCount > 2)
                gameBoardClass.log.goal3 = goalManagerClass.levelGoals[2].numberCollectedGoals;
        }
        catch
        {
            gameBoardClass.log.goal1 = 0;
            gameBoardClass.log.goal2 = 0;
            gameBoardClass.log.goal3 = 0;
        }


        gameBoardClass.log.moves = curCounterVal;

        gameBoardClass.log.buyMoves = thisBuyMoves;

        gameBoardClass.log.levelID = gameBoardClass.levelUID;

        gameBoardClass.log.EndSession();
    }

}
