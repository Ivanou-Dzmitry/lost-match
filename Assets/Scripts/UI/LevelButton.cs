using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    [Header("Stuff")]
    public bool isActive;
    private Button myButton;
    private int activeStars;

    [Header("Level UI")]
    public Image[] stars;
    public GameObject starsPanel;
    public TMP_Text levelText;
    public int level;
    private int highScore;
    public GameObject confirmPanel;
    

    [Header("Stars")]
    public Sprite starOffSprite;
    public Sprite starOnSprite;

    private GameData gameDataClass;

    // Start is called before the first frame update
    void Start()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        
        myButton = GetComponent<Button>();

        LoadData();
        ChooseSprite();
        ActivateStars();
    }


    void LoadData()
    {
        //game data check
        if (gameDataClass != null)
        {
            if (gameDataClass.saveData.isActive[level - 1])
            {
                isActive = true;
            }
            else
            {
                isActive = false;
            }
        }

        //active stars
        activeStars = gameDataClass.saveData.stars[level - 1];

        if (activeStars == 0)
        {
            starsPanel.SetActive(false);
        }
        else
        {
            starsPanel.SetActive(true);
            
            //show stars
            for (int i = 0; i < activeStars; i++)
            {
                stars[i].sprite = starOnSprite;
            }
        }
               
    }

    void ChooseSprite()
    {
        if (isActive)
        {
            myButton.interactable = true;
            levelText.enabled = true;
            levelText.text = "" + level;
        }
        else
        {
            myButton.interactable = false;
            levelText.enabled = false;
        }
    }

    void ActivateStars()
    {
        //show stars
        for (int i = 0; i < activeStars; i++)
        {
            stars[i].sprite = starOnSprite;
        }
    }

    public void ShowConfirmPanel(int level)
    {
        confirmPanel.GetComponent<LevelConfirmPanel>().level = level;
        
        confirmPanel.SetActive(true);            
    }

}
