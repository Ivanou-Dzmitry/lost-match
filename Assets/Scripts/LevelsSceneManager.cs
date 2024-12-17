using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LevelsSceneManager : MonoBehaviour
{
    private UIManager uiManagerClass;
    private GameData gameDataClass;
    private int lastLevel;
    private int currentScreenNumber;

    [Header("Music")]
    private SoundManager soundManagerClass;
    public AudioClip thisSceneMusic;

    [Header("Load Levels")]
    public GameObject levelButtonPrefab; // Assign the prefab in the Inspector
    public Transform parentTransform;   // Assign the parent transform for layout in the Inspector
    public GameObject panelWithButtons;
    public GameObject confirmPanel;     // Assign the ConfirmPanel from the scene in the Inspector

    private int elementsPadding;

    [Header("DEbug")]
    public TMP_Text levelTxt;

    // Start is called before the first frame update
    void Start()
    {
        //class init
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
        uiManagerClass = GameObject.FindWithTag("UIManager").GetComponent<UIManager>();
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        lastLevel = 0;

        //get last level
        if (gameDataClass != null)
        {
            for (int i = 0; i < gameDataClass.saveData.isActive.Length; i++)
            {
                if (gameDataClass.saveData.isActive[i])
                {
                    lastLevel++;
                }
            }
        }

        Debug.Log("Opened levels: " + lastLevel);

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(thisSceneMusic);
        }

        DeleteCurrentLevelButtons();
        InstantiateLevelButtons(10, 1);

        currentScreenNumber = 1;
        levelTxt.text = "" + currentScreenNumber;
    }

    public void DeleteCurrentLevelButtons()
    {
       
        GameObject[] levelButtons = GameObject.FindGameObjectsWithTag("LevelButton");

        foreach (GameObject button in levelButtons)
        {
            Destroy(button);
        }
    }

    public void Next10Levels()
    {
        DeleteCurrentLevelButtons();

        int endNumber = (currentScreenNumber*10 + 1);

        currentScreenNumber += 1;
        int startNumber = currentScreenNumber * 10;
        
        InstantiateLevelButtons(startNumber, endNumber);        
    }

    public void Previous10Levels()
    {
        DeleteCurrentLevelButtons();

        if (currentScreenNumber > 1)
            currentScreenNumber -= 1;        

        int startNumber = (currentScreenNumber * 10);

        int endNumber = startNumber - 9;

        InstantiateLevelButtons(startNumber, endNumber);
    }

    void InstantiateLevelButtons(int startNumber, int endNumber)
    {
        levelTxt.text = ""+ currentScreenNumber;

        for (int i = startNumber; i >= endNumber; i--)
        {
            // Instantiate the prefab
            GameObject newButton = Instantiate(levelButtonPrefab, parentTransform);

            //Get transform
            RectTransform rectTransform = newButton.GetComponent<RectTransform>();

            float elementDimension = 128; //default

            elementsPadding = 0;

            VerticalLayoutGroup layoutGroup = panelWithButtons.GetComponent<VerticalLayoutGroup>();

            //get padding
            elementsPadding = (int)(layoutGroup.spacing);

            //get size panel.y/10
            elementDimension = (uiManagerClass.LevelButtonsPanelSize()/10) - elementsPadding; //dimension

            if (rectTransform != null)
            {
                // Set the width and height
                rectTransform.sizeDelta = new Vector2(elementDimension, elementDimension); // Example: width = 200, height = 100
            }

            //naming and tagging
            newButton.name = "LevelButton_" + i;
            newButton.tag = "LevelButton";

            // Access the LevelButton script on the instantiated prefab
            LevelButton levelButtonScript = newButton.GetComponent<LevelButton>();

            if (levelButtonScript != null)
            {
                // Set the Level value
                levelButtonScript.level = i;

                // Set the ConfirmPanel reference
                levelButtonScript.confirmPanel = confirmPanel;

                Button buttonComponent = newButton.GetComponent<Button>();

                //set action for open
                if (buttonComponent != null)
                {
                    buttonComponent.onClick = new Button.ButtonClickedEvent();                    
                    buttonComponent.onClick.AddListener(() => levelButtonScript.ShowConfirmPanel(levelButtonScript.level)); // Add listener
                }

            }
            else
            {
                Debug.LogWarning("The prefab does not have a LevelButton script attached.");
            }
        }
    }

}
