using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System;


public class BonusShop : MonoBehaviour
{
    public enum ShopState
    {
        Levels,
        Game,
        SetBonus,
        SetLastBonus
    }

    public enum ShopType
    {
        Busters,
        Lives,
        Moves,
        Closed
    }

    public ShopState shopState;

    //for different types
    public ShopType shopType;

    //classes
    private GameData gameDataClass;
    private TimeManager timeManagerClass;
    private SoundManager soundManagerClass;
    private GameBoard gameBoardClass;

    [Header("End Game Class")]
    public EndGameManager endGameManagerClass;

    //temp data
    public int[] tempBonuses;
    public int tempCreditsCount;

    private int creditsCount;
    public int[] ordersCount;

    [Header("Shop Panel")]
    public GameObject[] shopPanel;

    [Header("Shop Name")]
    public TMP_Text[] shopName;

    [Header("Sound")]
    public AudioClip buySound;

    [Header("Credits")]
    public TMP_Text creditsCountPanelText;
    
    public TMP_Text[] creditsCountText;

    //public TMP_Text creditsCountShopText; //credits in shop

    public Slider creditsCountSlider; //slider
    public Button buyButton;

    [Header("Notification Stuff")]
    private TMP_Text infoText;    //The store displays boosters that are available in the equipment    
    public float fadeDuration = 2.0f; // Duration of the fade
    private Coroutine fadeOutCoroutine;
    private int timeLeft;
    
    //update 1 per sec
    private Coroutine updateCoroutine;
    public bool conditionLife = false;  // The condition to toggle the icon


    [Header("ClockIcons")]
    public GameObject livesBusterOnPanel;
    public GameObject livesBusterShop;

    //public GameObject timePanel;
    //public TMP_Text timeLeftText;

    public TMP_Text livesCount;

    //!important
    int bonusCount = 12;

    public ParticleSystem buyParticles01;

    [Header("Description Panel")]
    public int bonusSelected;
    public GameObject bonusDescPanel;
    public TMP_Text bonusName;
    public TMP_Text bonusDescription;
    public Image bonusImage;

    [Header("Bonus Description")]
    public Sprite[] bonusPicture;
    public string[] bonusNameString;
    public string[] bonusDescString;

    [Header("Busters")]
    private List<int> bustersTime = new List<int>();    

    private List<string> defInfoText = new List<string>();
    
    private Color defaultInfoTextColor = new Color(0.196f, 0.231f, 0.4f, 1.0f);

    [Header("Covers Buster Buttons")]
    public GameObject coverL;
    public GameObject coverR;

    private void Awake()
    {
        tempBonuses = new int[bonusCount];
        ordersCount = new int[bonusCount];
        
        ZeroBonus();

        //time for busters and life
        bustersTime.Add(30); //life sec
        bustersTime.Add(600); //color sec 60x?
        bustersTime.Add(500); //line sec 60x?
    }

    void OnEnable()
    {
        // Start the coroutine to update once per second
        updateCoroutine = StartCoroutine(UpdatePerSec());
    }


    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();        
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();        
        timeManagerClass = GameObject.FindWithTag("TimeManager").GetComponent<TimeManager>();

        SetupShop();

        //set type
        shopType = ShopType.Closed;

        //add default
        defInfoText.Add("Busters allow you to collect lost items faster");
        defInfoText.Add("Energy is needed to collect lost items");
        defInfoText.Add("Get the required number of moves to continue the game");

        //run life timer
        int livesCount = gameDataClass.saveData.bonuses[5];
        bool fundsForBooster5 = gameDataClass.saveData.credits < gameDataClass.saveData.bonusesPrice[5];
        if (livesCount == 0 && fundsForBooster5)
        {
            timeManagerClass.CreateTimer("lifeRecovery", bustersTime[0], TimerStart, TimerEnd);            
        }
    }

    private void TimerStart()
    {
        //here
    }

    private void TimerEnd()
    {
        //for lives
        string lifeTime = timeManagerClass.GetRemainingTime("lifeRecovery");
        if(lifeTime != "")
        {
            int livesCount = gameDataClass.saveData.bonuses[5];
            bool fundsForBooster5 = gameDataClass.saveData.credits < gameDataClass.saveData.bonusesPrice[5];
            if (livesCount == 0 && fundsForBooster5)
            {
                BusterUpdate(5);
            }
        }

        string colorTime = timeManagerClass.GetRemainingTime("colorBuster");
        if(colorTime == "")
        {
            BusterUpdate(5);
        }


        string lineTime = timeManagerClass.GetRemainingTime("lineBuster");
        if(lineTime == "")
        {
            BusterUpdate(11);
        }
     
    }


    public void UseTimeBuster(string busterName)
    {
        int colorBusterCount = gameDataClass.saveData.bonuses[1];
        if (busterName == "colorBuster" && colorBusterCount > 0)
        {
            timeManagerClass.CreateTimer("colorBuster", bustersTime[1], TimerStart, TimerEnd);
        }

        int lineBusterCount = gameDataClass.saveData.bonuses[11];
        if (busterName == "lineBuster" && lineBusterCount > 0)
        {
            timeManagerClass.CreateTimer("lineBuster", bustersTime[2], TimerStart, TimerEnd);
        }

    }

 
    public void SetupShop()
    {
        //get data from save
        creditsCount = gameDataClass.saveData.credits;
        
        //temp credits
        tempCreditsCount = creditsCount;

        //show data
        for(int i = 0; i < creditsCountText.Length; i++)
        {
            if (creditsCountText[i] != null)
                creditsCountText[i].text = "" + creditsCount;
        }       

        //main panel
        creditsCountPanelText.text = "" + creditsCount;


        for (int i = 0; i < creditsCountText.Length; i++)
        {
            if (creditsCountText[i] != null)
                creditsCountText[i].text = "" + tempCreditsCount;
        }

        if (creditsCountSlider != null)
        {
            creditsCountSlider.maxValue = tempCreditsCount;
            creditsCountSlider.minValue = 0;
            creditsCountSlider.value = tempCreditsCount;
        }

    }

    public void ZeroBonus()
    {
        for (int i = 0; i < tempBonuses.Length; i++)
        {
            tempBonuses[i] = 0;
        }

        for (int i = 0; i < ordersCount.Length; i++)
        {
            ordersCount[i] = 0;
        }

        bonusSelected = -1;
    }

    public void BuyBonus()
    {
        if (buySound != null)
            soundManagerClass.PlaySound(buySound);

        for (int i = 0; i < tempBonuses.Length; i++)
        {             
            gameDataClass.saveData.bonuses[i] = gameDataClass.saveData.bonuses[i] + tempBonuses[i];
        }

        //set moves
        if(shopType == ShopType.Moves)
        {
            if (endGameManagerClass != null)
            {
                endGameManagerClass.BuyMoves();
            }                
        }

        //set credits
        gameDataClass.saveData.credits = tempCreditsCount;

        //update text on panel
        creditsCountPanelText.text = "" + gameDataClass.saveData.credits;
       
        ZeroBonus();        
    }

    public void BuyEffects()
    {
        if (buyParticles01 != null)
            buyParticles01.Play();
    }

    public void ShowInfo(int value, string type, string busterName = null)
    {
        switch (type)
        {
            case "NoFounds":
                infoText.text = $"Not enough credits to purchase! {busterName} costs {value} credits";
                break;
            case "MaxCount":
                infoText.text = $"Maximum quantity of {busterName} - {value} pcs";
                break;
            case "NoLives":
                infoText.text = "Need to refill energy!";
                break;
            case "BuyBonus":
                infoText.text = "The " + busterName + " was added to cart";
                break;
            case "ReturnBonus":
                infoText.text = "The " + busterName + " was removed from the cart";
                break;
            case "LifeWaiting":
                infoText.text = $"Wait until your energy is refilled";
                break;
            default:
                infoText.text = "";
                break;
        }


        // Stop the previous coroutine if it's running
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        // Start the new coroutine
        fadeOutCoroutine = StartCoroutine(FadeOutText());
    }

    IEnumerator FadeOutText()
    {
        Color originalColor = ColorPalette.Colors["DarkBlue"];
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            infoText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null; // Wait until the next frame
        }

        // After fading out, you can hide or disable the text if needed
        if (shopType == ShopType.Busters)
            infoText.text = defInfoText[0];

        if (shopType == ShopType.Lives)
            infoText.text = defInfoText[1];

        if (shopType == ShopType.Moves)
            infoText.text = defInfoText[2];
       
        infoText.color = originalColor;
    }

    void BuyButtonLogic()
    {
        int sum = ordersCount.Sum();

        if (sum == 0)
        {
            buyButton.interactable = false;
            buyButton.GetComponent<Animator>().enabled = false;
        }
        else
        {
            buyButton.interactable = true;
            buyButton.GetComponent<Animator>().enabled = true;
        }
    }

    public void UpdateCredits(int credits)
    {
        // Update the slider value
        //creditsCountSlider.value = credits;

        // Get the Image component of the fillRect
        if(creditsCountSlider != null)
        {
            Image fillImage = creditsCountSlider.fillRect.GetComponent<Image>();
           
            // Hide the fillRect image if credits are 0, otherwise show it
            if (fillImage != null)
            {
                fillImage.enabled = credits > 0;
            }
        }
            
    }

    public void LivesClick()
    {
        if (shopState == BonusShop.ShopState.Levels)
        {
            {
                Debug.Log("!Lives!");
            }
        }
    }

    public void CloseShop()
    {
        ZeroBonus();

        // Stop the previous coroutine if it's running
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        //close shop
        if (shopType == ShopType.Busters)
        {
            shopPanel[0].SetActive(false);
        }
        else
        {
            shopPanel[1].SetActive(false);
        }
            
        shopType = ShopType.Closed;
    }

    //converter
    public void IntToShopType(int type)
    {
        //0-busters, 1-lives, 2- moves
        ShopType shopType = (ShopType)type;
        OpenShop(shopType);
    }


    public void OpenShop(ShopType type)
    {       
        ZeroBonus();       
        SetupShop();
      
        //for buster shop
        if (type == ShopType.Busters)
        {
            shopPanel[0].SetActive(true);
            shopName[0].text = "BUSTERS";
            shopType = ShopType.Busters;
        }
        //for lives shop
        if (type == ShopType.Lives)
        {
            shopPanel[1].SetActive(true);
            shopName[1].text = "ENERGY";
            shopType = ShopType.Lives;
            livesCount.text = "" + gameDataClass.saveData.bonuses[5]; //lives in shop
        }

        //for muves shop
        if (type == ShopType.Moves)
        {
            shopPanel[1].SetActive(true);
            shopName[1].text = "KEEP PLAYING?";
            shopType = ShopType.Moves;
            livesCount.text = "" + gameDataClass.saveData.bonuses[5]; //lives in shop
        }

        //get info text
        GameObject infoTextObject = GameObject.FindGameObjectWithTag("ShopInfoText");
        infoText = infoTextObject.GetComponent<TMP_Text>();

        //set intro text
        if (infoText != null)
        {
            if (type == ShopType.Busters)
                infoText.text = defInfoText[0];

            if (type == ShopType.Lives)
                infoText.text = defInfoText[1];

            if (type == ShopType.Moves)
                infoText.text = defInfoText[2];

            infoText.color = defaultInfoTextColor;
        }

        CoverTimeButtons(type);


    }

    private void CoverTimeButtons(ShopType type)
    {
        GameBoard gameBoardClass = GameObject.FindWithTag("GameBoard")?.GetComponent<GameBoard>();

        if (gameBoardClass != null && type == ShopType.Busters && shopState == ShopState.Game)
        {
            Image coverImgR = coverR != null ? coverR.GetComponent<Image>() : null;
            Image coverImgL = coverL != null ? coverL.GetComponent<Image>() : null;

            if (coverImgR != null)            
                coverImgR.enabled = false;
            
            if (coverImgL != null)            
                coverImgL.enabled = false;            

            foreach (GameObject button in GameObject.FindGameObjectsWithTag("BonusButton"))
            {
                BonusButton bBtn = button.GetComponent<BonusButton>();

                if (bBtn?.busterType == BonusButton.BusterType.Time && gameBoardClass.currentState != GameState.win)
                {
                    if (coverImgR != null)
                        coverImgR.enabled = true;

                    if (coverImgL != null)
                        coverImgL.enabled = true;
                }
            }
        }
    }

    void OnDisable()
    {
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        // Stop the coroutine when the object is disabled
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    private IEnumerator UpdatePerSec()
    {
        while (true)
        {
            if (buyButton != null)
            {
                BuyButtonLogic();
            }

            UpdateCredits(tempCreditsCount);

            if (timeManagerClass != null)
            {
                string lifeTime = timeManagerClass.GetRemainingTime("lifeRecovery");

                //icon in shop
                if (livesBusterShop != null)
                {
                    BonusButton livesBuster01 = livesBusterShop.GetComponent<BonusButton>();
                    
                    if (lifeTime != "")
                    {
                        conditionLife = true;
                        livesBuster01.clockIcon.SetActive(conditionLife);                        
                        livesBuster01.busterTimePanel.SetActive(conditionLife);
                        livesBuster01.busterTimeText.text = lifeTime;
                    }
                    else
                    {
                        conditionLife = false;
                        livesBuster01.clockIcon.SetActive(conditionLife);
                        livesBuster01.busterTimePanel.SetActive(conditionLife);
                        livesCount.text = "" + gameDataClass.saveData.bonuses[5];
                    }
                }


                //icon on panel
                if (livesBusterOnPanel != null)
                {
                    BonusButton livesBuster02 = livesBusterOnPanel.GetComponent<BonusButton>();

                    if (lifeTime != "")
                    {
                        conditionLife = true;
                        livesBuster02.clockIcon.SetActive(conditionLife);
                        livesBuster02.busterCountPanel.SetActive(false);
                        livesBuster02.busterTimePanel.SetActive(conditionLife);
                        livesBuster02.busterTimeText.text = lifeTime;
                    }
                    else
                    {
                        conditionLife = false;
                        livesBuster02.clockIcon.SetActive(conditionLife);
                        livesBuster02.busterCountPanel.SetActive(true);
                        livesBuster02.busterTimePanel.SetActive(conditionLife);
                    }                   
                }
            }

            yield return new WaitForSeconds(1f); // Wait for 1 second
        }
    }

    public void BusterUpdate(int busterNumber)
    {
        switch (busterNumber)
        {
            case 1:
                gameDataClass.saveData.bonuses[busterNumber] = 0;
                gameDataClass.saveData.colorBusterRecoveryTime = "";
                gameDataClass.SaveToFile();
                break;
            case 11:
                gameDataClass.saveData.bonuses[11] = 0;
                gameDataClass.saveData.lineBusterRecoveryTime = "";
                gameDataClass.SaveToFile();
                break;
            case 5:
                gameDataClass.saveData.bonuses[busterNumber] = 1;
                gameDataClass.saveData.lifeRecoveryTime = "";
                gameDataClass.SaveToFile();
                break;
            default:
                break;
        }
    }
}
