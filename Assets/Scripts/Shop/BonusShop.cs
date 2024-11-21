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
        SetBonus
    }

    public ShopState shopState;

    //classes
    private GameData gameDataClass;

    //temp data
    public int[] tempBonuses;
    public int tempCreditsCount;

    private int creditsCount;
    //private int livesCount;
    public int[] ordersCount;

    

    public TMP_Text creditsCountPanelText;
    public TMP_Text creditsCountShopText;
    public Slider creditsCountSlider; //slider
    public Button buyButton;

    public TMP_Text infoText;    
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

    private void Awake()
    {
        tempBonuses = new int[bonusCount];
        ZeroBonus();
    }


        // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        SetupShop();

        //Debug.Log("here");
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


    public void OpenShop()
    {       
        ZeroBonus();

        SetupShop();
    }

    public void BuyBonus()
    {
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
        buyParticles01.Play();
    }

    public void ShowInfo(int value, string type)
    {
        switch (type)
        {
            case "NoFounds":
                infoText.text = "Not enough credits to purchase! This bonus costs " + value + " credits";
                break;
            case "MaxCount":
                infoText.text = "Maximum quantity purchased: " + value + " pcs.";
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
        //textToFade.gameObject.SetActive(false);
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
}
