using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BomusShop : MonoBehaviour
{
    //classes
    private GameData gameDataClass;

    private int creditsCount;
    public TMP_Text creditsCountPanelText;
    public TMP_Text creditsCountShopText;

    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        creditsCount = gameDataClass.saveData.credits;

        creditsCountPanelText.text = "" + creditsCount;
        creditsCountShopText.text = "Credits: " + creditsCount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
