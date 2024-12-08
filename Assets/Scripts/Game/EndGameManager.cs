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
    }

    public void DecreaseCounterVal()
    {
        if (gameBoardClass.currentState != GameState.pause)
        {
            curCounterVal--;
            movesCounter.text = "" + curCounterVal;

            //for end game
            if (curCounterVal <= 0 && gameBoardClass.matchState == GameState.matching_stop)
            {
                LoseGame();
            }
        }
    }

    public void WinGame()
    {
        winPanel.SetActive(true);
        gameBoardClass.currentState = GameState.win;     

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
        levelNumberLose.text = "LEVEL " + (gameBoardClass.level + 1);

        int currentCreditsCount = gameDataClass.saveData.credits + scoreManagerClass.score;
        creditsCountLose.text = "Credits: " + currentCreditsCount;

        //reduce lives
        int currentLives = gameDataClass.saveData.bonuses[5];

        if (currentLives > 0)
        {
            currentLives = currentLives - 1;
            gameDataClass.saveData.bonuses[5] = currentLives;
        }
        
        tryPanel.SetActive(true);

        //disable button if life = 0
        DisableLooseButton();

        gameBoardClass.currentState = GameState.lose;
        curCounterVal = 0;
        movesCounter.text = "" + curCounterVal;

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(loseMusic);
        }
    }


    public void PlayNext()
    {
        if (gameDataClass.saveData.bonuses[5] > 0)
        {
            gameDataClass.saveData.levelToLoad = (gameBoardClass.level + 1);
            gameDataClass.SaveToFile();

            SceneManager.LoadScene("GameBoard");

            SaveCredits();
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
