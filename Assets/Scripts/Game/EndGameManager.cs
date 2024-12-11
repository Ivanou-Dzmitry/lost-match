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
    public ScoreManager scoreManagerClass;
    public SoundManager soundManagerClass;

    //panels
    public GameObject winPanel;
    public GameObject tryPanel;

    public TMP_Text movesCounter;
    public int curCounterVal;
    public Image movesAlarm;
    public Animator animatorAlarm;

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

    public int finalLevelNumber = 10;


    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        scoreManagerClass = GameObject.FindWithTag("ScoreManager").GetComponent<ScoreManager>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

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

        movesCounter.text = "" + curCounterVal;

        movesAlarm.enabled = false;
    }

    public void DecreaseCounterVal()
    {
        if (gameBoardClass.currentState != GameState.pause)
        {
            curCounterVal--;
            movesCounter.text = "" + curCounterVal;

            //turn on alarm
            if(curCounterVal <= 5)
            {
                movesAlarm.enabled = true;
                animatorAlarm.SetTrigger("PlayAnimation");
            }
            else
            {
                movesAlarm.enabled = false;
            }

            //for end game
/*            if (curCounterVal <= 0 && gameBoardClass.matchState == GameState.matching_stop)
            {
                LoseGame();
            }*/
        }
    }

    public void WinGame()
    {
        if(winPanel.activeSelf == false)
            winPanel.SetActive(true);

        //stop animation finalTextPanel.activeSelf
        animatorAlarm.enabled = false;

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

        for(int i = 0; i< congratPart.Length; i++)
        {
            congratPart[i].Play();
        }

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(winMusic);
        }

    }

    public void LoseGame()
    {
        if (tryPanel.activeSelf == false)
            tryPanel.SetActive(true);

        levelNumberLose.text = "LEVEL " + (gameBoardClass.level + 1);

        //stop animation
        animatorAlarm.enabled = false;

        int currentCreditsCount = gameDataClass.saveData.credits + scoreManagerClass.score;
        creditsCountLose.text = "Credits: " + currentCreditsCount;

        //reduce lives
        int currentLives = gameDataClass.saveData.bonuses[5];

        if (currentLives > 0)
        {
            currentLives = currentLives - 1;
            gameDataClass.saveData.bonuses[5] = currentLives;
        }

        //disable button if life = 0
        DisableLooseButton();
        
        curCounterVal = 0;
        movesCounter.text = "" + curCounterVal;

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(loseMusic);
        }
    }


    public void PlayNext()
    {
        int nextLevelNumber = gameBoardClass.level + 1;

        if (gameDataClass.saveData.bonuses[5] > 0 && nextLevelNumber < finalLevelNumber)
        {            
            gameDataClass.saveData.levelToLoad = (gameBoardClass.level + 1);
            gameDataClass.SaveToFile();

            SaveCredits();
            SceneManager.LoadScene("GameBoard");            
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

    private void Update()
    {
        DisableLooseButton();      
    }

    private void DisableLooseButton()
    {
        if (gameDataClass.saveData.bonuses[5] > 0)
        {
            retryLooseButton.interactable = true;
            Animator animator = retryLooseButton.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
            }
        }
        else
        {
            retryLooseButton.interactable = false;
            Animator animator = retryLooseButton.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }
    }

    public void QuitAndLooseLife()
    {
        if (gameDataClass.saveData.bonuses[5] > 0)
        {
            gameDataClass.saveData.bonuses[5] -= 1; //minus 1 life
            gameDataClass.SaveToFile();
        }
    }

}
