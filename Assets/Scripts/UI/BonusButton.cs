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

    //number
    public int bonusNumber;

    //buttons
    public GameObject bonusButtonShop;
    public GameObject bonusButtonScreen;

    
    private int bonusCount;
    private int tempBonusCount;

    public TMP_Text bonusCountText;


    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();

        if (bonusShopClass.infoText != null )
            bonusShopClass.infoText.text = "";

        UpdateBonusCount();
    }


    void OnEnable()
    {
        if (gameDataClass != null && bonusShopClass!= null)
        {
            UpdateBonusCount();
            bonusShopClass.infoText.text = "";
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
        
        int debt = 0;
        bool operation_permissible = false;

        debt = credits - bonusPrice;

        if (debt > 0)
            operation_permissible = true;

        if (operation_permissible)
        {
            bonusShopClass.tempCreditsCount = credits - bonusPrice;
            bonusShopClass.tempBonuses[bonus] += 1;
            bonusShopClass.infoText.text = "";

            
        }
        else
        {
            bonusShopClass.ShowInfo(bonusPrice);
        }

        //Debug.Log("Add");
    }

    public void RemoveBonus(int bonus)
    {
        if (bonusShopClass.tempBonuses[bonus] > 0)
        {
            bonusShopClass.tempCreditsCount = bonusShopClass.tempCreditsCount + gameDataClass.saveData.bonusesPrice[bonus];
            bonusShopClass.tempBonuses[bonus] -= 1;
            bonusShopClass.infoText.text = "";
        }

        //Debug.Log("Remove");
    }





    // Update is called once per frame
    void Update()
    {
        if (bonusShopClass.shopState == BonusShop.ShopState.Levels)
        {
            bonusShopClass.creditsCountShopText.text = "" + bonusShopClass.tempCreditsCount;
            bonusShopClass.creditsCountSlider.value = bonusShopClass.tempCreditsCount;
        }
    }
}
