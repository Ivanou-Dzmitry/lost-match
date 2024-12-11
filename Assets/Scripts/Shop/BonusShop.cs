using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class BonusShop : MonoBehaviour
{
    public enum ShopState
    {
        Levels,
        Game,
        SetBonus,
        SetLastBonus
    }

    public ShopState shopState;

    //classes
    private GameData gameDataClass;
    private TimeManager timeManagerClass;
    private SoundManager soundManagerClass;

    //temp data
    public int[] tempBonuses;
    public int tempCreditsCount;

    private int creditsCount;
    //private int livesCount;
    public int[] ordersCount;

    [Header("Shop Panel")]
    public GameObject bonusShopPanel;
    public GameObject panelWithBonuses;
    public TMP_Text panelNameTxt;

    [Header("Sound")]
    public AudioClip buySound;

    [Header("Panel Shop")]
    public GameObject  panelWithShop;
    public Vector2 constantSizePWS = new Vector2(768, 1700);

    [Header("Panel Controls")]
    public GameObject panelWithControls;
    public Vector2 constantSizePWC = new Vector2(768, 1634);

    [Header("Credits")]
    public TMP_Text creditsCountPanelText;
    public TMP_Text creditsCountShopText;
    public Slider creditsCountSlider; //slider
    public Button buyButton;

    [Header("Notification Stuff")]
    public TMP_Text infoText;    //The store displays boosters that are available in the equipment
    public float fadeDuration = 2.0f; // Duration of the fade
    private Coroutine fadeOutCoroutine;

    int bonusCount = 6;

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



    private string defaultInfoText = "Shop displays items that are available in the inventory, and purchased items";

    private void Awake()
    {
        tempBonuses = new int[bonusCount];
        ordersCount = new int[bonusCount];
        ZeroBonus();        
    }


        // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        timeManagerClass = GameObject.FindWithTag("GameData").GetComponent<TimeManager>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

        SetupShop();

        //Debug.Log("here");

        if(infoText != null)
            infoText.text = defaultInfoText;
    }

    public void SetupShop()
    {
        //get data from save
        creditsCount = gameDataClass.saveData.credits;
        //gameDataClass.saveData.bonuses[5] = gameDataClass.saveData.lives; //

        //add prices
        //gameDataClass.saveData.bonusesPrice = bonusPrice;

        //temp credits
        tempCreditsCount = creditsCount;

        //show data
        if(creditsCountPanelText != null)
        {
            creditsCountPanelText.text = "" + creditsCount;
        }

        //in shop
        if (creditsCountShopText != null)
        {
            creditsCountShopText.text = "" + tempCreditsCount;

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

    public void TurnOnPanels()
    {
        if (panelWithBonuses != null)
        {
            foreach (Transform child in panelWithBonuses.transform)
            {
                // Check if the child has the "PanelWithBonus" tag
                if (child.CompareTag("PanelWithBonus"))
                {
                    // Enable the panel
                    child.gameObject.SetActive(true);
                }
            }
        }

        //restore size
        RectTransform panelShopRect = panelWithShop.GetComponent<RectTransform>();
        panelShopRect.sizeDelta = constantSizePWS;

        //restore size
        RectTransform panelControlRect = panelWithControls.GetComponent<RectTransform>();
        panelControlRect.sizeDelta = constantSizePWC;
    }


    public void OpenShop(string shopName)
    {       
        ZeroBonus();

        SetupShop();

        panelNameTxt.text = shopName;
    }

    public void BuyBonus()
    {
        if (buySound != null)
            soundManagerClass.PlaySound(buySound);

        for (int i = 0; i < tempBonuses.Length; i++)
        {             
            gameDataClass.saveData.bonuses[i] = gameDataClass.saveData.bonuses[i] + tempBonuses[i];
        }

        //lives
        //gameDataClass.saveData.lives += tempBonuses[5]; 

        //set credits
        gameDataClass.saveData.credits = tempCreditsCount;

        //update text on panel
        creditsCountPanelText.text = "" + gameDataClass.saveData.credits;
        //livesCountPanelText.text = "" + gameDataClass.saveData.lives;
       
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
                infoText.text = $"Maximum quantity of {busterName} purchased: {value} pcs";
                break;
            case "NoLives":
                infoText.text = "Need to buy batteries!";
                break;
            case "BuyBonus":
                infoText.text = "The " + busterName + " was added to cart";
                break;
            case "ReturnBonus":
                infoText.text = "The " + busterName + " was removed from the cart";
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
        infoText.text = defaultInfoText;
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
        if (buyButton != null)
        {
            BuyButtonLogic();
        }

        UpdateCredits(tempCreditsCount);
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


    public void DisableSpecificChildren(string type)
    {
        if (panelWithBonuses != null)
        {
            // Find all GameObjects with the "PanelWithBonus" tag
            GameObject[] panels = GameObject.FindGameObjectsWithTag("PanelWithBonus");

            for (int i =0; i < panels.Length; i++)
            {
                //for buster shop
                if (type=="Buster")
                {
                    if(panels[i].name == "Lives")
                    {
                        panels[i].gameObject.SetActive(false);
                    }
                        
                }
                //for lives shop
                if (type == "Lives")
                {
                    if(panels[i].name != "Lives")
                    {
                        panels[i].gameObject.SetActive(false);
                    }
                        
                }

                //for muves shop
                if (type == "Moves")
                {
                    if (panels[i].name != "Moves")
                    {
                        panels[i].gameObject.SetActive(false);
                    }
                }
            }

            float totalVisibleHeight = 0f;


            //get decrease value
            if (panelWithBonuses != null)
            {
                foreach (Transform child in panelWithBonuses.transform)
                {
                    // Check if the child has the "PanelWithBonus" tag
                    if (child.CompareTag("PanelWithBonus"))
                    {
                        if (!child.gameObject.activeSelf)
                        {
                            RectTransform panelRect = child.gameObject.GetComponent<RectTransform>();
                            totalVisibleHeight += panelRect.rect.height;
                        }
                    }
                }
            }

            //height
            // Adjust the ShopPanel height
            RectTransform shopPanelRect = panelWithShop.GetComponent<RectTransform>();
            float newHeight = shopPanelRect.rect.height - totalVisibleHeight;
            shopPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(newHeight, 0f));

            RectTransform shopControlsRect = panelWithControls.GetComponent<RectTransform>();
            float newHeightControls = shopControlsRect.rect.height - totalVisibleHeight;
            shopControlsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(newHeightControls, 0f));

        }
    }
}
