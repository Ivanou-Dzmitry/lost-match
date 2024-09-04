using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BonusShop : MonoBehaviour
{
    //classes
    private GameData gameDataClass;

    private int creditsCount;
    private int livesCount;
    public TMP_Text creditsCountPanelText;
    public TMP_Text creditsCountShopText;

    public TMP_Text livesCountPanelText;

    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        creditsCount = gameDataClass.saveData.credits;
        livesCount = gameDataClass.saveData.lives;

        creditsCountPanelText.text = "" + creditsCount;
        

        livesCountPanelText.text = "" + livesCount;
    }

    public void OpenShop()
    {
        creditsCountShopText.text = "Credits: " + creditsCount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
