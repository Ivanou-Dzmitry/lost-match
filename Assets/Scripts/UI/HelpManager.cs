using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HelpManager : MonoBehaviour
{
    public GameObject helpPanel;
    public TMP_Text helpText;
    public Image helpImage;
    public Button helpButton;
    public TMP_Text helpButtonText;

    public string[] helpTxt;
    public Sprite[] helpImg;

    private int helpPage;

    public void SetHelp()
    {
        helpPage = 0;

        helpText.text = helpTxt[helpPage];
        helpImage.sprite = helpImg[helpPage];

        helpButtonText.text = "Next";
    }

    private void Awake()
    {
        SetHelp();
    }

    public void HelpScroll()
    {
        helpPage += 1;

        if(helpPage < helpTxt.Length)
        {
            helpText.text = helpTxt[helpPage];
            helpImage.sprite = helpImg[helpPage];

            if (helpPage == helpTxt.Length - 1)
                helpButtonText.text = "OK";
        }
    }

    public void HelpClose()
    {
        if(helpPage == helpTxt.Length)
        {
            helpPage = 0;
            SetHelp();
            helpPanel.SetActive(false); 
        }
    }

}
