using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class BonusButton : MonoBehaviour
{
    //classes
    private GameData gameDataClass;
    private BonusShop bonusShopClass;
    private SoundManager soundManagerClass;

    //number
    public int bonusNumber;

    private bool updInfo;

    //buttons
    public GameObject bonusButtonShop;
    
    private int bonusCount;
    private int tempBonusCount;

    private int bonusPrice;

    public TMP_Text bonusCountText;
    public TMP_Text bonusPriceText;

    //popup
    public TMP_Text counterPopUpText;
    private static readonly float fadeDuration = 1.0f; // Duration of the fade

    public Button plusButton;
    public Button minusButton;

    [Header("Sound")]
    public AudioClip buttonClick;
    public AudioClip buySound01;


    public static readonly Color SoftPinkClr = new Color(0.902f, 0.729f, 0.859f, 1f);
    public static readonly Color LightGreenClr = new Color(0.729f, 0.902f, 0.8f, 1f);

    private float timer = 1f; // Tracks time

    public Image maxSign;

    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

        if (bonusShopClass.infoText != null )
            bonusShopClass.infoText.text = "";

        UpdateBonusCount();

        //set bonuce price
        if (gameDataClass.saveData.bonusesPrice != null)
            this.bonusPrice = gameDataClass.saveData.bonusesPrice[bonusNumber];
        
        //set bonuce price text
        if (this.bonusPriceText != null)
            this.bonusPriceText.text = "" + this.bonusPrice;

        if(this.counterPopUpText != null)
            this.counterPopUpText.text = string.Empty;

        updInfo = true;
    }

    private void Awake()
    {
        //disable minus button
        if (this.minusButton != null)
        {
            this.minusButton.interactable = false;
            this.minusButton.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        }
    }


    void OnEnable()
    {
        if (gameDataClass != null && bonusShopClass!= null)
        {
            UpdateBonusCount();

            bonusShopClass.infoText.text = "";

            this.GetComponent<Image>().color = SoftPinkClr;



            //max sign
            this.maxSign.enabled = false;

            if (gameDataClass.saveData.bonuses[bonusNumber] == 0)
                this.bonusButtonShop.GetComponent<Button>().interactable = false;

            this.counterPopUpText.text = string.Empty;

            updInfo = true;
        }
    }


    public void UpdateBonusCount()
    {
        // Code to execute once per second
        if (bonusShopClass.shopState == BonusShop.ShopState.Levels)
        {
            bonusShopClass.creditsCountShopText.text = "" + bonusShopClass.tempCreditsCount;
            bonusShopClass.creditsCountSlider.value = bonusShopClass.tempCreditsCount;
        }

        this.bonusCount = gameDataClass.saveData.bonuses[bonusNumber];

        //for shopping
        this.tempBonusCount = bonusShopClass.tempBonuses[bonusNumber];

        int totalBonusCount = this.bonusCount + this.tempBonusCount;

        //show in text
        this.bonusCountText.text = "" + (totalBonusCount);

        //set max and current
        int maxCount = gameDataClass.saveData.maxBonusCount[bonusNumber];
        int currentCount = gameDataClass.saveData.bonuses[bonusNumber] + bonusShopClass.tempBonuses[bonusNumber];

        //turn on sign
        if (this.maxSign != null)
        {
            this.maxSign.enabled = (currentCount == maxCount);
        }

        //button enables
        if (totalBonusCount == 0)
        {
            this.bonusButtonShop.GetComponent<Button>().interactable = false;
        }
        else
        {
            if (bonusShopClass.shopState != BonusShop.ShopState.SetBonus)
                this.bonusButtonShop.GetComponent<Button>().interactable = true;
        }

    }

    public void AddBonus(int bonus)
    { 
        int bonusPrice = gameDataClass.saveData.bonusesPrice[bonus];
        int credits = bonusShopClass.tempCreditsCount;
        int maxCount = gameDataClass.saveData.maxBonusCount[bonus];
        int currentCount = gameDataClass.saveData.bonuses[bonus];

        //check max
        int bonusCount = currentCount + bonusShopClass.tempBonuses[bonus];

        int debt = 0;
        bool operation_permissible = false;

        debt = credits - bonusPrice;       

        //if credits enought
        if (debt >= 0 && bonusCount < maxCount)
            operation_permissible = true;

        if (operation_permissible)
        {            
            bonusShopClass.tempCreditsCount = credits - bonusPrice;
            bonusShopClass.tempBonuses[bonus] += 1;
            bonusShopClass.infoText.text = "";
            
            bonusShopClass.BuyEffects();
            //orders
            bonusShopClass.ordersCount[bonus] += 1;
            ButtonColor(bonus);

            this.counterPopUpText.text = "+1";
            StartCoroutine(FadeOutPopUp());

            //play sound
            soundManagerClass.PlaySound(buySound01);     
            
            //mac label
            if (bonusShopClass.tempBonuses[bonus] == maxCount)
            {
                this.maxSign.enabled = true;
            }
        }
        else
        {
            if (operation_permissible == false)
                bonusShopClass.ShowInfo(bonusPrice, "NoFounds");
            
            if (bonusCount >= maxCount)
            {
                bonusShopClass.ShowInfo(bonusCount, "MaxCount");
                this.maxSign.enabled = true;
            }
                
        }

        //sfx for buy
        soundManagerClass.PlaySound(buttonClick);

        updInfo = true;

    }

    public void RemoveBonus(int bonus)
    {
        if (bonusShopClass.tempBonuses[bonus] > 0)
        {
            bonusShopClass.tempCreditsCount = bonusShopClass.tempCreditsCount + gameDataClass.saveData.bonusesPrice[bonus];
            bonusShopClass.tempBonuses[bonus] -= 1;
            bonusShopClass.infoText.text = "";
            this.counterPopUpText.text = "-1";

            //max label
            this.maxSign.enabled = false;
        }
       
        StartCoroutine(FadeOutPopUp());

        //orders
        if (bonusShopClass.ordersCount[bonus] > 0)
        {
            bonusShopClass.ordersCount[bonus] -= 1;
        }

        ButtonColor(bonus);

        //sfx for buy
        soundManagerClass.PlaySound(buttonClick);

        updInfo = true;
    }

    private void ButtonColor(int bonus)
    {       
        if (bonusShopClass.ordersCount[bonus] > 0)
        {
            this.GetComponent<Image>().color = LightGreenClr;
            this.minusButton.interactable = true;
            this.minusButton.GetComponent<Image>().color = new Color(1, 1, 1, 1);
        }
        else
        {
            this.GetComponent<Image>().color = SoftPinkClr;
            this.minusButton.interactable = false;
            this.minusButton.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        }                               
    }


    // Update is called once per frame
    void Update()
    {
        // Increment the timer by the time since the last frame
        timer += Time.deltaTime;

        // Check if one second has passed
        if (timer >= 1f || updInfo == true)
        {
            // Reset the timer
            timer = 0f;

            UpdateBonusCount();

            updInfo = false;
        }

    }

    public void BonusButtonClick()
    {
        //Debug.Log(bonusShopClass.bonusSelected);

        if (bonusShopClass.shopState == BonusShop.ShopState.SetBonus)
        {
            int selectedBonus = this.bonusNumber;
            int bonusCount = gameDataClass.saveData.bonuses[selectedBonus];
            gameDataClass.saveData.bonuses[selectedBonus] = bonusCount + 1;

            bonusShopClass.bonusDescPanel.SetActive(false);

            updInfo = true;

            //Debug.Log("Return " + selectedBonus);

            bonusShopClass.shopState = BonusShop.ShopState.Game;
            bonusShopClass.bonusSelected = -1;
        }

            else if (bonusShopClass.shopState == BonusShop.ShopState.Game)
        {
            bonusShopClass.bonusSelected = -1;
            
            int selectedBonus = this.bonusNumber;

            //set selectd bonus
            bonusShopClass.bonusSelected = selectedBonus;

            int bonusCount = gameDataClass.saveData.bonuses[selectedBonus];

            gameDataClass.saveData.bonuses[selectedBonus] = bonusCount - 1;

            //Debug.Log("Click on Bonus: " + this.name + "/ " + this.bonusNumber +"/count: "+ bonusCount);
           
            updInfo = true;

            bonusShopClass.bonusDescPanel.SetActive(true);
            bonusShopClass.bonusImage.sprite = bonusShopClass.bonusPicture[selectedBonus];
            bonusShopClass.bonusName.text = bonusShopClass.bonusNameString[selectedBonus];
            bonusShopClass.bonusDescription.text = bonusShopClass.bonusDescString[selectedBonus];

            GameObject[] allBonusButtons = GameObject.FindGameObjectsWithTag("BonusButtonPrefabGame");

            string curName = this.name;

            for (int i = 0; i < allBonusButtons.Length; i++)
            {
                if(allBonusButtons[i].name != curName)
                {
                    BonusButton bb = allBonusButtons[i].gameObject.GetComponent<BonusButton>();
                    bb.bonusButtonShop.GetComponent<Button>().interactable = false;
                }
            }

            
            // set state
            bonusShopClass.shopState = BonusShop.ShopState.SetBonus;
        }
    }


    private void OnDestroy()
    {
        // Code to execute before the component is destroyed
        updInfo = true;
    }

    IEnumerator FadeOutPopUp()
    {
        Color originalColor = counterPopUpText.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            counterPopUpText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null; // Wait until the next frame
        }

        counterPopUpText.text = string.Empty;
    }
}
