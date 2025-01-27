using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


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

    public Material[] levelMaterials;

    //private int elementsPadding;

    //for swipe
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float swipeThreshold = 2.0f; // Minimum distance for a swipe
    private float maxSwipeLenght = 100.0f;

    private bool swipeDetected = false; // Prevent repeated triggers
    private bool isRotating = false; // Track if rotation is in progress

    [Header("DEbug")]
    public TMP_Text levelTxt;

    public GameObject levelCylinder; // Reference to your cylinder object
    public float rotationAmount = 45f; // Rotation amount in degrees
    public float rotationSpeed = 5f; // Speed for smooth rotation
    private float targetRotationX;     // Desired X-axis rotation
    private float currentRotationX = 0f;
    
    private int totalSteps;
    private int maxSteps;
    int levelSegmentsCount;

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

        levelSegmentsCount = levelsCount / 10; //each segment 10 levels
        
        Debug.Log("levelSegmentsCount" + levelSegmentsCount);
        
        maxSteps = (levelsCount / levelSegmentsCount); //important each level - 2 steps/ -1 avoid necessary rotation

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(thisSceneMusic);
        }

        DeleteCurrentLevelButtons();

        currentScreenNumber = GetRoundedValue(lastLevel, 10);
        levelTxt.text = "Map " + currentScreenNumber;

        if (levelCylinder != null)
        {
            currentRotationX = levelCylinder.transform.localEulerAngles.x;
            targetRotationX = levelCylinder.transform.eulerAngles.x;
            levelCylinder.transform.eulerAngles = new Vector3(targetRotationX, 0f, 0f); // Fix initial orientation
            
            SegmentInsnaciate(0, currentScreenNumber, 0);   // add first segment         
        }

        //load buttons
        LoadLevelButtons(currentScreenNumber, segmentsList[0].transform, 0);

        totalSteps = GetTotalSteps(currentScreenNumber);

        Debug.Log($"[INTRO] Screen: {currentScreenNumber}, TotalSteps: {totalSteps}, TargetRotationX: {targetRotationX}, Max{maxSteps}");
    }


    int GetTotalSteps(int currentScreenNumber)
    {
        return 2 * (currentScreenNumber - 1);
    }


    private void SegmentInsnaciate(float rotation, int screen, int sN=-1)
    {
        Quaternion segRotation = Quaternion.Euler(rotation, 0f, 0f); // 90 degrees on X-axis
        GameObject segment = Instantiate(levelBackSegment, parentTransform3D.position, segRotation);
        segment.transform.SetParent(parentTransform3D);
        segment.transform.localScale = Vector3.one;  // (1, 1, 1)               
        
        //material
        if(screen > 0)
            AdssignMaterial(screen, segment);

        if (sN == -1) 
        {
            segmentsList.Add(segment);
        }
        else
        {
            segmentsList.Insert(sN, segment);
        }

        segment.name = "segment_" + "_" + sN;
    }

    public void LoadLevelButtons(int currentScreenNumber, Transform parentTransform, float rotation)
    {
        int startNumber = currentScreenNumber * 10;      // Upper bound
        int endNumber = startNumber - 9;                // Lower bound

        InstantiateLevelButtons(startNumber, endNumber, parentTransform, rotation);        
    }

    void InstantiateLevelButtons(int startNumber, int endNumber, Transform parentTrnasform, float rotation)
    {
        float startRotation = 8.5f; // Starting rotation on the X-axis
        float rotationStep = -8.5f;    // Decrement step for each object

        float xOffsetEven = 0.4f;   // X-axis offset for even indices
        float xOffsetOdd = -0.4f;   // X-axis offset for odd indices

        //DeleteChildrenWithTag(segmentsList[forwardSegmentIndex], "LevelButton3D");

        for (int i = startNumber; i >= endNumber; i--)
        {
            // Create the base name for the button
            string baseName = "Button3d_" + i;
            GameObject existingButton = GameObject.Find(baseName);
            if (existingButton != null)
            {
                // If the object exists, destroy it
                Destroy(existingButton);
            }

            //3d
            GameObject new3DButton = Instantiate(levelButton3DPrefab, parentTrnasform);
            new3DButton.name = "Button3d_" + i;

            float currentRotation = (startRotation + (endNumber - i) * rotationStep) + rotation;

            new3DButton.transform.rotation = Quaternion.Euler(currentRotation, 0, 0);
            //new3DButton.transform.localRotation = Quaternion.identity;
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

            MovePivotToCenter(new3DButton);            
        }
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

    private IEnumerator FadeAndDestroy(GameObject obj, float delay)
    {
        if (obj == null) yield break;

        yield return new WaitForSeconds(delay); // Wait for the specified delay

        Destroy(obj); // Destroy the object after the delay
    }


    public void  NextLevels()
    {
        DebugLogger("NEXT IN");
                   
        targetRotationX -= rotationAmount;

        totalSteps++;

        if (totalSteps % 2 == 0)
        {
            currentScreenNumber++;

            if (totalSteps <= maxSteps)
            {                
                GameObject segmentToRemove = segmentsList[0];
                segmentsList.RemoveAt(0); // Automatically shifts the remaining elements
                StartCoroutine(FadeAndDestroy(segmentToRemove, 1f));                
            }
        }

        if (totalSteps % 2 != 0)
        {
            if (totalSteps <= maxSteps - 1)
            {
                int nextScreen = currentScreenNumber + 1;

                SegmentInsnaciate(90.0f, nextScreen, 1);
                
                if(nextScreen <= levelSegmentsCount)
                    LoadLevelButtons(currentScreenNumber + 1, segmentsList[1].transform, 90.0f);
            }
        }

        if (currentScreenNumber <= levelSegmentsCount)
        {
            levelTxt.text = "Map " + currentScreenNumber;
        }
        else
        {
            levelTxt.text = "";
        }
            

        if (totalSteps <= maxSteps)
            Rotator();

        DebugLogger("NEXT OUT");
    }

    public void PreviousLevels()
    {
        DebugLogger("PREV IN");
       
        if (totalSteps >= 1)
            targetRotationX += rotationAmount;

        if (totalSteps == 1)
            Rotator();

        totalSteps--;

        if (totalSteps % 2 != 0)
        {
            currentScreenNumber--;
            SegmentInsnaciate(-90.0f, currentScreenNumber, 0);

            if (currentScreenNumber - 1  >= 0)
                LoadLevelButtons(currentScreenNumber, segmentsList[0].transform, -90.0f);
        }

        if (totalSteps % 2 == 0)
        {
            GameObject segmentToRemove = segmentsList[1];
            segmentsList.RemoveAt(1); // Automatically shifts the remaining elements
            StartCoroutine(FadeAndDestroy(segmentToRemove, 1f));
        }

        if (totalSteps >= 1)
            Rotator();

        levelTxt.text = "Map " + currentScreenNumber;

        DebugLogger("PREV OUT");
    }

    private void DebugLogger(string where)
    {
        Debug.Log($"[PREV] {where} Screen: {currentScreenNumber}, TotalSteps: {totalSteps}, TargetRotationX: {targetRotationX}");
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
                    LevelButton lvlButton = hit.collider.gameObject.GetComponent<LevelButton>();

                    if (lvlButton != null && lvlButton.isActive && panelsActivity == false)
                    {
                        lvlButton.ShowConfirmPanel(lvlButton.level);
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

            if (verticalSwipeDistance > 0)
            {
                //Debug.Log("Previous Levels");
                if (totalSteps > 0)
                    PreviousLevels();  
            }
            else
            {
          //Debug.Log("Next Levels");
                if(totalSteps < maxSteps)
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
            levelCylinder.transform.localRotation = Quaternion.Euler(new Vector3(currentRotationX, 0f, 0f));

            yield return null; // Wait for the next frame
        }
       
        // Snap to the exact target rotation at the end
        currentRotationX = Mathf.Round(targetRotationX / 45f) * 45f; // Ensure it's an exact multiple of 45
        levelCylinder.transform.localRotation = Quaternion.Euler(new Vector3(currentRotationX, 0f, 0f));

        isRotating = false;        
    }


    public void AdssignMaterial(int currentScreenNumber, GameObject gameObj)
    {
        // Check if the currentScreenNumber is within the bounds of the levelMaterials array
        if (currentScreenNumber >= 1 && currentScreenNumber <= levelMaterials.Length)
        {
            // Get the material based on the current screen number
            Material newMaterial = levelMaterials[currentScreenNumber - 1]; // Array is 0-based, so subtract 1

            // Get the MeshRenderer from the game object (including child objects)
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
            Debug.LogError("currentScreenNumber is out of bounds! Ensure it is within the levelMaterials array range.");
        }
    }

    void RotateButtons()
    {
        // Find all objects with the tag "LevelButton3D"
        GameObject[] levelButtons = GameObject.FindGameObjectsWithTag("LevelButton3D");

        // Loop through each object and set its rotation
        foreach (GameObject button in levelButtons)
        {
            if (button != null)
            {
                button.transform.rotation = Quaternion.Euler(0, 0, 0); // Set rotation to (0, 0, 0)
            }
        }
    }


    void MovePivotToCenter(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.mesh != null)
        {
            Mesh mesh = meshFilter.mesh;

            // Calculate the center of the mesh's bounds
            Vector3 meshCenter = mesh.bounds.center;

            // Offset the vertices to move the pivot to the center
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= meshCenter;
            }

            // Update the mesh with the modified vertices
            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            // Move the object's transform to compensate for the vertex shift
            obj.transform.position += obj.transform.TransformVector(meshCenter);
        }
        else
        {
            Debug.LogWarning("MeshFilter or Mesh is missing on the object.");
        }
    }

}
