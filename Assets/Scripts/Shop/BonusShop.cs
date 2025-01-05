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

    public TMP_Text creditsCountShopText; //credits in shop

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
    int bonusCount = 11;

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
    public bool colorBusterInUse;
    private List<float> busterTime = new List<float>();

    private List<string> defInfoText = new List<string>();
    
    private Color defaultInfoTextColor = new Color(0.196f, 0.231f, 0.4f, 1.0f);

    private void Awake()
    {
        tempBonuses = new int[bonusCount];
        ordersCount = new int[bonusCount];
        
        ZeroBonus();

        //time for busters
        busterTime.Add(0.2f);
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
        defInfoText.Add("Displays items that are available in the inventory, and purchased items");
        defInfoText.Add("Energy is needed to collect lost items");
        defInfoText.Add("Get the required number of moves to continue the game");
    }

    private void RunTimlessBuster()
    {        
        string time = gameDataClass.saveData.colorBusterRecoveryTime;
        colorBusterInUse = false;

        if (!string.IsNullOrEmpty(time))
        {
            GetBusterTime(time, 1);
        }

        if (string.IsNullOrEmpty(time))
        {
            StartBusterTimer(1);            
        }
    }

    public string GetBusterTime(string time, int buster)
    {

        int busterCount = gameDataClass.saveData.bonuses[buster];

        DateTime recoveryEndTime;

        if (DateTime.TryParse(time, out recoveryEndTime))
        {
            TimeSpan remainingTime = recoveryEndTime - DateTime.Now;

            for(int i = 0; i<busterTime.Count; i++)
            {
                Debug.Log(busterTime[i]);
            }

            if (remainingTime.TotalMinutes > 0 && remainingTime.TotalMinutes < busterTime[0] && busterCount == 1)
            {
                colorBusterInUse = true;
                string timeLeft = $"{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}"; //{remainingTime.Seconds:D2}               
                Debug.Log($"Buster in use. Time left: {timeLeft}");
                return timeLeft;                
            }
            else
            {
                colorBusterInUse = false;
                // Reset the colorBuster and recovery time after 30 minutes
                gameDataClass.saveData.bonuses[buster] = 0;
                gameDataClass.saveData.colorBusterRecoveryTime = "";                
                gameDataClass.SaveToFile();
                Debug.Log("Buster timer finished. Bonus reset.");

                return "00:00"; // Return "00:00" if no valid time is available
            }
        }

        return "00:00"; // Return "00:00" if no valid time is available
    }


    private void StartBusterTimer(int buster)
    {
        if (gameDataClass.saveData.bonuses[buster] == 1)
        {
            // Set the recovery time to 30 minutes from now
            colorBusterInUse = true;
            DateTime recoveryEndTime = DateTime.Now.AddMinutes(busterTime[0]);
            gameDataClass.saveData.colorBusterRecoveryTime = recoveryEndTime.ToString();
            gameDataClass.SaveToFile();

            Debug.Log("Buster timer started");
        }
        else
        {
            Debug.Log("No buster");
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

        //in shop
        if (creditsCountShopText != null)
        {
            creditsCountShopText.text = "" + tempCreditsCount;

            if(creditsCountSlider != null)
            {
                creditsCountSlider.maxValue = tempCreditsCount;
                creditsCountSlider.minValue = 0;
                creditsCountSlider.value = tempCreditsCount;
            }
        }

        if (timeManagerClass != null)
        {
            string timeLeft = timeManagerClass.CheckFreeLifeConditions();
            Debug.Log(timeLeft);
        }

   /*     if (timeManagerClass != null && clockIconPanel != null)
        {
            timeLeft = timeManagerClass.CheckConditions();
            
            if(timeLeft != 0)
            {
                clockIconPanel.SetActive(true);
            }
            else
            {
                clockIconPanel.SetActive(false);
            }                
        }*/

        RunTimlessBuster();
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

        //lives
        //gameDataClass.saveData.lives += tempBonuses[5]; 

        //set credits
        gameDataClass.saveData.credits = tempCreditsCount;

        //update text on panel
        creditsCountPanelText.text = "" + gameDataClass.saveData.credits;
       
        ZeroBonus();

        //timless bosters
        if (gameDataClass.saveData.bonuses[1] > 0)
            RunTimlessBuster();
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
        Color originalColor = infoText.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            infoText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null; // Wait until the next frame
        }

        // After fading out, you can hide or disable the text if needed
        infoText.text = defInfoText[1];
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

    // Update is called once per frame
    void Update()
    {
/*        if (buyButton != null)
        {
            BuyButtonLogic();
        }

        UpdateCredits(tempCreditsCount);*/
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

        //if timer started
        if (timeManagerClass.timeState == TimeManager.TimeState.Waiting)
        {
            ShowInfo(0, "LifeWaiting");
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
                //get time
                string timer = timeManagerClass.CheckFreeLifeConditions();

                if (livesBusterShop != null)
                {

                    BonusButton livesBuster01 = livesBusterShop.GetComponent<BonusButton>();

                    if (timeManagerClass.timeState == TimeManager.TimeState.Waiting)
                    {
                        conditionLife = true;
                        livesBuster01.clockIcon.SetActive(conditionLife);
                        //livesBuster.busterCountPanel.SetActive(false);
                        livesBuster01.busterTimePanel.SetActive(conditionLife);
                        livesBuster01.busterTimeText.text = timer;
                    }
                    else
                    {
                        conditionLife = false;
                        livesBuster01.clockIcon.SetActive(conditionLife);
                        //livesBuster.busterCountPanel.SetActive(true);
                        livesBuster01.busterTimePanel.SetActive(conditionLife);
                        livesCount.text = "" + gameDataClass.saveData.bonuses[5];
                    }
                }


                //icon on panel
                if (livesBusterOnPanel != null)
                {
                    BonusButton livesBuster02 = livesBusterOnPanel.GetComponent<BonusButton>();

                    if (timeManagerClass.timeState == TimeManager.TimeState.Waiting)
                    {
                        conditionLife = true;
                        livesBuster02.clockIcon.SetActive(conditionLife);
                        livesBuster02.busterCountPanel.SetActive(false);
                        livesBuster02.busterTimePanel.SetActive(conditionLife);
                        livesBuster02.busterTimeText.text = timer;
                    }
                    else
                    {
                        livesBuster02.clockIcon.SetActive(conditionLife);
                        livesBuster02.busterCountPanel.SetActive(true);
                        livesBuster02.busterTimePanel.SetActive(conditionLife);
                    }
                }

            }

            yield return new WaitForSeconds(1f); // Wait for 1 second
        }
    }
}
