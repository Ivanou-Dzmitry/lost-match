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
    //public Image[] stars;

    public GameObject[] stars3D;
    public Material starOnMaterial;  // Active star material
    public Material starOffMaterial;  // Inactive star material

    public GameObject starsPanel;
    public TMP_Text levelText;    
    public int level;
    public GameObject confirmPanel;

/*    [Header("Stars")]
    public Sprite starOffSprite;
    public Sprite starOnSprite;*/

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

        //fix
        Material material = levelText.fontMaterial;
        material.renderQueue = 3002;
    }

    void LoadData()
    {
        //game data check
        if (gameDataClass != null && level > 0 && level <= gameDataClass.saveData.isActive.Length)
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
        if (gameDataClass != null && level > 0 && level <= gameDataClass.saveData.isActive.Length)
        {
            SetStarMaterials();
        }                           
    }


    void SetStarMaterials()
    {
        // Get the number of active stars for the current level
        int activeStars = gameDataClass.saveData.stars[level - 1]; // Ensure level index is valid

        // Deactivate starsPanel if no stars are active
        if (activeStars == 0)
        {
            starsPanel.SetActive(false);
            return;
        }

        // Activate starsPanel in case it's deactivated and there are stars
        starsPanel.SetActive(true);

        // Loop through the stars and set materials accordingly
        for (int i = 0; i < stars3D.Length; i++)
        {
            if (i < activeStars)
            {
                // Set active material for stars within the active count
                stars3D[i].GetComponent<MeshRenderer>().materials = new Material[] { starOnMaterial };
            }
            else
            {
                // Set inactive material for remaining stars
                stars3D[i].GetComponent<MeshRenderer>().materials = new Material[] { starOffMaterial };
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
            if (stars3D[i] != null)
                stars3D[i].SetActive(true);
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
