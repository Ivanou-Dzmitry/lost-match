using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;



public class BonusShop : MonoBehaviour
{
    public enum ShopState
    {
        Levels,
        Game
    }

    public ShopState shopState;

    //classes
    private GameData gameDataClass;

    [Header("Bonus Price")]
    public int[] bonusPrice;

    //temp data
    public int[] tempBonuses;
    public int tempCreditsCount;

    private int creditsCount;
    private int livesCount;
    public TMP_Text creditsCountPanelText;
    public TMP_Text creditsCountShopText;
    public Slider creditsCountSlider;

    public TMP_Text livesCountPanelText;

    public TMP_Text infoText;
    public float fadeDuration = 6.0f; // Duration of the fade

    private void Awake()
    {
        tempBonuses = new int[5];
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
        livesCount = gameDataClass.saveData.lives;

        //add prices
        gameDataClass.saveData.bonusesPrice = bonusPrice;

        //temp credits
        tempCreditsCount = creditsCount;

        //show data
        if(creditsCountPanelText != null && livesCountPanelText != null)
        {
            creditsCountPanelText.text = "" + creditsCount;
            livesCountPanelText.text = "" + livesCount;
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

        //set credits
        gameDataClass.saveData.credits = tempCreditsCount;

        //update text on panel
        creditsCountPanelText.text = "" + gameDataClass.saveData.credits;
        livesCountPanelText.text = "" + gameDataClass.saveData.lives;
    }

    public void ShowInfo(int price)
    {
        infoText.text = "Not enough credits to purchase! This bonus costs " + price + " credits";
        StartCoroutine(FadeOutText());
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


    // Update is called once per frame
    void Update()
    {
                
    }
}
