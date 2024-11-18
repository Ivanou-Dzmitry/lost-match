using System.Collections;
using System.Collections.Generic;
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
    public int bonusNumber;

    //buttons
    public GameObject bonusButtonShop;
    public GameObject bonusButtonScreen;

    private int bonusCount;
    private int tempBonusCount;

    private int bonusPrice;

    public TMP_Text bonusCountText;
    public TMP_Text bonusPriceText;

    public Button plusButton;
    public Button minusButton;

    [Header("Sound")]
    public AudioClip buttonClick;

    public static readonly Color SoftPinkClr = new Color(0.902f, 0.729f, 0.859f, 1f);
    public static readonly Color LightGreenClr = new Color(0.729f, 0.902f, 0.8f, 1f);

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

        this.bonusPrice = gameDataClass.saveData.bonusesPrice[bonusNumber];
        this.bonusPriceText.text = "" + this.bonusPrice;
    }


    void OnEnable()
    {
        if (gameDataClass != null && bonusShopClass!= null)
        {
            UpdateBonusCount();
            bonusShopClass.infoText.text = "";

            this.GetComponent<Image>().color = SoftPinkClr;

            //disable minus button
            this.minusButton.interactable = false;
        }
    }


    public void UpdateBonusCount()
    {
        this.bonusCount = gameDataClass.saveData.bonuses[bonusNumber];

        //for shopping
        this.tempBonusCount = bonusShopClass.tempBonuses[bonusNumber];
        
        //show in text
        this.bonusCountText.text = "" + (this.bonusCount + this.tempBonusCount);

        //for shop
        if (this.bonusCount == 0 && bonusButtonShop != null)
        {
            bonusButtonShop.GetComponent<Button>().interactable = false;
            bonusCountText.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }
        else if(bonusButtonShop != null)
        {
            bonusButtonShop.GetComponent<Button>().interactable = true;
            bonusCountText.color = new Color(0f, 0f, 0f, 1f);
        }

        //for panel
        if (this.bonusCount == 0 && bonusButtonScreen != null)
        {
            bonusButtonScreen.GetComponent<Button>().interactable = false;
            bonusCountText.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }
        else if (bonusButtonScreen != null)
        {
            bonusButtonScreen.GetComponent<Button>().interactable = true;
            bonusCountText.color = new Color(0f, 0f, 0f, 1f);
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
        }
        else
        {
            if (debt >= 0)
                bonusShopClass.ShowInfo(bonusPrice, "NoFounds");
            
            if (bonusCount >= maxCount)
                bonusShopClass.ShowInfo(bonusCount, "MaxCount");
        }

        //sfx for buy
        soundManagerClass.PlaySound(buttonClick);


    }

    public void RemoveBonus(int bonus)
    {
        if (bonusShopClass.tempBonuses[bonus] > 0)
        {
            bonusShopClass.tempCreditsCount = bonusShopClass.tempCreditsCount + gameDataClass.saveData.bonusesPrice[bonus];
            bonusShopClass.tempBonuses[bonus] -= 1;
            bonusShopClass.infoText.text = "";           
        }

        //orders
        if (bonusShopClass.ordersCount[bonus] > 0)
        {
            bonusShopClass.ordersCount[bonus] -= 1;
        }

        ButtonColor(bonus);

        //sfx for buy
        soundManagerClass.PlaySound(buttonClick);
    }

    private void ButtonColor(int bonus)
    {
        if (bonusShopClass.ordersCount[bonus] > 0)
        {
            this.GetComponent<Image>().color = LightGreenClr;
            this.minusButton.interactable = true;
        }
        else
        {
            this.GetComponent<Image>().color = SoftPinkClr;
            this.minusButton.interactable = false;
        }                               
    }


    // Update is called once per frame
    void Update()
    {
        if (bonusShopClass.shopState == BonusShop.ShopState.Levels)
        {
            bonusShopClass.creditsCountShopText.text = "" + bonusShopClass.tempCreditsCount;
            bonusShopClass.creditsCountSlider.value = bonusShopClass.tempCreditsCount;
        }

        UpdateBonusCount();
    }
}
