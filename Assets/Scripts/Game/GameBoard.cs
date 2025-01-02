using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using System.Xml.Linq;
using TMPro;
using UnityEngine.UIElements;


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
    Breakable02
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

    //for blank
    private bool[,] emptyElement;

    [Header("Prefabs")]
    //public GameObject elementPrefab;
    public GameObject break01Prefab;
    public GameObject break02Prefab;
    public GameObject blocker01Prefab;
    public GameObject blocker02Prefab;
    //public GameObject expand01Prefab;
    //public GameObject locker01Prefab;

    //for lock
    public SpecialElements[,] lockedCells;

    //for blockers
    public SpecialElements[,] blockerCells;

    //for breakables
    public SpecialElements[,] breakableCells;

    //for bombs
    public ElementController[,] bombsCells;

    //bombs values    
    private int minMatchCount = 3;
    public int minMatchForBomb = 4;
    private int matchLimit = 81; //awoid match bomb bugs

    private int matchForLineBomb = 4;
    private int matchForWrapBomb = 2;
    private int matchForColorBomb = 3;

    //dict
    private Dictionary<TileKind, int> preloadDict;
    private Dictionary<TileKind, GameObject> breacableDict;
    private Dictionary<TileKind, GameObject> blockersDict;

    [Header("ColorBomb Staff")]
    //for colorbomb
    public List<GameObject> createdLines = new List<GameObject>();

    //add rainbow
    private List<Color> rayColors = new List<Color>()
    {
        Color.red,              // Red
        new Color(1f, 0.647f, 0f), // Orange
        Color.yellow,           // Yellow
        Color.green,            // Green
        Color.blue,             // Blue
        new Color(0.294f, 0f, 0.51f), // Indigo
        new Color(0.56f, 0f, 1f) // Violet
    };

    public Material colorBombRayMat;

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

                    gameBoardBack = worldClass.levels[level].elementsBack; //back

                    boardLayout = worldClass.levels[level].boardLayout;

                    preloadBoardLayout = worldClass.levels[level].preloadBoardLayout;
                }
            }
        }

        //for blockers
        blockersDict = new Dictionary<TileKind, GameObject>
        {
            { TileKind.Blocker01, blocker01Prefab },
            { TileKind.Blocker02, blocker02Prefab }
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
            { TileKind.Breakable02, break02Prefab }
        };

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

        //all dots on board
        allElements = new GameObject[column, row];

        //init type of objects
        blockerCells = new SpecialElements[column, row];
        lockedCells = new SpecialElements[column, row];
        breakableCells = new SpecialElements[column, row];

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

        //load back sprite
        elementsBackGO.GetComponent<SpriteRenderer>().sprite = gameBoardBack.gameBoardBackSprite;

        if(soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(levelMusic);
        }

        //stop 1
        matchState = MatchState.matching_stop;

        levelNumberTxt.text = "Level " + (level + 1);

        createdLines.Clear();
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

                GameObject breakableElement = Instantiate(breakablePrefab, tempPos, Quaternion.identity);

                breakableCells[boardLayout[i].columnX, boardLayout[i].rowY] = breakableElement.GetComponent<SpecialElements>();

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


    private void SetUpBoard()
    {
        GenerateEmptyElements();
        GenerateBlockers();
        GenerateBreakable();

        //for naming
        int namingCounter = 0;

        //fill board with elements
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (!emptyElement[i, j] && !blockerCells[i, j])
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
        GenBonusCells();

        //if not null gen preload cells
        if (preloadBoardLayout != null)
        {
            GenPreloadLayout();
        }

        matchState = MatchState.matching_stop;
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
            uiManagerClass.ShowInGameInfo("Great!", true, ColorPalette.Colors["VioletMed"]); //show panel with text

        Debug.Log("matchCount: " + matchCount);
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
                if (allElements[i, j] == null && !emptyElement[i, j] && !blockerCells[i, j])
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
            InstantiateAndConfigureParticle(element, particlePosition, rotation);

            // Handle combo particles
            if (element.isCombo)
            {
                if (element.comboE1 != -1)
                {
                    particlePosition = UpdatePosition(particlePosition, element.isRowBomb, element.comboE1);
                    InstantiateAndConfigureParticle(element, particlePosition, rotation);
                }

                if (element.comboE2 != -1)
                {
                    particlePosition = UpdatePosition(particlePosition, element.isRowBomb, element.comboE2);
                    InstantiateAndConfigureParticle(element, particlePosition, rotation);
                }
            }
        }

        // Helper to instantiate and configure a particle
        void InstantiateAndConfigureParticle(ElementController element, Vector3 position, Quaternion rotation)
        {
            GameObject particle = Instantiate(element.lineBombParticle, position, rotation);
            SpriteMask spriteMask = particle.GetComponentInChildren<SpriteMask>();
            if (spriteMask != null)
                SetSpriteMaskToScreenCenter(spriteMask, rotation == Quaternion.identity ? 0 : 90);
            Destroy(particle, 1.9f);
        }

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

    void SetSpriteMaskToScreenCenter(SpriteMask spriteMask, int angle = -1)
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
    }


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

            

            //for blockers
            DamageBlockers(thisColumn, thisRow);            

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
            if(createdLines.Count > 0)
                StartCoroutine(DeleteColorBombLines(.3f));
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
                if (allElements[i, j] == null && !emptyElement[i, j] && !blockerCells[i, j])
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

        if (IsDeadLock())
        {            
            ShuffleBoard();
            uiManagerClass.ShowInGameInfo("Mixed up", true, ColorPalette.Colors["DarkBlue"]); //show panel with text
        }

        if (currentState != GameState.pause)
            currentState = GameState.move;

        //stop matching
        matchState = MatchState.matching_stop;

        goalManagerClass.UpdateGoals();
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
    private void GenBonusCells()
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
                if (!emptyElement[i, j] && !blockerCells[i, j] && !bombsCells[i, j]) //list of not
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
            uiManagerClass.ShowInGameInfo("Mixed up", true, ColorPalette.Colors["DarkBlue"]); //show panel with text
        }
    }

    // Coroutine to fade out and delete color bomb lines after a delay
    private IEnumerator DeleteColorBombLines(float delay)
    {
        // Wait for the specified delay before starting the fade-out process
        yield return new WaitForSeconds(delay);

        // Fade out each line and then destroy it
        foreach (GameObject line in createdLines)
        {
            if (line != null)
            {
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // Start fading out the line before destroying it
                    StartCoroutine(FadeAndDestroyLine(lineRenderer, line));
                }
            }
        }

        // Clear the list after deletion (all lines will be destroyed by now)
        createdLines.Clear();
    }

    public void CreateColorBombLines(Vector2 startPoint, Vector2 endPoint, Color color, float width)
    {
        // Create a new GameObject for the LineRenderer
        GameObject lineObject = new GameObject("DynamicLine");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.name = "colorbomb_line_" + endPoint.x + "_" + endPoint.y;

        // Set the material
        lineRenderer.material = colorBombRayMat;

        //width = 0.5f;

        // Divide the line into 8 segments
        int segments = 12;
        lineRenderer.positionCount = segments + 1; // 9 points for 8 segments      

        // Convert Vector2 to Vector3 (z = 0 for 2D lines)
        //Vector3 startPoint3D = new Vector3(startPoint.x, startPoint.y, 0f);
        //Vector3 endPoint3D = new Vector3(endPoint.x, endPoint.y, 0f);

        // Calculate the total distance between start and end points
        float lineLength = Vector2.Distance(startPoint, endPoint);

        //Debug.Log(lineRenderer.name + "=" + lineLength);

        // Set the positions for the start and end points
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(segments, endPoint);

        // Determine the maxShift based on the line length
        ///float maxShift = (lineLength < 2f) ? 0.01f : 0.2f; // Use 0.1f for short lines, 0.5f for long lines

        if (lineLength < 1.5f)
        {
            // For short lines, set only start and end points
            lineRenderer.positionCount = 2; // Only 2 points for start and end
            lineRenderer.SetPosition(0, new Vector3(startPoint.x, startPoint.y, 0f));
            lineRenderer.SetPosition(1, new Vector3(endPoint.x, endPoint.y, 0f));
        }
        else
        {
            // For longer lines, divide into segments and apply sawtooth pattern
            //int segments = 16;
            lineRenderer.positionCount = segments + 1; // 17 points for 16 segments

            // Convert Vector2 to Vector3 (z = 0 for 2D lines)
            Vector3 startPoint3D = new Vector3(startPoint.x, startPoint.y, 0f);
            Vector3 endPoint3D = new Vector3(endPoint.x, endPoint.y, 0f);

            // Calculate the normalized direction vector
            Vector3 direction = (endPoint3D - startPoint3D).normalized;

            // Calculate and apply the sawtooth pattern
            float segmentLength = lineLength / segments; // Uniform length for each segment
            float maxShift = (Mathf.Abs(endPoint.x - startPoint.x) > 2f && Mathf.Abs(endPoint.y - startPoint.y) > 2f) ? 0.4f : 0.2f; // Larger shift for longer diagonals

            for (int i = 0; i <= segments; i++) // Include both endpoints
            {
                Vector3 point = startPoint3D + direction * (i * segmentLength);

                // Calculate shift factor that decreases as we get closer to the end point
                float t = (float)i / segments; // Interpolation value between 0 and 1
                float shiftFactor = maxShift * (1 - t); // Gradually decrease shift as we approach the end

                // Create the sawtooth effect by shifting odd points along x and y
                if (i % 2 != 0) // Check if the index is odd
                {
                    // Apply alternating shifts for the sawtooth effect
                    float shift = shiftFactor * (i % 2 == 1 ? 1 : -1); // Alternating direction for each odd point

                    // Apply the shift to both X and Y axes
                    point.x += shift;
                    point.y += shift;
                }

                lineRenderer.SetPosition(i, point);
            }
        }

        int randStart = UnityEngine.Random.Range(0, rayColors.Count);
        //int randEnd = UnityEngine.Random.Range(0, rayColors.Count);

        Color startRayColor = rayColors[randStart];
        Color endRayColor = rayColors[randStart];

        // Set the color
        lineRenderer.startColor = startRayColor;
        lineRenderer.endColor = endRayColor;

        // Set the width
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        // Set additional properties (optional)
        lineRenderer.useWorldSpace = true; // Use world coordinates
        lineRenderer.sortingOrder = 1;    // Ensure visibility

        //sorting
        lineRenderer.sortingLayerName = "Elements";
        lineRenderer.sortingOrder = 3;


        //set parent
        lineObject.transform.parent = gameArea.transform;

        createdLines.Add(lineObject);
    }


    // Coroutine to fade out the line's opacity and destroy it
    private IEnumerator FadeAndDestroyLine(LineRenderer lineRenderer, GameObject lineObject)
    {
        float fadeDuration = 0.5f; // Time in seconds to fade out
        float startTime = Time.time;
        Color startColor = lineRenderer.startColor;

        //get width
        float startWidth = lineRenderer.startWidth;

        // Gradually decrease opacity over time
        while (Time.time < startTime + fadeDuration)
        {
            float t = (Time.time - startTime) / fadeDuration; // Calculate the time factor
            float alpha = Mathf.Lerp(1f, 0f, t); // Lerp from 1 to 0 for opacity

            float lineWidth = Mathf.Lerp(startWidth, 0f, t); // Lerp from 1 to 0 for opacity

            // Set the color with the new alpha
            Color fadedColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lineRenderer.startColor = fadedColor;
            lineRenderer.endColor = fadedColor;

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            yield return null; // Wait until the next frame
        }

        // Ensure the final opacity is 0
        lineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        lineRenderer.endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        // Destroy the line object after fading
        Destroy(lineObject);
    }

}
