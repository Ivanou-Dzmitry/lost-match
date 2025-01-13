using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using Unity.VisualScripting;


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
    public GameObject levelButton3DPrefab; // Assign the prefab in the Inspector
    public Transform parentTransform3D;
    public GameObject levelBackSegment;

    public Transform parentTransform;   // Assign the parent transform for layout in the Inspector
    public GameObject panelWithButtons;

    [Header("Panels")]  
    public GameObject[] allPanelsList;

    //private int elementsPadding;

    //for swipe
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float swipeThreshold = 2.0f; // Minimum distance for a swipe
    private float maxSwipeLenght = 100.0f;

    private bool swipeDetected = false; // Prevent repeated triggers
    private bool isRotating = false; // Track if rotation is in progress

    [Header("Center Panel")]
    public GameObject centerPanel;
    public Sprite[] centerPanelImages;
    //private Image backSprite;

    //public Button[] centerPanelButtons;

    [Header("Particles")]
    public GameObject swipeParticles;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    private float previousScrollValue = 0f;

    [Header("DEbug")]
    public TMP_Text levelTxt;

    public GameObject cylinder; // Reference to your cylinder object
    public float rotationAmount = 45f; // Rotation amount in degrees
    public float rotationSpeed = 5f; // Speed for smooth rotation
    private float targetRotationX;     // Desired X-axis rotation
    private float currentRotationX = 0f;
    
    private float oneStep; // 45+45=1
    private int totalSteps;

    //private float rotationRange = 90;

    private GameObject segmentUnder;
    private GameObject segmentCurrent;
    private GameObject segmentAbove;

    public List<GameObject> segmentsList = new List<GameObject>(); // Empty list

    private Coroutine rotationCoroutine = null; // To manage the rotation coroutine

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
        }

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(thisSceneMusic);
        }

        DeleteCurrentLevelButtons();

        currentScreenNumber = GetRoundedValue(lastLevel, 10);
        levelTxt.text = "Map " + currentScreenNumber;

        oneStep = 0;

        // get back image
        /*        backSprite = centerPanel.GetComponent<Image>();
                backSprite.sprite = centerPanelImages[currentScreenNumber-1];*/

        // Initialize the previous scroll value and target rotation
        if (scrollRect != null)
            previousScrollValue = scrollRect.verticalNormalizedPosition;

        if (cylinder != null)
        {
            currentRotationX = cylinder.transform.localEulerAngles.x;
            targetRotationX = cylinder.transform.eulerAngles.x;
            cylinder.transform.eulerAngles = new Vector3(targetRotationX, 0f, 0f); // Fix initial orientation

            SegmentInsnaciate(90, "levels02");            
            SegmentInsnaciate(0, "levels01");            
            SegmentInsnaciate(-90, "levels02");
        }

        totalSteps = 0;

        LoadLevelButtons(currentScreenNumber, segmentsList[1].transform, 0);

        Debug.Log(currentScreenNumber + "/-" + totalSteps + "/-" + targetRotationX + "/-" + oneStep);
    }

    private void SegmentInsnaciate(float rotation, string matName, int sN=-1)
    {
        Quaternion segRotation = Quaternion.Euler(rotation, 0f, 0f); // 90 degrees on X-axis
        GameObject segment = Instantiate(levelBackSegment, parentTransform3D.position, segRotation);
        segment.transform.SetParent(parentTransform3D);
        segment.transform.localScale = Vector3.one;  // (1, 1, 1)
        segment.name = "seg_" + matName +"_" + rotation + "_" + totalSteps;

        string pathToMaterial = "Materials/For_levels/" + matName;

        AdssignMaterial(pathToMaterial, segment);

        if (sN == -1) 
        {
            segmentsList.Add(segment);
        }
        else
        {
            segmentsList.Insert(sN, segment);
        }
        
    }

    public void LoadLevelButtons(int currentScreenNumber, Transform parentTransform, float rotation)
    {
        int startNumber = currentScreenNumber * 10;      // Upper bound
        int endNumber = startNumber - 9;                // Lower bound

        InstantiateLevelButtons(startNumber, endNumber, parentTransform, rotation);        
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

        GameObject[] levelButtons3D = GameObject.FindGameObjectsWithTag("LevelButton3D");

        foreach (GameObject button in levelButtons3D)
        {
            Destroy(button);
        }
    }

    public void  NextLevels()
    {       
        currentScreenNumber += 1;
        int startNumber = currentScreenNumber * 10;
        currentScreenNumber -= 1;

        //if levels exists
        if (startNumber <= levelsCount)
        {

            if (startNumber < levelsCount)
            {
                targetRotationX -= rotationAmount;
                oneStep += rotationAmount;
                totalSteps ++;

                InstanciateFunc(true, 180, "levels02", 0);
            }
            
            Debug.Log("oneStep: " + oneStep);

            if (oneStep == 90)
            {
                currentScreenNumber ++;                
                oneStep = 0;
                DeleteChildrenWithTag(segmentsList[2], "LevelButton3D");
                levelTxt.text = "Map " + currentScreenNumber;
            }

            if (oneStep == 45)
            {
                int nextLevelNumber = currentScreenNumber + 1;
                LoadLevelButtons(nextLevelNumber, segmentsList[1].transform, 90);
            }

                Rotator();            
        }
        else
        {
            //currentScreenNumber -= 1; //return curent screen number
            //startNumber = currentScreenNumber * 10;
            DeleteChildrenWithTag(segmentsList[2], "LevelButton3D");
        }

        Debug.Log("Next" + currentScreenNumber + "/" + totalSteps + "/" + targetRotationX + "/" + oneStep);
    }

    public void PreviousLevels()
    {
      //  Debug.Log(currentScreenNumber +"/"+ totalSteps + "/"+ targetRotationX + "/" + oneStep);


        if (currentScreenNumber > 1)
        {                                   
            targetRotationX += rotationAmount;
            oneStep += rotationAmount;
            totalSteps --;

            InstanciateFunc(false, -180, "levels01", 2);

            if (oneStep == 90)
            {
                //DeleteCurrentLevelButtons();
                currentScreenNumber -= 1;
                levelTxt.text = "Map " + currentScreenNumber;
                oneStep = 0;

                DeleteChildrenWithTag(segmentsList[0], "LevelButton3D");
            }

            if (oneStep == 45)
            {
                LoadLevelButtons(currentScreenNumber - 1, segmentsList[1].transform, -90);
            }

            Rotator();
        }
        else
        {
            //cylinder.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
            targetRotationX += 0;            
            currentScreenNumber = 1;
            //oneStep = 0;
            DeleteChildrenWithTag(segmentsList[0], "LevelButton3D");
        }

        Debug.Log("Out:" + currentScreenNumber + "/" + totalSteps + "/" + targetRotationX + "/" + oneStep);
    }

    private void InstanciateFunc(bool isNext, float rotation, string materialName, int insertIndex)
    {
        if (totalSteps % 2 != 0)
        {
            RemoveSegment(isNext, rotation, materialName, insertIndex);
        }
    }

    void InstantiateLevelButtons(int startNumber, int endNumber, Transform parentTrnasform, float rotation)
    {
        //centerPanelButtons = null;

        float startRotation = 5f; // Starting rotation on the X-axis
        float rotationStep = -9f;    // Decrement step for each object

        float xOffsetEven = 0.30f;   // X-axis offset for even indices
        float xOffsetOdd = -0.30f;   // X-axis offset for odd indices

        for (int i = startNumber; i >= endNumber; i--)
        {
            //3d
            GameObject new3DButton = Instantiate(levelButton3DPrefab, parentTrnasform);
            new3DButton.name = "Button3d_" + i;   
            
            float currentRotation = (startRotation + (endNumber - i) * rotationStep) + rotation;

            new3DButton.transform.rotation = Quaternion.Euler(currentRotation, 0, 0);
            new3DButton.tag = "LevelButton3D";

            // Calculate the X-axis offset
            float xOffset = (i % 2 == 0) ? xOffsetEven : xOffsetOdd;

            // Apply the position offset
            Vector3 currentPosition = new3DButton.transform.position;
            new3DButton.transform.position = new Vector3(currentPosition.x + xOffset, currentPosition.y, currentPosition.z);

            LevelButton levelButtonScript3D = new3DButton.GetComponent<LevelButton>();

            if (levelButtonScript3D != null && new3DButton.tag == "LevelButton3D")
            {
                levelButtonScript3D.level = i;
                levelButtonScript3D.confirmPanel = allPanelsList[0];
            }
            else
            {
                Debug.LogWarning("The 3D prefab does not have a LevelButton script attached.");
            }
        }  
    }


    void Update()
    {
        bool panelsActivity = true;
        panelsActivity = PanelActivity();
        
        if (panelsActivity == false)
            SwipeDetector();

        if (Input.GetMouseButtonDown(0)) // Detect left mouse button click
        {
            // Create a ray from the camera through the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Perform the raycast
            if (Physics.Raycast(ray, out hit))
            {
                //Debug.Log("Click"+ ray + "/ "+ hit.collider.gameObject.name);
                // Check if the clicked object is the plane
                if (hit.collider.gameObject.name != null) // Assuming this script is on the plane
                {
                    LevelButton lb = hit.collider.gameObject.GetComponent<LevelButton>();

                    if (lb != null && lb.isActive)
                    {
                        lb.ShowConfirmPanel(lb.level);
                    }                    
                }
            }
        }

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
            rotationCoroutine = null;

            if (verticalSwipeDistance > 0)
            {
                Debug.Log("Previous Levels");
                PreviousLevels();                
            }
            else
            {
                Debug.Log("Next Levels");
                NextLevels();
            }
        }
    }

    private void Rotator()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine); // Stop the current rotation if it's ongoing
        }

        rotationCoroutine = StartCoroutine(SmoothRotate());
    }

    private IEnumerator SmoothRotate()
    {
        isRotating = true;

        while (Mathf.Abs(Mathf.DeltaAngle(currentRotationX, targetRotationX)) > 0.1f)
        {
            // Interpolate towards the target rotation
            currentRotationX = Mathf.Lerp(currentRotationX, targetRotationX, Time.deltaTime * rotationSpeed);

            // Apply the rotation
            cylinder.transform.localRotation = Quaternion.Euler(new Vector3(currentRotationX, 0f, 0f));

            yield return null; // Wait for the next frame
        }

        isRotating = false;

        // Snap to the exact target rotation at the end
        currentRotationX = Mathf.Round(targetRotationX / 45f) * 45f; // Ensure it's an exact multiple of 45
        cylinder.transform.localRotation = Quaternion.Euler(new Vector3(currentRotationX, 0f, 0f));

        rotationCoroutine = null;
    }


    public void AdssignMaterial(string materialPath, GameObject gameObj)
    {
        // Load the material from the Resources folder using the given materialPath
        Material newMaterial = Resources.Load<Material>(materialPath);

        if (newMaterial != null)
        {
            // Get the MeshRenderer from the segmentAbove (including child objects)
            MeshRenderer segmentRenderer = gameObj.GetComponentInChildren<MeshRenderer>();

            if (segmentRenderer != null)
            {
                // Access and modify the materials array
                Material[] materials = segmentRenderer.materials;
                if (materials.Length > 0)
                {
                    materials[0] = newMaterial; // Set the first material to the new material
                    segmentRenderer.materials = materials; // Reassign the array back to the renderer
                }
            }
            else
            {
                Debug.LogWarning("MeshRenderer not found on the instantiated object.");
            }
        }
        else
        {
            Debug.LogError("Material not found! Ensure it's in a 'Resources' folder at path: " + materialPath);
        }
    }

    void RemoveSegment(bool isNext, float rotation, string materialName, int insertIndex)
    {
        int segmentIndexToRemove = isNext ? segmentsList.Count - 1 : 0;

        // Remove and destroy the specified segment
        GameObject segmentToRemove = segmentsList[segmentIndexToRemove];
        segmentsList.RemoveAt(segmentIndexToRemove);
        Destroy(segmentToRemove);

        // Add a new segment at the specified index
        SegmentInsnaciate(rotation, materialName, insertIndex);

        // Update segment names
        segmentsList[0].name = "above";
        segmentsList[1].name = "current";
        segmentsList[2].name = "under";
    }

    void DeleteChildrenWithTag(GameObject parentObject, string tagToMatch)
    {
        // Loop through all child objects of the parent
        foreach (Transform child in parentObject.transform)
        {
            // Check if the child's tag matches the specified tag
            if (child.CompareTag(tagToMatch))
            {
                // Destroy the child GameObject
                Destroy(child.gameObject);
            }
        }
    }

}
