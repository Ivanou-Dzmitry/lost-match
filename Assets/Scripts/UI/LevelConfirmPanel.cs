using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelConfirmPanel : MonoBehaviour
{
    [Header("Level Info")]
    public string sceneToLoadName;
    public int level;
    public int levelToLoad;
    private int activeStars;

    [Header("UI")]
    public Image[] stars;
    public TMP_Text highScoreText;
    public TMP_Text headerText;
    private int highScore;

    [Header("Stars")]
    public Sprite starOffSprite;
    public Sprite starOnSprite;

    private GameData gameDataClass;
    [Header("GUI")]
    public GameObject confirmPanel;


    void OnEnable()
    {
        confirmPanel.SetActive(false);

        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        LoadData(); //from file
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    void LoadData()
    {
        //game data check
        if (gameDataClass != null)
        {
            activeStars = gameDataClass.saveData.stars[level - 1];
            highScore = gameDataClass.saveData.highScore[level - 1];
        }

        levelToLoad = level - 1;

        //load game immediatly
        if (highScore == 0)
        {
            Play();
        } else
        {
            confirmPanel.SetActive(true);
            highScoreText.text = "Items collected: " + highScore;
            headerText.text = "Level " + level + " Records";

            for (int i = 0; i < activeStars; i++)
            {
                stars[i].sprite = starOnSprite;
            }
        }

    }
    public void Play()
    {
        gameDataClass.saveData.levelToLoad = levelToLoad;
        gameDataClass.SaveToFile();

        SceneManager.LoadScene(sceneToLoadName);
    }
}
