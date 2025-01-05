using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelsSceneManager : MonoBehaviour
{
    private UIManager uiManagerClass;
    private GameData gameDataClass;
    private int lastLevel;
    private int levelsCount;
    public int currentScreenNumber;

    [Header("Music")]
    private SoundManager soundManagerClass;
    public AudioClip thisSceneMusic;

    [Header("Load Levels")]
    public GameObject levelButtonPrefab; // Assign the prefab in the Inspector
    public Transform parentTransform;   // Assign the parent transform for layout in the Inspector
    public GameObject panelWithButtons;

    [Header("Panels")]  
    public GameObject[] allPanelsList;

    private int elementsPadding;

    //for swipe
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float swipeThreshold = 2.0f; // Minimum distance for a swipe
    private float maxSwipeLenght = 100.0f;

    private bool swipeDetected = false; // Prevent repeated triggers

    [Header("Center Panel")]
    public GameObject centerPanel;
    public Sprite[] centerPanelImages;
    private Image backSprite;

    //public Button[] centerPanelButtons;

    [Header("Particles")]
    public GameObject swipeParticles;

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

            //get levels count
            levelsCount = gameDataClass.saveData.isActive.Length;
            //Debug.Log(levelsCount +"/"+lastLevel);
        }

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(thisSceneMusic);
        }

        DeleteCurrentLevelButtons();

        currentScreenNumber = GetRoundedValue(lastLevel, 10);        

        LoadLevelButtons(currentScreenNumber);

        // get back image
        backSprite = centerPanel.GetComponent<Image>();
        backSprite.sprite = centerPanelImages[currentScreenNumber-1];
    }

    public void LoadLevelButtons(int currentScreenNumber)
    {
        int startNumber = currentScreenNumber * 10;      // Upper bound
        int endNumber = startNumber - 9;                // Lower bound

        InstantiateLevelButtons(startNumber, endNumber);

        levelTxt.text = "Map " + currentScreenNumber;
    }

    int GetRoundedValue(int numerator, int denominator)
    {
        return (int)Math.Ceiling(numerator / (double)denominator);
    }

    public void DeleteCurrentLevelButtons()
    {
       
        GameObject[] levelButtons = GameObject.FindGameObjectsWithTag("LevelButton");

        foreach (GameObject button in levelButtons)
        {
            Destroy(button);
        }
    }

    public void  Next10Levels()
    {        
        currentScreenNumber += 1;
        
        int startNumber = currentScreenNumber * 10;
        
        //if levels exists
        if(startNumber <= levelsCount)
        {
            DeleteCurrentLevelButtons();
            LoadLevelButtons(currentScreenNumber);

            backSprite.sprite = centerPanelImages[currentScreenNumber - 1];
            SwipeParticlesRun();
        }
        else
        {
            currentScreenNumber -= 1; //return curent screen number
        }               
    }

    public void Previous10Levels()
    {
        DeleteCurrentLevelButtons();

        if (currentScreenNumber > 1)
        {
            currentScreenNumber -= 1;
            SwipeParticlesRun();
        }
            

        backSprite.sprite = centerPanelImages[currentScreenNumber - 1];

        LoadLevelButtons(currentScreenNumber);
    }

    void InstantiateLevelButtons(int startNumber, int endNumber)
    {
        //centerPanelButtons = null;

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
                levelButtonScript.confirmPanel = allPanelsList[0];

                //get final button
                Button buttonComponent = newButton.GetComponentInChildren<Button>();

                RectTransform buttonRectTransform = buttonComponent.GetComponent<RectTransform>();
                buttonRectTransform.sizeDelta = new Vector2(elementDimension, elementDimension);

                // Get the anchored position of the button
                Vector2 anchoredPosition = buttonRectTransform.anchoredPosition;

                // Generate a random value between 0 and 64
                //float randomValue = UnityEngine.Random.Range(-16f, 16f);

                //important - distance from center
                if (i % 2 == 0)  // Even index
                {
                    anchoredPosition.x += elementDimension/2;  // Add 10 to the x position
                }
                else  // Odd index
                {
                    anchoredPosition.x -= elementDimension/2;  // Subtract 10 from the x position
                }

                //distribution
                buttonRectTransform.anchoredPosition = anchoredPosition;

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


    void Update()
    {
        bool panelsActivity = true;
        panelsActivity = PanelActivity();
        
        if (panelsActivity == false)
            SwipeDetector();        
    }

    private void SwipeDetector()
    {        
        // Touch Input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    startTouchPosition = touch.position;
                    swipeDetected = false; // Reset for a new swipe
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Ended:
                    if (!swipeDetected)
                    {
                        endTouchPosition = touch.position;
                        HandleSwipe();
                        swipeDetected = true; // Mark swipe as detected
                    }
                    break;
            }
        }

        // Mouse Input (for testing on PC)
        if (Input.GetMouseButtonDown(0))
        {
            startTouchPosition = Input.mousePosition;
            swipeDetected = false; // Reset for a new swipe
        }

    }

    private bool PanelActivity()
    {
        bool pnlAct = false;

        for(int i = 0; i < allPanelsList.Length; i++)
        {
            if (allPanelsList[i].activeSelf == true)
            {
                pnlAct = true;
            }
        }

        return pnlAct;
    }


    private void HandleSwipe()
    {
        float verticalSwipeDistance = endTouchPosition.y - startTouchPosition.y;

        // Check if the swipe distance exceeds the threshold. maxSwipeLenght - Avoid button click for Shops
        if (Mathf.Abs(verticalSwipeDistance) > swipeThreshold && Mathf.Abs(verticalSwipeDistance) < maxSwipeLenght)
        {
            if (verticalSwipeDistance > 0)
            {
                    SwipeUp();
            }
            else
            {
                    SwipeDown();
            }
            
        }
    }

    private void SwipeParticlesRun()
    {
        // Get the mouse position in world coordinates
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 11f; // Set a z-axis value (distance from the camera)
        Vector3 particlePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // Instantiate the particle effect
        GameObject particleEffect = Instantiate(swipeParticles, particlePosition, Quaternion.identity);
        // Optionally, ensure the particle system starts playing
        ParticleSystem particleSystem = particleEffect.GetComponent<ParticleSystem>();
        particleSystem.Play();
        //Debug.Break();
        Destroy(particleEffect, 1.9f);
    }

    private void SwipeUp()
    {
        
        Previous10Levels();
    }

    private void SwipeDown()
    {
        Next10Levels();
    }


}
