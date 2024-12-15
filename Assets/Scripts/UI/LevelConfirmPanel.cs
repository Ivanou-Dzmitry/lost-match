using System.Collections.Generic;
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
    private LevelGoals levelGoalsClass;

    public List<GoalPanel> currentGoals = new List<GoalPanel>();
    public GameObject goalPrefab;
    public GameObject goalIntroParent;

    public TMP_Text goalsDescriptionText;

    [Header("GUI")]
    public GameObject confirmPanel;


    void OnEnable()
    {
        //confirmPanel.SetActive(false);

        //classes
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        levelGoalsClass = GameObject.FindWithTag("LevelGoals").GetComponent<LevelGoals>();

        //stars off
        for (int i = 0; i < 3; i++)
        {
            stars[i].sprite = starOffSprite;
        }

        if (gameDataClass != null)
            LoadData(); //from file

        if (levelGoalsClass != null)
            SetupIntroGoals();
    }

    void LoadData()
    {
        activeStars = 0;

        //game data check
        if (gameDataClass != null)
        {
            activeStars = gameDataClass.saveData.stars[level - 1];
            highScore = gameDataClass.saveData.highScore[level - 1];
        }
        else
        {
            activeStars = 3;
            highScore = 9999;
        }

        levelToLoad = level - 1;

        //confirmPanel.SetActive(true);

        //set text
        highScoreText.text = "Collected: " + highScore;
        headerText.text = "LEVEL " + level;

        //stars turn on
        for (int i = 0; i < activeStars; i++)
        {
            stars[i].sprite = starOnSprite;
        }
        
    }

    //add goals
    void SetupIntroGoals()
    {
        for (int i = 0; i < levelGoalsClass.levelGoals.Length; i++)
        {
            //intro prefabs
            GameObject introGoal = Instantiate(goalPrefab, goalIntroParent.transform.position, Quaternion.identity);
            introGoal.transform.SetParent(goalIntroParent.transform);
            introGoal.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            introGoal.name = "LevelGoal-" + i;

            GoalPanel introPanel = introGoal.GetComponent<GoalPanel>();
            introPanel.thisSprite = levelGoalsClass.levelGoals[i].goalSprite;
            introPanel.thisString = "" + levelGoalsClass.levelGoals[i].numberGoalsNeeded; //goals 
        }
        
        goalsDescriptionText.text = "" + levelGoalsClass.goalDescription;
    }

    //delete goals on close panel
    public void DeleteAllChildren(GameObject parent)
    {
        // Loop through each child of the parent object
        foreach (Transform child in parent.transform)
        {
            // Destroy each child GameObject
            GameObject.Destroy(child.gameObject);
        }
    }


    public void Play()
    {
        gameDataClass.saveData.levelToLoad = levelToLoad;
        gameDataClass.SaveToFile();

        SceneManager.LoadScene(sceneToLoadName);
    }
}
