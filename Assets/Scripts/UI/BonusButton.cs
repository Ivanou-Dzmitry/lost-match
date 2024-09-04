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
            this.GetComponentInChildren<Button>().interactable = false;
            bonusCountText.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
