using System.Collections;
using System;
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
    private BonusShop bonusShopClass;
    private TimeManager timeManagerClass;

    public List<GoalPanel> currentGoals = new List<GoalPanel>();
    public GameObject goalPrefab;
    public GameObject goalIntroParent;

    public TMP_Text goalsDescriptionText;

    [Header("GUI")]
    public GameObject confirmPanel;

    [Header("Timless Busters")]    
    public GameObject buster01Prefab;
    public GameObject buster11Prefab;

    private BonusButton buster01;
    private BonusButton buster11;

    private Coroutine updateCoroutine;


    void OnEnable()
    {
        //confirmPanel.SetActive(false);

        //classes
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        levelGoalsClass = GameObject.FindWithTag("LevelGoals").GetComponent<LevelGoals>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        timeManagerClass = GameObject.FindWithTag("TimeManager").GetComponent<TimeManager>();

        //stars off
        for (int i = 0; i < 3; i++)
        {
            stars[i].sprite = starOffSprite;
        }

        if (gameDataClass != null)
            LoadData(); //from file

        if (levelGoalsClass != null)
            SetupIntroGoals();

        buster01 = buster01Prefab.GetComponent<BonusButton>();
        buster11 = buster11Prefab.GetComponent<BonusButton>();

        // Start the coroutine when the object is enabled
        updateCoroutine = StartCoroutine(UpdateBusterTime());
    }

    void OnDisable()
    {
        // Stop the coroutine when the object is disabled
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    private IEnumerator UpdateBusterTime()
    {
        while (true)
        {
            // Update the UI text once per second
            if (bonusShopClass != null)
            {

                if (buster01Prefab != null)
                {
                    
                    string time1 = timeManagerClass.GetRemainingTime("colorBuster");

                    if (buster01.bonusCount > 0 && time1 == "")
                    {
                        buster01.useBusterPanel.SetActive(true);
                    }
                    else
                    {
                        buster01.useBusterPanel.SetActive(false);
                    }
                        

                    if (time1 != "")
                    {
                        buster01.busterTimePanel.SetActive(true);
                        buster01.busterTimeText.text = "" + time1;
                        buster01.addBusterPanel.SetActive(false);
                        buster01.clockIcon.SetActive(true);
                    }
                    else
                    {
                        buster01.busterTimePanel.SetActive(false);
                        if (buster01.bonusCount == 0)
                        {
                            buster01.addBusterPanel.SetActive(true);
                        }
                        else
                        {
                            buster01.addBusterPanel.SetActive(false);
                        }
                            
                        buster01.clockIcon.SetActive(false);
                    }
                }

                if (buster11Prefab != null)
                {                    
                    string time11 = timeManagerClass.GetRemainingTime("lineBuster");

                    if (buster11.bonusCount > 0 && time11 == "")
                    {
                        buster11.useBusterPanel.SetActive(true);
                    }
                    else
                    {
                        buster11.useBusterPanel.SetActive(false);
                    }
                        

                    if (time11 != "" && buster11.bonusCount > 0)
                    {
                        buster11.busterTimePanel.SetActive(true);
                        buster11.busterTimeText.text = "" + time11;
                        buster11.addBusterPanel.SetActive(false);
                        buster11.clockIcon.SetActive(true);
                    }
                    else
                    {
                        buster11.busterTimePanel.SetActive(false);

                        if (buster11.bonusCount == 0)
                        {
                            buster11.addBusterPanel.SetActive(true);
                        }
                        else
                        {
                            buster11.addBusterPanel.SetActive(false);
                        }                            

                        buster11.clockIcon.SetActive(false);
                    }
                }
            }
            yield return new WaitForSeconds(1f); // Wait for 1 second
        }
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
        highScoreText.text = "Records: " + highScore + " items, " + activeStars + " stars";
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
            introGoal.transform.localScale = new Vector3(1.5f, 1.5f, 1.0f); //scale
            introGoal.name = "LevelGoal-" + i;

/*            Canvas goalCanvas = introGoal.GetComponentInChildren<Canvas>();
            goalCanvas.sortingLayerName = "UI";*/

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
            Destroy(child.gameObject);
        }
    }


    public void Play()
    {
        gameDataClass.saveData.levelToLoad = levelToLoad;
        gameDataClass.SaveToFile();

        SceneManager.LoadScene(sceneToLoadName);
    }

}
