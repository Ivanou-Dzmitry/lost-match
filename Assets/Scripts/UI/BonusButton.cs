using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using static BonusShop;


public class BonusButton : MonoBehaviour
{
    public enum BusterType
    {
        Indefinite,
        Timless,
        Time,
        Bundle
    }

    public enum BusterPlace
    {
        Indefinite,
        ShopBefore,
        ConfirmPanel,
        InGame
    }

    public BusterType busterType;
    public BusterPlace busterPlace;

    //classes
    private GameData gameDataClass;
    private BonusShop bonusShopClass;
    private SoundManager soundManagerClass;

    [Header("UI Options")]
    public bool colorizeUI = true;
    public bool interactibleUI = true;
    public bool maxSignUI = true;

    //number
    [Header("Busters")]
    public int bonusNumber;
    public string busterName;

    [Header("Bundle")]
    public int bundleRootBonus; //what bonus is root
    public int bundleCount;
    public bool isBundle;    

    private bool updInfo;

    [Header("Buttons UI")]
    //buttons
    public GameObject bonusButtonShop;

    public int bonusCount;
    private int tempBonusCount;

    private int bonusPrice;
    
    [Header("Count")]
    public TMP_Text bonusCountText;
    public GameObject busterCountPanel;
    public GameObject countPanel;

    [Header("Price")]
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

    [Header("Time")]
    public GameObject clockIcon;
    public GameObject busterTimePanel;
    public TMP_Text busterTimeText;

    [Header("ADD")]
    public GameObject addBusterPanel;
    public Button addBusterButton;
    public GameObject useBusterPanel;

    public static readonly Color beforeBuyColor = new Color(1.0f, 0.929f, 0.808f, 1.0f); //default
    public static readonly Color afterBuyColor = new Color(0.729f, 0.902f, 0.8f, 1f); //when buy

    private float timer = 1f; // Tracks time


    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

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

        //clock icon
        if (busterType == BusterType.Time)
        {
            //for shop
            if (busterPlace == BusterPlace.ShopBefore)
                clockIcon.SetActive(true);
            
            if(busterCountPanel != null)
                busterCountPanel.SetActive(false);

            //for confirm
            if(busterPlace == BusterPlace.ConfirmPanel && this.bonusCount > 0)
            {              
                useBusterPanel.SetActive(true);
                addBusterPanel.SetActive(false);
            }
        }

        //hide for bundle
        if(busterType == BusterType.Bundle)
        {
            countPanel.SetActive(false);
        }
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
           
            //set color for button
            if(colorizeUI)
                this.GetComponent<Image>().color = beforeBuyColor;

            //max sign
            if (this.maxSign != null)
                this.maxSign.enabled = false;

            if (gameDataClass.saveData.bonuses[bonusNumber] == 0 && interactibleUI)
                this.bonusButtonShop.GetComponent<Button>().interactable = false;

            //counter
            if(this.counterPopUpText!= null)
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

            for (int i = 0; i < bonusShopClass.creditsCountText.Length; i++)
            {
                if (bonusShopClass.creditsCountText[i] != null)
                    bonusShopClass.creditsCountText[i].text = "" + bonusShopClass.tempCreditsCount;
            }


            //bonusShopClass.creditsCountShopText.text = "" + bonusShopClass.tempCreditsCount;
            bonusShopClass.creditsCountSlider.value = bonusShopClass.tempCreditsCount;
        }

        // for Game
        if (bonusShopClass.shopState == BonusShop.ShopState.Game)
        {
            for (int i = 0; i < bonusShopClass.creditsCountText.Length; i++)
            {
                if (bonusShopClass.creditsCountText[i] != null)
                    bonusShopClass.creditsCountText[i].text = "" + bonusShopClass.tempCreditsCount;
            }

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
        if (this.maxSign != null && maxSignUI)
        {
            if(this.busterType != BusterType.Time)
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

    //add only root bonus
    public void AddBundle(GameObject topObject)
    {
        BonusButton thisBonusButton = topObject.GetComponent<BonusButton>();

        //data bonus
        int bonus = thisBonusButton.bonusNumber;

        //bonus for update with bundle
        int rootBonus = thisBonusButton.bundleRootBonus;

        int credits = bonusShopClass.tempCreditsCount;
        int maxCount = gameDataClass.saveData.maxBonusCount[rootBonus]; //root        
        int currentCount = gameDataClass.saveData.bonuses[rootBonus]; //root
        int bonusPrice = gameDataClass.saveData.bonusesPrice[bonus];

        //check max
        int bonusCount = currentCount + thisBonusButton.bundleCount; //root

        int debt = 0;
        bool operation_permissible = false;

        debt = credits - thisBonusButton.bonusPrice;

        //if credits enought
        if (debt >= 0 && bonusCount <= maxCount)
            operation_permissible = true;

        //Debug.Log(this.bonusPrice);

        if (operation_permissible)
        {
            bonusShopClass.tempBonuses[rootBonus] += thisBonusButton.bundleCount; //add Root bonus
            bonusShopClass.tempCreditsCount = credits - thisBonusButton.bonusPrice; //minus price

           bonusShopClass.BuyBonus();
           bonusShopClass.CloseShop();
        }
        else
        {
            if (operation_permissible == false)
                bonusShopClass.ShowInfo(bonusPrice, "NoFounds", thisBonusButton.busterName);

            //if max count
            if (bonusCount >= maxCount)
            {
                bonusShopClass.ShowInfo(maxCount, "MaxCount", thisBonusButton.busterName);
                if(this.busterType != BusterType.Time)
                    this.maxSign.enabled = true;
            }
        }
    }

    public void AddBonus(int bonus)
    {
        //int bonusPrice = gameDataClass.saveData.bonusesPrice[bonus];
        int credits = bonusShopClass.tempCreditsCount;
        int maxCount = gameDataClass.saveData.maxBonusCount[bonus];
        int currentCount = gameDataClass.saveData.bonuses[bonus];

        //check max
        int bonusCount = currentCount + bonusShopClass.tempBonuses[bonus];

        int debt = 0;
        bool operation_permissible = false;

         debt = credits - this.bonusPrice;
           
        //if credits enought
        if (debt >= 0 && bonusCount < maxCount)
            operation_permissible = true;

        if (operation_permissible)
        {           
            bonusShopClass.tempBonuses[bonus] += 1; //add 1 bonus
            bonusShopClass.tempCreditsCount = credits - this.bonusPrice; //minus price

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
                if (this.busterType != BusterType.Time)
                    this.maxSign.enabled = true;
            }
        }
        else
        {
            if (operation_permissible == false)
                bonusShopClass.ShowInfo(this.bonusPrice, "NoFounds", this.busterName);

            //if max count
            if (bonusCount >= maxCount)
            {
                bonusShopClass.ShowInfo(bonusCount, "MaxCount", this.busterName);
                if (this.busterType != BusterType.Time)
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

            //Minus logic
            bonusShopClass.tempBonuses[bonus] -= 1;                

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
            this.GetComponent<Image>().color = afterBuyColor;
            this.minusButton.interactable = true;
            this.minusButton.GetComponent<Image>().color = new Color(1, 1, 1, 1);
        }
        else
        {
            this.GetComponent<Image>().color = beforeBuyColor;
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

    public void OpenShop()
    {
        bonusShopClass.IntToShopType(0);
    }

    public void UseBuster(string busterName)
    {
        if (busterName == "colorBuster")
        {
            bonusShopClass.UseTimeBuster("colorBuster");
        }


        if (busterName == "lineBuster")
        {
            bonusShopClass.UseTimeBuster("lineBuster");
        }

        if (busterName == "null")
        {
            Debug.Log("Buster is not assigned");
        }

    }

}
