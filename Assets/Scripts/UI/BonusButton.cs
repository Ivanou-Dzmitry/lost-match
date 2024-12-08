using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;


public class BonusButton : MonoBehaviour
{
    //classes
    private GameData gameDataClass;
    private BonusShop bonusShopClass;
    private SoundManager soundManagerClass;

    //number
    [Header("Busters")]
    public int bonusNumber;
    public string busterName;

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

    public Image maxSign;

    [Header("Sound")]
    public AudioClip buttonClick;
    public AudioClip returnClick;
    public AudioClip buySound01;

    [Header("Particles")]
    public ParticleSystem bonusPart01;
    public ParticleSystem bonusPart02;


    public static readonly Color SoftPinkClr = new Color(0.902f, 0.729f, 0.859f, 1f);
    public static readonly Color LightGreenClr = new Color(0.729f, 0.902f, 0.8f, 1f);

    private float timer = 1f; // Tracks time



    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

        if (bonusShopClass.infoText != null)
            bonusShopClass.infoText.text = "";

        UpdateBonusCount();

        //set bonuce price
        if (gameDataClass.saveData.bonusesPrice != null)
            this.bonusPrice = gameDataClass.saveData.bonusesPrice[bonusNumber];

        //set bonuce price text
        if (this.bonusPriceText != null)
            this.bonusPriceText.text = "" + this.bonusPrice;

        if (this.counterPopUpText != null)
            this.counterPopUpText.text = string.Empty;

        updInfo = true;
    }

    private void Awake()
    {
        //disable minus button
        HideMinusButton();
    }

    private void HideMinusButton()
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
        if (gameDataClass != null && bonusShopClass != null)
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

            HideMinusButton();
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

        // for Game
        if (bonusShopClass.shopState == BonusShop.ShopState.Game)
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

        //turn off particles
        if (bonusShopClass.bonusSelected == -1)
        {
            BonusParticleManager(false);
        }

        //turn on sign
        if (this.maxSign != null)
        {
            this.maxSign.enabled = (currentCount == maxCount);
        }

      
        //button enables
        if (totalBonusCount == 0)
        {
            //for last bonus
            if (bonusShopClass.shopState != BonusShop.ShopState.SetLastBonus)
                this.bonusButtonShop.GetComponent<Button>().interactable = false;
        }
        else if (totalBonusCount > 0)
        {
            if (bonusShopClass.shopState != BonusShop.ShopState.SetBonus)
                this.bonusButtonShop.GetComponent<Button>().interactable = true;

            //for last bonus
            if (bonusShopClass.shopState == BonusShop.ShopState.SetLastBonus)
                this.bonusButtonShop.GetComponent<Button>().interactable = false;
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

            if (bonusShopClass.infoText != null)
            {
                bonusShopClass.infoText.text = "";
            }
            
            bonusShopClass.BuyEffects();

            //orders
            bonusShopClass.ordersCount[bonus] += 1;
            ButtonColor(bonus);

            this.counterPopUpText.text = "+1";
            StartCoroutine(FadeOutPopUp());

            //play sound
            soundManagerClass.PlaySound(buySound01);

            bonusShopClass.ShowInfo(1, "BuyBonus", this.busterName);

            //mac label
            if (bonusShopClass.tempBonuses[bonus] == maxCount)
            {
                this.maxSign.enabled = true;
            }
        }
        else
        {
            if (operation_permissible == false)
                bonusShopClass.ShowInfo(bonusPrice, "NoFounds", this.busterName);

            if (bonusCount >= maxCount)
            {
                bonusShopClass.ShowInfo(bonusCount, "MaxCount", this.busterName);
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

            bonusShopClass.ShowInfo(1, "ReturnBonus", this.busterName);
        }

        StartCoroutine(FadeOutPopUp());

        //orders
        if (bonusShopClass.ordersCount[bonus] > 0)
        {
            bonusShopClass.ordersCount[bonus] -= 1;
        }

        ButtonColor(bonus);

        //sfx for buy
        if (returnClick!=null)
            soundManagerClass.PlaySound(returnClick);

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

    public void BonusParticleManager(bool mode)
    {
        if(this.bonusPart01 != null && this.bonusPart02 != null)
        {
            if (mode)
            {
                this.bonusPart01.Play();
                this.bonusPart02.Play();
            }
            else
            {
                this.bonusPart01.Stop();
                this.bonusPart02.Stop();
            }
        }

    }

    public void BonusButtonClick()
    {
        if (bonusShopClass.shopState == BonusShop.ShopState.SetBonus || bonusShopClass.shopState == BonusShop.ShopState.SetLastBonus)
        {
            int selectedBonus = this.bonusNumber;
            int bonusCount = gameDataClass.saveData.bonuses[selectedBonus];

            gameDataClass.saveData.bonuses[selectedBonus] = bonusCount + 1;

            ToggleBonusDescriptionPanel(false);

            updInfo = true;

            bonusShopClass.shopState = BonusShop.ShopState.Game;
            bonusShopClass.bonusSelected = -1;

            //turn off particles
            BonusParticleManager(false);
        }
        else if (bonusShopClass.shopState == BonusShop.ShopState.Game)
        {
            bonusShopClass.bonusSelected = -1;
            
            int selectedBonus = this.bonusNumber;

            //set selectd bonus
            bonusShopClass.bonusSelected = selectedBonus;

            int bonusCount = gameDataClass.saveData.bonuses[selectedBonus];

            gameDataClass.saveData.bonuses[selectedBonus] = bonusCount - 1;

            //set state
            if (gameDataClass.saveData.bonuses[selectedBonus] == 0)
            {
                bonusShopClass.shopState = BonusShop.ShopState.SetLastBonus;
            }
            else
            {
                bonusShopClass.shopState = BonusShop.ShopState.SetBonus;
            }
           
            updInfo = true;

            ToggleBonusDescriptionPanel(true);

            //show description
            SetBonusDescription(selectedBonus);

            //disable other buttons
            DisableOtherBonusButtons(this.name);

            //turn on particles
            BonusParticleManager(true);
        }
    }

    void SetBonusDescription(int selectedBonus)
    {
        bonusShopClass.bonusImage.sprite = bonusShopClass.bonusPicture[selectedBonus];
        bonusShopClass.bonusName.text = bonusShopClass.bonusNameString[selectedBonus];
        bonusShopClass.bonusDescription.text = bonusShopClass.bonusDescString[selectedBonus];
    }

    void DisableOtherBonusButtons(string currentButtonName)
    {
        GameObject[] allBonusButtons = GameObject.FindGameObjectsWithTag("BonusButtonPrefabGame");

        foreach (var buttonObject in allBonusButtons)
        {
            if (buttonObject.name != currentButtonName)
            {
                Button button = buttonObject.GetComponent<BonusButton>().bonusButtonShop.GetComponent<Button>();
                button.interactable = false;
            }
        }
    }

    void ToggleBonusDescriptionPanel(bool isActive)
    {
        bonusShopClass.bonusDescPanel.SetActive(isActive);
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
