using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BonusButton : MonoBehaviour
{
    //classes
    private GameData gameDataClass;
    public int bonusNumber;
    private int bonusCount;
    public TMP_Text bonusCountText;

    // Start is called before the first frame update
    void Start()
    {
        //classes        
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        bonusCount = gameDataClass.saveData.bonuses[bonusNumber];

        bonusCountText.text = "" + bonusCount;

        if (bonusCount == 0)
        {
            this.GetComponent<Button>().interactable = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
