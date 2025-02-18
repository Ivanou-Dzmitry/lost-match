using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;


public enum GameState
{
    wait,
    move,
    win,
    lose,
    pause
}

public enum MatchState
{
    matching_stop,
    matching_inprogress
}

public enum TileKind
{
    element_01,
    element_02,
    element_03,
    element_04,
    element_05,
    Empty,
    Blocker01,
    Blocker02,
    ColumnBomb,
    RowBomb,
    WrapBomb,
    ColorBomb,
    Breakable01,
    Breakable02,
    Locked01,
    Expanding01,
    Breakable03,
    Blocker03,
    Locked02,
    Locked03,
    Expanding02,
    Expanding03
}

//type of matches
[System.Serializable]
public class MatchType
{
    public int type;
    public string color;
    public GameObject curElem;

    public override bool Equals(object obj)
    {
        if (obj is MatchType other)
        {
            return type == other.type && color == other.color && curElem == other.curElem;
        }
        return false;
    }

    public override int GetHashCode()
    {
        int hashCode = 17;
        hashCode = hashCode * 23 + type.GetHashCode();
        hashCode = hashCode * 23 + (color?.GetHashCode() ?? 0);
        hashCode = hashCode * 23 + (curElem?.GetHashCode() ?? 0);
        return hashCode;
    }
}

//type of tiles
[System.Serializable]
public class TileType
{
    public int columnX;
    public int rowY;
    public TileKind tileKind;
}

[System.Serializable]
public class GameBoardBack
{
    public Sprite gameBoardBackSprite;
}


public class GameBoard : MonoBehaviour
{
    [Header("Scriptable Objects")]
    public World worldClass;

    [Header("Level Info")]
    public int level;
    public TMP_Text levelNumberTxt;
    public AudioClip levelMusic;

    public GameState currentState;
    public MatchState matchState;

    [Header("Size")]
    public int column;
    public int row;

    public float refillDelay = 0.2f;
    public float destroyDelay = 1f;

    [Header("Layout")]
    public TileType[] boardLayout;
    public TileType[] preloadBoardLayout;
    public GameObject gameArea;

    public TextAsset xmlDocWithBackTileLayout;

    [Header("Art")]
    public GameObject elementsBackGO;
    public GameBoardBack gameBoardBack;

    //classes
    private GameData gameDataClass;
    private SoundManager soundManagerClass;
    private GoalManager goalManagerClass;
    private MatchFinder matchFinderClass;
    private ScoreManager scoreManagerClass;
    private UIManager uiManagerClass;
    //private BonusShop bonusShopClass;
    private EndGameManager endGameManagerClass;
    private BackBuilder backBuilderClass;
    private FXManager fxManagerClass;

    //arrays
    public GameObject[] elements;
    public GameObject[,] allElements;

    [Header("Match Suff")]
    public ElementController currentElement;

    public MatchType matchTypeClass = new MatchType();

    //for score
    public int baseValue = 1;
    public int streakValue = 1;
    public int[] scoreGoals;
    public string goalsDescription;
    public int minMatchForBomb = 4;

    //for blank
    private bool[,] emptyElement;

    [Header("Breakable")]
    //public GameObject elementPrefab;
    public GameObject break01Prefab;
    public GameObject break02Prefab;
    public GameObject break03Prefab;

    [Header("Blocker")]
    public GameObject blocker01Prefab;
    public GameObject blocker02Prefab;
    public GameObject blocker03Prefab;

    [Header("Expand")]
    public GameObject expand01Prefab;
    public GameObject expand02Prefab;
    public GameObject expand03Prefab;

    [Header("Locker")]
    public GameObject locker01Prefab;
    public GameObject locker02Prefab;
    public GameObject locker03Prefab;

    //for lock
    public SpecialElements[,] lockedCells;

    //for lock
    public SpecialElements[,] expandCells;
    private bool makeExpand = true;

    //for blockers
    public SpecialElements[,] blockerCells;

    //for breakables
    public SpecialElements[,] breakableCells;

    //for bombs
    public ElementController[,] bombsCells;

    //bombs values    
    private int minMatchCount = 3;
    
    private int matchLimit = 81; //awoid match bomb bugs

    private int matchForLineBomb = 4;
    private int matchForWrapBomb = 2;
    private int matchForColorBomb = 3;

    //dict
    private Dictionary<TileKind, int> preloadDict;
    private Dictionary<TileKind, GameObject> breacableDict;
    private Dictionary<TileKind, GameObject> blockersDict;
    private Dictionary<TileKind, GameObject> lockersDict; //lockers
    private Dictionary<TileKind, GameObject> expandDict; //expand

    [Header("Busters")]
    public bool colorBusterInUse;
    public bool lineBusterInUse;
    private int initialMoves; //for time booster run
    private int boosterValue = 10; //value when buster run

    private Coroutine updateCoroutine;    


    private void Awake()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        if (gameDataClass != null)
        {
            gameDataClass.LoadFromFile();
            level = gameDataClass.saveData.levelToLoad; //load level number
        }

        //setup world class
        if (worldClass != null)
        {
            if (level < worldClass.levels.Length)
            {
                if (worldClass.levels[level] != null)
                {
                    column = worldClass.levels[level].columns;

                    row = worldClass.levels[level].rows;

                    elements = worldClass.levels[level].element;

                    scoreGoals = worldClass.levels[level].scoreGoals; //get score goals for stars

                    goalsDescription = worldClass.levels[level].goalsDescription;

                    //gameBoardBack = worldClass.levels[level].elementsBack; //back

                    boardLayout = worldClass.levels[level].boardLayout;

                    preloadBoardLayout = worldClass.levels[level].preloadBoardLayout;

                    xmlDocWithBackTileLayout = worldClass.levels[level].xmlLayoutFile;
                }
            }
        }

        //for blockers
        blockersDict = new Dictionary<TileKind, GameObject>
        {
            { TileKind.Blocker01, blocker01Prefab },
            { TileKind.Blocker02, blocker02Prefab },
            { TileKind.Blocker03, blocker03Prefab }
        };

        //for lockers
        lockersDict = new Dictionary<TileKind, GameObject>
        {
            { TileKind.Locked01, locker01Prefab },
            { TileKind.Locked02, locker02Prefab },
            { TileKind.Locked03, locker03Prefab }
        };

        //for expand
        expandDict = new Dictionary<TileKind, GameObject>
        {
            { TileKind.Expanding01, expand01Prefab },
            { TileKind.Expanding02, expand02Prefab },
            { TileKind.Expanding03, expand03Prefab }
        };


        // Initialize the dictionary for preload elements
        preloadDict = new Dictionary<TileKind, int>
        {
            { TileKind.element_01, 0 },
            { TileKind.element_02, 1 },
            { TileKind.element_03, 2 },
            { TileKind.element_04, 3 },
            { TileKind.element_05, 4 }
        };

        //for break
        breacableDict = new Dictionary<TileKind, GameObject>
        {
            { TileKind.Breakable01, break01Prefab },
            { TileKind.Breakable02, break02Prefab },
            { TileKind.Breakable03, break03Prefab }
        };

    }

    void OnEnable()
    {
        // Start the coroutine to update once per second
        updateCoroutine = StartCoroutine(UpdatePerSec());
    }

    void OnDisable()
    {
        // Stop the coroutine when the object is disabled
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //class init
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
        matchFinderClass = GameObject.FindWithTag("MatchFinder").GetComponent<MatchFinder>();
        scoreManagerClass = GameObject.FindWithTag("ScoreManager").GetComponent<ScoreManager>();
        goalManagerClass = GameObject.FindWithTag("GoalManager").GetComponent<GoalManager>();
        uiManagerClass = GameObject.FindWithTag("UIManager").GetComponent<UIManager>();
        //bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        endGameManagerClass = GameObject.FindWithTag("EndGameManager").GetComponent<EndGameManager>();
        backBuilderClass = GameObject.FindWithTag("BackBuilder").GetComponent<BackBuilder>();
        fxManagerClass = GameObject.FindWithTag("FXManager").GetComponent<FXManager>();

        //all dots on board
        allElements = new GameObject[column, row];

        //init type of objects
        blockerCells = new SpecialElements[column, row];
        lockedCells = new SpecialElements[column, row];
        breakableCells = new SpecialElements[column, row];
        expandCells = new SpecialElements[column, row];

        //boms
        bombsCells = new ElementController[column, row];

        //init type of objects
        emptyElement = new bool[column, row];

        //setup board
        SetUpBoard();

        //Set Framerate
        Application.targetFrameRate = 30;

        //set resoluton
        Screen.SetResolution(1920, 1080, true);
        Screen.SetResolution((int)Screen.width, (int)Screen.height, true);

        //load backTIles!

        if(xmlDocWithBackTileLayout != null)
            backBuilderClass.LoadDataFromXML(xmlDocWithBackTileLayout, column, row);

        //load back sprite
        //elementsBackGO.GetComponent<SpriteRenderer>().sprite = gameBoardBack.gameBoardBackSprite;

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(levelMusic);
        }

        //stop 1
        matchState = MatchState.matching_stop;

        levelNumberTxt.text = "Level " + (level + 1);

        //for time booster
        initialMoves = endGameManagerClass.curCounterVal;
    }

    //empty cells
    public void GenerateEmptyElements()
    {
        for (int i = 0; i < boardLayout.Length; i++)
        {
            if (boardLayout[i].tileKind == TileKind.Empty)
            {
                emptyElement[boardLayout[i].columnX, boardLayout[i].rowY] = true;
            }
        }
    }

    //bubble gum
    private void GenerateBlockers()
    {
        int namingCounter = 0;

        for (int i = 0; i < boardLayout.Length; i++)
        {
            TileKind kind = boardLayout[i].tileKind;

            if (blockersDict.ContainsKey(kind))
            {
                Vector2 tempPos = new Vector2(boardLayout[i].columnX, boardLayout[i].rowY);

                GameObject blockerPrefab = blockersDict[kind];

                GameObject blockerElement = Instantiate(blockerPrefab, tempPos, Quaternion.identity);

                blockerCells[boardLayout[i].columnX, boardLayout[i].rowY] = blockerElement.GetComponent<SpecialElements>();

                namingCounter++;

                //naming
                string elementName = blockerPrefab.tag + "_c" + boardLayout[i].columnX + "_r" + boardLayout[i].rowY + "_" + namingCounter;
                blockerElement.name = elementName;

                //set properties
                blockerElement.transform.parent = gameArea.transform;

                // Add to all
                //allTypeDotsCoord[boardLayout[i].x, boardLayout[i].y] = tempPos;
            }
        }
    }

    public void GenerateBreakable()
    {
        int namingCounter = 0;

        for (int i = 0; i < boardLayout.Length; i++)
        {
            TileKind kind = boardLayout[i].tileKind;

            if (breacableDict.ContainsKey(kind))
            {
                Vector2 tempPos = new Vector2(boardLayout[i].columnX, boardLayout[i].rowY);

                GameObject breakablePrefab = breacableDict[kind];

                GameObject breakableElement = null;

                if (breakablePrefab != null)
                {
                    breakableElement = Instantiate(breakablePrefab, tempPos, Quaternion.identity);
                    breakableCells[boardLayout[i].columnX, boardLayout[i].rowY] = breakableElement.GetComponent<SpecialElements>();
                }                                
                
                namingCounter++;

                string elementName = breakablePrefab.tag + "_c" + boardLayout[i].columnX + "_r" + boardLayout[i].rowY + "_" + namingCounter;

                breakableElement.name = elementName;

                //set properties parent
                breakableElement.transform.parent = gameArea.transform;


                // Add to all
                //allTypeDotsCoord[boardLayout[i].x, boardLayout[i].y] = tempPos;
            }
        }
    }

    private void GenerateExpandTiles()
    {
        int namingCounter = 0;

        for (int i = 0; i < boardLayout.Length; i++)
        {

            TileKind kind = boardLayout[i].tileKind;

            if (expandDict.ContainsKey(kind))
            {
                Vector2 tempPos = new Vector2(boardLayout[i].columnX, boardLayout[i].rowY);

                    GameObject expandPrefab = expandDict[kind];

                    GameObject expandingElement = null;

                    if (expandPrefab != null)
                    {
                        expandingElement = Instantiate(expandPrefab, tempPos, Quaternion.identity);
                        expandCells[boardLayout[i].columnX, boardLayout[i].rowY] = expandingElement.GetComponent<SpecialElements>();
                    }

                    namingCounter++;
                    //naming
                    string elementName = expandingElement.tag + "_c" + boardLayout[i].columnX + "_r" + boardLayout[i].rowY + "_" + namingCounter;
                    expandingElement.name = elementName;

                    //set properties parent
                    expandingElement.transform.parent = gameArea.transform;                
            }
        }
    }

    private void GenerateLocked()
    {
        int namingCounter = 0;

        for (int i = 0; i < boardLayout.Length; i++)
        {
            TileKind kind = boardLayout[i].tileKind;

            if (lockersDict.ContainsKey(kind))
            {
                Vector2 tempPos = new Vector2(boardLayout[i].columnX, boardLayout[i].rowY);

                
                GameObject lockedPrefab = lockersDict[kind];

                if(lockedPrefab != null)
                {
                    GameObject lockedElement = Instantiate(lockedPrefab, tempPos, Quaternion.identity);

                    if (lockedElement != null)
                    {
                        lockedCells[boardLayout[i].columnX, boardLayout[i].rowY] = lockedElement.GetComponent<SpecialElements>();
                        Debug.Log("here");
                        namingCounter++;

                        string elementName = lockedPrefab.tag + "_c" + boardLayout[i].columnX + "_r" + boardLayout[i].rowY + "_" + namingCounter;
                        lockedElement.name = elementName;

                        lockedElement.transform.parent = gameArea.transform;
                    }
                }
            }
        }

        
    }


    private void SetUpBoard()
    {
        GenerateEmptyElements();
        GenerateBlockers();
        GenerateBreakable();
        GenerateLocked();
        GenerateExpandTiles();

        //for naming
        int namingCounter = 0;      

        //fill board with elements
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (!emptyElement[i, j] && !blockerCells[i, j] && !expandCells[i,j])
                {
                    //temp position and offset
                    Vector2 elementPosition = new Vector2(i, j);

                    //add elements
                    int elementNumber = UnityEngine.Random.Range(0, elements.Length);

                    int maxItertion = 0;

                    //board without match
                    while (MatchingCheck(i, j, elements[elementNumber]) && maxItertion < 100)
                    {
                        elementNumber = UnityEngine.Random.Range(0, elements.Length);
                        maxItertion++;
                    }

                    //instance element
                    GameObject element = Instantiate(elements[elementNumber], elementPosition, Quaternion.identity);

                    ElementController elementController = element.GetComponent<ElementController>();

                    //set position
                    elementController.column = i;
                    elementController.row = j;
                    
                    //set properties
                    element.transform.parent = gameArea.transform;

                    namingCounter++;

                    //elements naming
                    element.name = element.tag + "_c" + i + "_r" + j + "_" + namingCounter;
                                        
                    //add elements to array
                    allElements[i, j] = element;                    
                }
            }
        }

       
        //bonus cells bombs and etc.
        GenBoosters();

        //if not null gen preload cells
        if (preloadBoardLayout != null)
        {
            GenPreloadLayout();
        }

        //like color bomb
        if (colorBusterInUse || lineBusterInUse)
            SetTimlessBuster();

        matchState = MatchState.matching_stop;
    }

    private void SetTimlessBuster()
    {
        //list for elements
        List<ElementController> validElements = new List<ElementController>();

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (!emptyElement[i, j] && !blockerCells[i, j])
                {
                    foreach (Transform child in gameArea.transform)
                    {
                        // Optionally, check child position or name to match specific cells
                        Vector3 childPosition = child.transform.position;

                        // Example condition: Check if child's position matches (i, j)
                        if (Mathf.RoundToInt(childPosition.x) == i &&
                            Mathf.RoundToInt(childPosition.y) == j)
                        {
                            ElementController elemControl = child.gameObject.GetComponent<ElementController>();

                            if (elemControl != null)
                            {
                                if (elemControl.tag != "no_tag")
                                {
                                    validElements.Add(elemControl);
                                }
                            }
                        }
                    }
                }
            }
        }


        // Randomly select one valid element to change its tag
        if (validElements.Count > 0 && colorBusterInUse)
        {
            int randomIndex = UnityEngine.Random.Range(0, validElements.Count);
            ElementController randomElement = validElements[randomIndex];
            
            randomElement.GenerateColorBomb();

            Debug.Log($"Convert {randomElement.gameObject.name} to ColorBomb");
        }
        else
        {
            Debug.Log("No valid elements found to change tag.");
        }

        //for line
        if (validElements.Count > 0 && lineBusterInUse)
        {
            int randomIndex = UnityEngine.Random.Range(0, validElements.Count);
            ElementController randomElement = validElements[randomIndex];

            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                randomElement.GenerateColumnBomb();
            }
            else
            {
                randomElement.GenerateRowBomb();
            }

            Debug.Log($"Convert {randomElement.gameObject.name} to line bomb");
        }
        else
        {
            Debug.Log("No valid elements found to change tag.");
        }

    }

    //check for matching
    private bool MatchingCheck(int column, int row, GameObject element)
    {
        // Check horizontally for matches
        if (column > 1)
        {
            if (allElements[column - 1, row] != null && allElements[column - 2, row] != null)
            {
                if (allElements[column - 1, row].tag == element.tag && allElements[column - 2, row].tag == element.tag)
                {
                    return true;
                }
            }
        }

        // Check vertically for matches
        if (row > 1)
        {
            if (allElements[column, row - 1] != null && allElements[column, row - 2] != null)
            {
                if (allElements[column, row - 1].tag == element.tag && allElements[column, row - 2].tag == element.tag)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CongratInfo(int matchCount)
    {
        if (matchCount > 20)
        {
            uiManagerClass.ShowInGameInfo("Great!", true, 3, ColorPalette.Colors["VioletMed"]); //show panel with text           
        }                    
    }

    //step 9     
    public void DestroyMatches()
    {
        //condition
        bool condition = false;
        
        //remove doubles
        matchFinderClass.currentMatch = GameObjectUtils.RemoveDuplicatesByName(matchFinderClass.currentMatch);

        CongratInfo(matchFinderClass.currentMatch.Count);

        //bomb gen - part 1 based on slide Awoid generate bomb on mass destruction.
        if (matchFinderClass.currentMatch.Count >= minMatchForBomb && matchFinderClass.currentMatch.Count < matchLimit)
        {
            CheckToGenerateBombs();            
        }

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null)
                {                    
                    DestroyMatchesAt(i, j);
                }
            }

            condition = true;
        }

        // here start refill
        if (condition)
            StartCoroutine(DecreaseRowCo());
    }

    //Important!
    private IEnumerator DecreaseRowCo()
    {
        //condition
        bool condition = false;

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] == null && !emptyElement[i, j] && !blockerCells[i, j] && !expandCells[i, j])
                {
                    for (int k = j + 1; k < row; k++)
                    {
                        if (allElements[i, k] != null)
                        {
                            // Move dot to the new position
                            allElements[i, k].GetComponent<ElementController>().row = j;
                            allElements[i, j] = allElements[i, k]; // Move reference to the new position
                            allElements[i, k] = null; // Clear the old position                                                    
                            break;
                        }
                    }
                }
            }

            condition = true;
        }

        yield return new WaitForSeconds(0.4f);
       
        //step 2 refill
        if (condition)
            StartCoroutine(FillBoardCo());
    }

    private void RunParticles(ElementController element, int thisCol, int thisRow)
    {
        Vector3 elementPosition = allElements[thisCol, thisRow].transform.position;

        if (element.isColumnBomb || element.isRowBomb)
        {
            Quaternion rotation = element.isRowBomb ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
            Vector3 particlePosition = elementPosition;
            fxManagerClass.InstantiateAndConfigureParticle(element, particlePosition, rotation);

            // Handle combo particles
            if (element.isCombo)
            {
                if (element.comboE1 != -1)
                {
                    particlePosition = UpdatePosition(particlePosition, element.isRowBomb, element.comboE1);
                    fxManagerClass.InstantiateAndConfigureParticle(element, particlePosition, rotation);
                }

                if (element.comboE2 != -1)
                {
                    particlePosition = UpdatePosition(particlePosition, element.isRowBomb, element.comboE2);
                    fxManagerClass.InstantiateAndConfigureParticle(element, particlePosition, rotation);
                }
            }
        }

        // Helper to instantiate and configure a particle
/*        void InstantiateAndConfigureParticle(ElementController element, Vector3 position, Quaternion rotation)
        {
            GameObject particle = Instantiate(element.lineBombParticle, position, rotation);
            SpriteMask spriteMask = particle.GetComponentInChildren<SpriteMask>();
            if (spriteMask != null)
                SetSpriteMaskToScreenCenter(spriteMask, rotation == Quaternion.identity ? 0 : 90);
            Destroy(particle, 1.9f);
        }*/

        // Helper to update position based on bomb type
        Vector3 UpdatePosition(Vector3 originalPosition, bool isRowBomb, int comboValue)
        {
            if (isRowBomb)
                originalPosition.y = comboValue;
            else
                originalPosition.x = comboValue;
            return originalPosition;
        }

        //wrap part
        if (element.isWrapBomb)
        {
            GameObject elementParticle = Instantiate(element.wrapBombParticle, elementPosition, Quaternion.identity);
            elementParticle.name = "wrap_part_" + "_" + thisCol + "_" + thisRow;
            elementParticle.transform.parent = gameArea.transform;
            
            if (element.isCombo)
            {            
                Transform wrapBombComboPart = elementParticle.transform.Find("wrapbomb_combo_part");
                wrapBombComboPart.gameObject.SetActive(true);
            }
            
            Destroy(elementParticle, 1.9f);
        }

        //color part
        if (element.isColorBomb)
        {
            GameObject elementParticle = Instantiate(element.colorBombParticle, elementPosition, Quaternion.identity);
            elementParticle.name = "color_part_" + "_" + thisCol + "_" + thisRow;
            elementParticle.transform.parent = gameArea.transform;
            Destroy(elementParticle, 1.9f);
        }

        //std element
        if (element.destroyParticle != null)
        {
            GameObject elementParticle = Instantiate(element.destroyParticle, elementPosition, Quaternion.identity);
            elementParticle.name = "part_" + element.name +"_"+ thisCol + "_" + thisRow;
            elementParticle.transform.parent = gameArea.transform;
            Destroy(elementParticle, .9f);
        }
    }

/*    void SetSpriteMaskToScreenCenter(SpriteMask spriteMask, int angle = -1)
    {
        float yOffset = 1; //see public float yOffset = 1; in CameraManager

        // Get the screen center in world space
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 worldCenter = Camera.main.ScreenToWorldPoint(screenCenter);

        // Set the Sprite Mask position (ensure the correct z-axis value)
        worldCenter.z = 0f; // Adjust this depending on your scene setup
        worldCenter.y -= yOffset;
        spriteMask.transform.position = worldCenter;

        spriteMask.transform.localScale = new Vector3(column, row, 0);

        //rotate for horizontal
        if (angle > 0)
            spriteMask.transform.eulerAngles += new Vector3(0, 0, angle);
    }*/


    private void DestroyBreakableAt(int thisColumn, int thisRow)
    {
        if (breakableCells[thisColumn, thisRow] != null)
        {
            SpecialElements currentBreak = breakableCells[thisColumn, thisRow];
            currentBreak.TakeDamage(1); // Apply damage

            int hitPoints = currentBreak.hitPoints;

            if (hitPoints >= 0 && hitPoints < currentBreak.elementSounds.Length)
            {
                // Play sound if available
                if (currentBreak.elementSounds[hitPoints] != null)
                {
                    soundManagerClass.PlaySound(currentBreak.elementSounds[hitPoints]);
                }

                // Run particles if available
                if (currentBreak.elementParticles[hitPoints] != null)
                {
                    GameObject particle = Instantiate(
                        currentBreak.elementParticles[hitPoints],
                        breakableCells[thisColumn, thisRow].transform.position,
                        Quaternion.identity
                    );
                    Destroy(particle, 2.0f); // Particle delay
                }
            }

            // Destroy cell if hit points are 0 or less
            if (hitPoints <= 0)
            {
                breakableCells[thisColumn, thisRow] = null;
            }
        }
    }

    // step 10  
    private void DestroyMatchesAt(int thisColumn, int thisRow)
    {        

        if (allElements[thisColumn, thisRow].GetComponent<ElementController>().isMatched)
        {
            ElementController currentElement = allElements[thisColumn, thisRow].GetComponent<ElementController>();

            //destroy breakable
            DestroyBreakableAt(thisColumn, thisRow);

            //goal for dots
            if (goalManagerClass != null)
            {
                if (currentElement.isRowBomb || currentElement.isColumnBomb)
                {
                    goalManagerClass.CompareGoal("LineBomb", thisColumn, thisRow); //for line bombs
                }
                else if (currentElement.isWrapBomb)
                {
                    goalManagerClass.CompareGoal("WrapBomb", thisColumn, thisRow); //for Wrap bombs                    
                }
                else
                {
                    goalManagerClass.CompareGoal(allElements[thisColumn, thisRow].tag.ToString(), thisColumn, thisRow); //for usual dots
                }

                goalManagerClass.UpdateGoals();
            }

            DamageLockers(thisColumn, thisRow);

            //for blockers
            DamageBlockers(thisColumn, thisRow);      
            
            DamageExpandable(thisColumn, thisRow);  

            //sound
            if (currentElement.elementSound != null)
            {
                soundManagerClass.PlaySound(currentElement.elementSound);
            }

            //particles
            if (currentElement != null)
            {
                RunParticles(currentElement, thisColumn, thisRow);
            }

            scoreManagerClass.IncreaseScore(baseValue); //score

            //remove bombs
            if (bombsCells[thisColumn, thisRow] != null)
            {
                bombsCells[thisColumn, thisRow] = null;
            }

            currentElement.DestroyAnimation();

            //main destroy
            Destroy(allElements[thisColumn, thisRow], .4f); //!Important
            allElements[thisColumn, thisRow] = null;

            //clear match list
            matchFinderClass.currentMatch.Clear();

            //for colorbomb
            if(fxManagerClass.createdLines.Count > 0)
                StartCoroutine(fxManagerClass.DeleteColorBombLines(.3f));
        }       
    }

    private void RefillBoard()
    {
        int counter = 0;
        string currentTime = DateTime.Now.ToString("ssfff");

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] == null && !emptyElement[i, j] && !blockerCells[i, j] && !expandCells[i, j])
                {
                    Vector2 tempPosition = new Vector2(i, j);
                    int refilledElementNumber = UnityEngine.Random.Range(0, elements.Length);
                    int maxIteration = 0;

                    while (MatchingCheck(i, j, elements[refilledElementNumber]) && maxIteration < 100)
                    {
                        refilledElementNumber = UnityEngine.Random.Range(0, elements.Length);
                        maxIteration++;
                    }

                    GameObject element = Instantiate(elements[refilledElementNumber], tempPosition, Quaternion.identity);
                    allElements[i, j] = element;

                    //set properties
                    element.transform.parent = gameArea.transform;

                    // Set dot properties
                    ElementController refiledElement = element.GetComponent<ElementController>();
                    refiledElement.row = j;
                    refiledElement.column = i;

                    counter++;

                    //element.name = $"{element.tag}_{currentTime}_{counter}";
                    element.name = element.tag + "_c" + i + "_r" + j + "_" + currentTime +"_" + counter;                    
                }
            }
        }
        
        matchState = MatchState.matching_inprogress;

        matchFinderClass.FindAllMatches(); //find match 2
    }

    private bool MatchesOnBoard()
    {
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null)
                {
                    if (allElements[i, j].GetComponent<ElementController>().isMatched)
                    {                        
                        return true;
                    }
                }
            }
        }

        return false;
    }

    //refill final step
    private IEnumerator FillBoardCo()
    {
        //need to avoid bugs
        yield return new WaitForSeconds(refillDelay);

        RefillBoard(); //refil board

        yield return new WaitForSeconds(refillDelay);

        while (MatchesOnBoard())
        {
            streakValue++; //for score                                 
            matchState = MatchState.matching_inprogress;
            DestroyMatches();     //run decrease columns       
            yield break;
        }

        currentElement = null;

        CheckToMakeExpandable();

        if (IsDeadLock())
        {            
            ShuffleBoard();
            uiManagerClass.ShowInGameInfo("Mixed up", true, 0, ColorPalette.Colors["DarkBlue"]); //show panel with text
        }

        if (currentState != GameState.pause)
            currentState = GameState.move;

        makeExpand = true;

        //stop matching
        matchState = MatchState.matching_stop;

        goalManagerClass.UpdateGoals();

        //run booster generator
        int movesMade = initialMoves - endGameManagerClass.curCounterVal; // Moves used so far

        if (movesMade % boosterValue == 0) // Run every 10 moves
        {
            if (colorBusterInUse || lineBusterInUse)
            {
                SetTimlessBuster();
            }
        }
    }


    //gen bombs part 3
    private MatchType ColumnOrRow()
    {
        // Copy of the current match remove double
        List<GameObject> matchCopy = new List<GameObject>(matchFinderClass.currentMatch);

        matchTypeClass.type = 0;
        matchTypeClass.color = "";
        matchTypeClass.curElem = null;

        // Iterate through each dot in the match
        foreach (GameObject matchObject in matchCopy)
        {
            if (matchObject != null)
            {
                ElementController thisDot = matchObject.GetComponent<ElementController>();


                string color = matchObject.tag;  // Get the color from the tag

                int column = thisDot.column;
                int row = thisDot.row;

                int columnMatch = 0;
                int rowMatch = 0;

                // Compare with other dots in the match
                foreach (GameObject otherMatchObject in matchCopy)
                {
                    if (otherMatchObject != null)
                    {
                        ElementController nextDot = otherMatchObject.GetComponent<ElementController>();

                        if (otherMatchObject == matchObject)
                        {
                            continue;
                        }

                        if (nextDot.column == column && nextDot.CompareTag(color))
                        {
                            columnMatch++;
                        }

                        if (nextDot.row == row && nextDot.CompareTag(color))
                        {
                            rowMatch++;
                        }
                    }
                }

                // Check for the type of match
                if (columnMatch == matchForLineBomb || rowMatch == matchForLineBomb)
                {
                    matchTypeClass.type = 1;
                    matchTypeClass.color = color;
                    matchTypeClass.curElem = matchObject;
                    return matchTypeClass;
                }
                else if (columnMatch == matchForWrapBomb && rowMatch == matchForWrapBomb)
                {
                    matchTypeClass.type = 2;
                    matchTypeClass.color = color;
                    matchTypeClass.curElem = matchObject;
                    return matchTypeClass;
                }
                else if (columnMatch == matchForColorBomb || rowMatch == matchForColorBomb)
                {
                    matchTypeClass.type = 3;
                    matchTypeClass.color = color;
                    matchTypeClass.curElem = matchObject;
                    return matchTypeClass;
                }
            }
        }

        // If no match type found, return default
        matchTypeClass.type = 0;
        matchTypeClass.color = "";
        matchTypeClass.curElem = null;
        return matchTypeClass;
    }



    //gen bomb part 2
    public void CheckToGenerateBombs()
    {       
        if (matchFinderClass.currentMatch.Count > minMatchCount && matchFinderClass.currentMatch.Count < matchLimit)
        {
            // Determine match type
            MatchType typeOfMatch = ColumnOrRow();

            //for auto
            if (currentElement == null && typeOfMatch.type != 0)
            {
                currentElement = typeOfMatch.curElem.GetComponent<ElementController>();
                currentElement.otherElement = null;
            }

            if (currentElement != null)
            {
                bool currentDotMatched = currentElement.isMatched && currentElement.tag == typeOfMatch.color;
                ElementController otherDot = currentElement.otherElement != null ? currentElement.otherElement.GetComponent<ElementController>() : null;
                bool otherDotMatched = otherDot != null && otherDot.isMatched && otherDot.tag == typeOfMatch.color;

                switch (typeOfMatch.type)
                {
                    case 1:
                        // Color bomb
                        if (currentDotMatched)
                        {
                            currentElement.isMatched = false;
                            currentElement.GenerateColorBomb();
                        }
                        else if (otherDotMatched)
                        {
                            otherDot.isMatched = false;
                            otherDot.GenerateColorBomb();
                        }
                        break;
                    case 2:
                        // Wrap bomb
                        if (currentDotMatched)
                        {
                            currentElement.isMatched = false;
                            currentElement.GenerateWrapBomb();                            
                        }
                        else if (otherDotMatched)
                        {
                            otherDot.isMatched = false;
                            otherDot.GenerateWrapBomb();
                        }
                        break;
                    case 3:
                        // Column/Row bomb
                        matchFinderClass.LineBombCheck(typeOfMatch);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private void DamageLockers(int thisColumn, int thisRow)
    {
        //lockers
        if (lockedCells[thisColumn, thisRow] != null)
        {
            lockedCells[thisColumn, thisRow].TakeDamage(1);

            int hitPoint = lockedCells[thisColumn, thisRow].hitPoints;

            Debug.Log("hitPoint: " + hitPoint);

            SpecialElements currentLocker = lockedCells[thisColumn, thisRow];
          
            if (hitPoint <= 0 || hitPoint <= currentLocker.elementSounds.Length)
            {

                int index = Mathf.Clamp(hitPoint, 0, currentLocker.elementSounds.Length - 1);

                //sound
                if (currentLocker.elementSounds[index] != null)
                {
                    soundManagerClass.PlaySound(currentLocker.elementSounds[index]);
                }

                //particles
                if (currentLocker.elementParticles[index] != null)
                {
                    GameObject lockerParticle = Instantiate(
                        currentLocker.elementParticles[index],
                        lockedCells[thisColumn, thisRow].transform.position,
                        Quaternion.identity
                    );

                    lockerParticle.name = "locker_part_" + thisColumn + "_" + thisRow + "_" + index;
                    lockerParticle.transform.parent = gameArea.transform;
                    //Debug.Break();
                    Destroy(lockerParticle, 2.9f); // Particle delay
                }

                if (lockedCells[thisColumn, thisRow].hitPoints <= 0)
                {
                    lockedCells[thisColumn, thisRow] = null;
                }
                
            }
        }
    }


    //blockers
    private void DamageBlockers(int column, int row)
    {
        DamageBlockerAt(column - 1, row);
        DamageBlockerAt(column + 1, row);
        DamageBlockerAt(column, row - 1);
        DamageBlockerAt(column, row + 1);
    }

    //blockers
    public void DamageBlockerAt(int thisColumn, int thisRow)
    {
        // Check if the position is within bounds
        if (thisColumn >= 0 && thisColumn < column && thisRow >= 0 && thisRow < row)
        {
            // Check if there is a blocker at the position
            if (blockerCells[thisColumn, thisRow])
            {
                // Apply damage
                blockerCells[thisColumn, thisRow].TakeDamage(1);

                // Log the current blocker
                SpecialElements currentBlocker = blockerCells[thisColumn, thisRow];

                //get hit points
                int hitPoint = blockerCells[thisColumn, thisRow].hitPoints;

                // Effects queue
                if (hitPoint <= 0 || hitPoint <= currentBlocker.elementSounds.Length)
                {
                    int index = Mathf.Clamp(hitPoint, 0, currentBlocker.elementSounds.Length - 1);

                    if (currentBlocker.elementSounds[index] != null)
                    {
                        soundManagerClass.PlaySound(currentBlocker.elementSounds[index]);
                    }

                    //particles
                    if (currentBlocker.elementParticles[index] != null)
                    {
                        GameObject blockerParticle = Instantiate(
                            currentBlocker.elementParticles[index],
                            blockerCells[thisColumn, thisRow].transform.position,
                            Quaternion.identity
                        );

                        blockerParticle.name = "blocker_part_" + thisColumn + "_" + thisRow + "_"+ index;
                        blockerParticle.transform.parent = gameArea.transform;

                        Destroy(blockerParticle, 1.9f); // Particle delay
                    }

                    // Remove the blocker if hit points are 0 or less
                    if (hitPoint <= 0)
                    {
                        blockerCells[thisColumn, thisRow] = null;
                    }
                }

            }
        }
    }

    //for bomb and blockers
    public void BombRow(int row)
    {
        for (int i = 0; i < column; i++)
        {
            if (blockerCells[i, row])
            {
                blockerCells[i, row].TakeDamage(1);

                if (blockerCells[i, row].hitPoints <= 0)
                {
                    blockerCells[i, row] = null;
                }
            }
        }
    }
    
    //for bomb and blockers
    public void BombColumn(int column)
    {
        for (int i = 0; i < row; i++)
        {
            if (blockerCells[column, i])
            {
                blockerCells[column, i].TakeDamage(1);

                if (blockerCells[column, i].hitPoints <= 0)
                {
                    blockerCells[column, i] = null;
                }
            }
        }
    }

    //bobms and etc
    private void GenBoosters()
    {
        foreach (var layout in boardLayout)
        {
            int column = layout.columnX;
            int row = layout.rowY;

            // Get current dot
            GameObject currentDot = allElements[column, row];

            if (currentDot != null)
            {
                // Get dot component
                ElementController curDotGet = currentDot.GetComponent<ElementController>();

                if (curDotGet != null)
                {
                    switch (layout.tileKind)
                    {
                        case TileKind.ColorBomb:
                            curDotGet.GenerateColorBomb();
                            curDotGet.isColorBomb = true;
                            curDotGet.name = "ColrB_" + curDotGet.name;
                            bombsCells[curDotGet.column, curDotGet.row] = curDotGet;                            
                            break;

                        case TileKind.WrapBomb:
                            curDotGet.GenerateWrapBomb();
                            curDotGet.isWrapBomb = true;
                            curDotGet.tag = "no_tag"; //not tag
                            curDotGet.name = "WrapB_" + curDotGet.name;
                            bombsCells[curDotGet.column, curDotGet.row] = curDotGet;
                            break;

                        case TileKind.RowBomb:
                            curDotGet.GenerateRowBomb();
                            curDotGet.isRowBomb = true;
                            curDotGet.tag = "no_tag"; //not tag
                            curDotGet.name = "RowB_" + curDotGet.name;
                            bombsCells[curDotGet.column, curDotGet.row] = curDotGet;
                            break;

                        case TileKind.ColumnBomb:
                            curDotGet.GenerateColumnBomb();
                            curDotGet.isColumnBomb = true;
                            curDotGet.tag = "no_tag"; //not tag
                            curDotGet.name = "ColmB_" + curDotGet.name;
                            bombsCells[curDotGet.column, curDotGet.row] = curDotGet;
                            break;
                    }
                }
            }
        }
    }

    public void GenPreloadLayout()
    {
        for (int i = 0; i < preloadBoardLayout.Length; i++)
        {
            TileKind kind = preloadBoardLayout[i].tileKind;

            if (preloadDict.ContainsKey(kind))
            {
                Vector2 tempPos = new Vector2(preloadBoardLayout[i].columnX, preloadBoardLayout[i].rowY);
                int valueX = preloadBoardLayout[i].columnX;
                int valueY = preloadBoardLayout[i].rowY;

                // Delete old random elements
                Destroy(allElements[valueX, valueY].gameObject);

                // Create preload
                GameObject preloadElements = Instantiate(elements[preloadDict[kind]], tempPos, Quaternion.identity);
                
                //set properties
                preloadElements.transform.parent = gameArea.transform;

                // Set position
                ElementController elemnt = preloadElements.GetComponent<ElementController>();
                elemnt.column = valueX;
                elemnt.row = valueY;

                //preload dots naming
                preloadElements.name = preloadElements.tag + "_c" + valueX + "_r" + valueY + "_" + "_pre" + i;

                // Add to dots
                allElements[valueX, valueY] = preloadElements;
            }
        }
    }

    private bool CheckForMatches()
    {
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null)
                {
                    if (i < column - 2)
                    {
                        if (allElements[i + 1, j] != null && allElements[i + 2, j] != null)
                        {
                            if (allElements[i + 1, j].tag == allElements[i, j].tag && allElements[i + 2, j].tag == allElements[i, j].tag)
                            {
                                return true;
                            }
                        }
                    }

                    if (j < row - 2)
                    {
                        if (allElements[i, j + 1] != null && allElements[i, j + 2] != null)
                        {
                            if (allElements[i, j + 1].tag == allElements[i, j].tag && allElements[i, j + 2].tag == allElements[i, j].tag)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private void SwitchPieces(int column, int row, Vector2 direction)
    {
        if (allElements[column + (int)direction.x, row + (int)direction.y] != null)
        {
            GameObject holder = allElements[column + (int)direction.x, row + (int)direction.y] as GameObject;

            allElements[column + (int)direction.x, row + (int)direction.y] = allElements[column, row];

            allElements[column, row] = holder;
        }
    }

    public bool SwithAndCheck(int column, int row, Vector2 direction)
    {
        SwitchPieces(column, row, direction);

        if (CheckForMatches())
        {
            SwitchPieces(column, row, direction);
            return true;
        }

        SwitchPieces(column, row, direction);
        return false;
    }

    private bool IsDeadLock()
    {
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null)
                {
                    if (i < column - 1)
                    {
                        if (SwithAndCheck(i, j, Vector2.right))
                        {
                            return false;
                        }
                    }

                    if (j < row - 1)
                    {
                        if (SwithAndCheck(i, j, Vector2.up))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    public void ShuffleBoard()
    {
        //for game obj
        List<GameObject> newBoard = new List<GameObject>();

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null && !bombsCells[i, j])
                {
                    newBoard.Add(allElements[i, j]);
                }
            }
        }


        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (!emptyElement[i, j] && !blockerCells[i, j] && !bombsCells[i, j] && !expandCells[i, j]) //list of not
                {
                    int cellToUse = UnityEngine.Random.Range(0, newBoard.Count);

                    int maxItertion = 0;

                    //board without match
                    while (MatchingCheck(i, j, newBoard[cellToUse]) && maxItertion < 100)
                    {
                        cellToUse = UnityEngine.Random.Range(0, newBoard.Count);
                        maxItertion++;
                    }

                    //container
                    ElementController element = newBoard[cellToUse].GetComponent<ElementController>();

                    //assign col
                    element.column = i;

                    //assig row
                    element.row = j;

                    allElements[i, j] = newBoard[cellToUse];

                    newBoard.Remove(newBoard[cellToUse]);
                }
            }
        }

        if (IsDeadLock())
        {
            ShuffleBoard();            
            uiManagerClass.ShowInGameInfo("Mixed up", true, 0, ColorPalette.Colors["DarkBlue"]); //show panel with text
        }
    }

    private void DamageAdjacentExpand(int column, int row)
    {
        if (expandCells[column, row])
        {
            expandCells[column, row].TakeDamage(1);

            int hitPoint = expandCells[column, row].hitPoints;

            SpecialElements currentExpandable = expandCells[column, row];

            // Effects queue
            if (hitPoint <= 0 || hitPoint <= currentExpandable.elementSounds.Length)
            {
                int index = Mathf.Clamp(hitPoint, 0, currentExpandable.elementSounds.Length - 1);

                if (currentExpandable.elementSounds[index] != null)
                {
                    soundManagerClass.PlaySound(currentExpandable.elementSounds[index]);
                }

                //particles
                if (currentExpandable.elementParticles[index] != null)
                {
                    GameObject expandParticle = Instantiate(
                        currentExpandable.elementParticles[index],
                        expandCells[column, row].transform.position,
                        Quaternion.identity
                    );

                    expandParticle.name = "expand_part_" + column + "_" + row + "_" + index;
                    expandParticle.transform.parent = gameArea.transform;

                    Destroy(expandParticle, 1.9f); // Particle delay
                }
            }

            if (expandCells[column, row].hitPoints <= 0)
            {
                expandCells[column, row] = null;
            }

            makeExpand = false;
        }    
    }


    private void DamageExpandable(int thisColumn, int thisRow)
    {
        if (thisColumn > 0)
        {
            DamageAdjacentExpand(thisColumn - 1, thisRow);
        }

        if (thisColumn < column - 1)
        {
            DamageAdjacentExpand(thisColumn + 1, thisRow);
        }

        if (thisRow > 0)
        {
            DamageAdjacentExpand(thisColumn, thisRow - 1);
        }

        if (thisRow < row - 1)
        {
            DamageAdjacentExpand(thisColumn, thisRow + 1);
        }
    }

    private void CheckToMakeExpandable()
    {
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (expandCells[i, j] && makeExpand)
                {
                    BirthExpandable();
                    return;
                }
            }
        }
    }

    private Vector2 CheckForExpand(int thisColumn, int thisRow)
    {

        if (thisColumn < column - 1 && allElements[thisColumn + 1, thisRow])
        {
            return Vector2.right;
        }

        if (thisColumn > 0 && allElements[thisColumn - 1, thisRow])
        {
            return Vector2.left;
        }


        if (thisRow < row - 1 && allElements[thisColumn, thisRow + 1])
        {
            return Vector2.up;
        }


        if (thisRow > 0 && allElements[thisColumn, thisRow - 1])
        {
            return Vector2.down;
        }

        return Vector2.zero;
    }

    private void BirthExpandable()
    {
        bool slimeBorn = false;
        int loops = 0;
        const int maxLoops = 200;

        while (!slimeBorn && loops < maxLoops)
        {
            int newX = UnityEngine.Random.Range(0, column);
            int newY = UnityEngine.Random.Range(0, row);

            if (expandCells[newX, newY])
            {
                Vector2 adj = CheckForExpand(newX, newY);

                if (adj != Vector2.zero)
                {
                    int adjX = newX + (int)adj.x;
                    int adjY = newY + (int)adj.y;

                    Destroy(allElements[adjX, adjY]);
                    Vector2 tempPos = new Vector2(adjX, adjY);

                    // Add slime
                    GameObject expandingElement = Instantiate(expand01Prefab, tempPos, Quaternion.identity);
                    expandCells[adjX, adjY] = expandingElement.GetComponent<SpecialElements>();

                    int randomValue = UnityEngine.Random.Range(1, 100);
                    //naming
                    string elementName = expandingElement.tag + "_c" + adjX + "_r" + adjY + "_" + randomValue + "_new";
                    expandingElement.name = elementName;

                    //set properties parent
                    expandingElement.transform.parent = gameArea.transform;

                    slimeBorn = true;
                }
            }

            loops++;
        }
    }


    private IEnumerator UpdatePerSec()
    {
        while (true)
        {
            //for busters
            if (gameDataClass != null)
            {
                colorBusterInUse = gameDataClass.saveData.colorBusterRecoveryTime != "";
                lineBusterInUse = gameDataClass.saveData.lineBusterRecoveryTime != "";
            }

            yield return new WaitForSeconds(1f); // Wait for 1 second
        }        
    }

}
