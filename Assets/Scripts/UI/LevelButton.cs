using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BonusShop;

public class LevelButton : MonoBehaviour
{
    [Header("Stuff")]
    public bool isActive;
   //private Button myButton;
    private int activeStars;

    [Header("Confirm Panel UI")]
    public Image[] stars;
    public GameObject starsPanel;
    public TMP_Text levelText;    
    public int level;
    public GameObject confirmPanel;

    [Header("Stars")]
    public Sprite starOffSprite;
    public Sprite starOnSprite;

    [Header("Materials")]
    public Material materialOn;
    public Material materialOff;
    public MeshRenderer buttonMesh;

    //classes
    private GameData gameDataClass;
    private LevelGoals levelGoalsClass;
    private BonusShop bonusShopClass;

    [Header("Animation")]
    private Animator animatorElement;

    // Start is called before the first frame update
    void Start()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        levelGoalsClass = GameObject.FindWithTag("LevelGoals").GetComponent<LevelGoals>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();        

        //myButton = GetComponentInChildren<Button>();
        animatorElement = GetComponent<Animator>();

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
                if(animatorElement != null)
                {
                    animatorElement.SetTrigger("LevelOn");
                    float randomSpeed = Random.Range(0.5f, 1f);
                    animatorElement.SetFloat("Speed", randomSpeed);
                }
            }
            else
            {
                isActive = false;
                if (animatorElement != null)
                {
                    animatorElement.SetTrigger("LevelOff");
                    float randomSpeed = Random.Range(0.5f, 1f);
                    animatorElement.SetFloat("Speed", randomSpeed);
                }
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
                if(stars[i] != null)
                    stars[i].sprite = starOnSprite;
            }
        }
               
    }

    void ChooseSprite()
    {
        if (isActive)
        {
            levelText.enabled = true;
            levelText.text = "" + level;
        }
        else
        {         
            levelText.enabled = false;
        }

        Material[] materials = buttonMesh.materials;

        if (materials.Length > 0)
        {
            materials[0] = isActive ? materialOn : materialOff;
            buttonMesh.materials = materials; // Reassign the modified array back
        }
        else
        {
            Debug.LogWarning("No materials found on buttonMesh!");
        }
    }

    void ActivateStars()
    {
        //show stars
        for (int i = 0; i < activeStars; i++)
        {
            if (stars[i]!=null)
                stars[i].sprite = starOnSprite;
        }
    }


    public void ShowConfirmPanel(int level)
    {
        //chesk lives
        int lives = gameDataClass.saveData.bonuses[5];

        if (lives > 0)
        {
            confirmPanel.GetComponent<LevelConfirmPanel>().level = level;

            levelGoalsClass.GetGoals(level - 1);
   
            confirmPanel.SetActive(true);
        }
        else
        {
            //open lives shop
            bonusShopClass.OpenShop(ShopType.Lives);            

            StartCoroutine(ShowInfoAfterPanelActive());
        }
    }

    private IEnumerator ShowInfoAfterPanelActive()
    {
        yield return null; // Wait for one frame
        bonusShopClass.ShowInfo(0, "NoLives");
    }

}
