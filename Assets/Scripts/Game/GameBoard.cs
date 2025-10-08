using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Level;


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
    public WorldManager worldManager;


    [Header("Level Info")]
    //public int level;
    public Level level;
    public int loadedLevel;
    public int totalLevels;
    public TMP_Text levelNumberTxt;

    public GameState currentState;
    public MatchState matchState;

    public int levelDifficulty;

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

    [Header("Particles")]
    public GameObject lineBombParticle;


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

    private HashSet<GameObject> usedObjects = new HashSet<GameObject>();

    private int matchForLineBomb = 4;

    private int matchForColorBomb = 5;

    //dict
    private Dictionary<TileKind, int> preloadDict;
    private Dictionary<TileKind, GameObject> breacableDict;
    private Dictionary<TileKind, GameObject> blockersDict;
    private Dictionary<TileKind, GameObject> lockersDict; //lockers
    private Dictionary<TileKind, GameObject> expandDict; //expand

    [Header("Boosters")]
    public bool colorBusterInUse;
    public bool lineBusterInUse;
    private int initialMoves; //for time booster run
    private int boosterValue = 10; //value when buster run TIME boosters

    public Button btnSpeedUp;

    private Coroutine updateCoroutine;

    [Header("LOG")]
    public GameLog log;
    private const string className = "GameBoard:";

    private int bigMatch;

    public string levelUID;

    private ElementController[,] elementControllers;

    private void Awake()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        if (gameDataClass != null)
        {
            gameDataClass.LoadFromFile();
            loadedLevel = gameDataClass.saveData.levelToLoad;
        }

        if (worldManager != null)
        {
            level = worldManager.GetLevel(loadedLevel, out World foundWorld);

            //get total levels
            totalLevels = worldManager.GetTotalLevelsCount();            

            if (level == null)
            {
                log.WriteSysLog($"{className}Failed to load level {loadedLevel}");
                return;
            }

            worldClass = foundWorld;

            // Read level data
            column = level.columns;
            row = level.rows;

            levelDifficulty = (int)level.levelDifficulty; //get difficulty

            elements = level.element;
            if (elements == null || elements.Length == 0)
                log.WriteSysLog($"{className}No elements added to level {loadedLevel}!");

            scoreGoals = level.scoreGoals;
            if (scoreGoals == null || scoreGoals.Length == 0)
                log.WriteSysLog($"{className}No score goals added to level {loadedLevel}!");

            goalsDescription = level.goalsDescription;
            if (goalsDescription == null || goalsDescription.Length == 0)
                log.WriteSysLog($"{className}No Level Description added to level {loadedLevel}!");

            boardLayout = level.boardLayout;
            preloadBoardLayout = level.preloadBoardLayout;
            xmlDocWithBackTileLayout = level.xmlLayoutFile;
            levelUID = level.uID;
        }
        else
        {
            log.WriteSysLog($"{className}WorldManager not assigned!");
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

        //logger start
        if (log == null)
        {
           log = gameObject.AddComponent<GameLog>();
        }
   
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

        //load backTIles!

        if (xmlDocWithBackTileLayout != null)
            backBuilderClass.LoadDataFromXML(xmlDocWithBackTileLayout, column, row);

        //stop 1
        matchState = MatchState.matching_stop;

        levelNumberTxt.text = ($"Level {loadedLevel}");

        //for time booster
        initialMoves = endGameManagerClass.curCounterVal;

        //set big match depends on level size
        if (column <= row)
            bigMatch = column*3;
        else
            bigMatch = row * 3;

        CacheElementControllers();
    }

    //empty cells
    public void GenerateEmptyElements()
    {
        for (int i = 0; i < boardLayout.Length; i++)
        {
            int x = boardLayout[i].columnX;
            int y = boardLayout[i].rowY;

            if (boardLayout[i].tileKind == TileKind.Empty)
            {
                if (x >= 0 && x < emptyElement.GetLength(0) &&
                    y >= 0 && y < emptyElement.GetLength(1))
                {
                    emptyElement[x, y] = true;
                }
                else
                {
                    log.WriteSysLog($"{className}Skipped empty cell: ({x}, {y}) is out of bounds.");
                }
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
        if (elementControllers == null)
        {
            elementControllers = new ElementController[column, row];
        }

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

        //like color bomb  TIME boosters
        if (colorBusterInUse || lineBusterInUse)
            SetTimlessBuster();

        matchState = MatchState.matching_stop;

        CacheElementControllers();

        if (IsDeadLock())
        {
            ShuffleBoard();

            //info mixed up
            if (uiManagerClass != null)
                uiManagerClass.ShowInGameInfo("Mixed up", true, 0, ColorPalette.Colors["DarkBlue"]);
            else
                log.WriteSysLog($"{className}uiManagerClass is null! Cannot show info.");
        }
    }

    //TIME booster 1st.
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

    //when many matches
    private void CongratInfo(int matchCount)
    {
        if (matchCount > bigMatch)
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

        //debug block
        //Debug.Log($"In match: {matchFinderClass.currentMatch.Count}");

        var tagGroups = matchFinderClass.currentMatch
            .GroupBy(obj => obj.tag)
            .ToList(); // Materialize once to avoid re-enumeration


        var filteredList = matchFinderClass.currentMatch
            .GroupBy(obj => obj.tag)
            .Where(group => group.Count() > 3)
            .SelectMany(group => group)
            .Where(obj =>
            {
                var element = obj.GetComponent<ElementController>();
                return element != null && element.matchedByBomb == false;
            })
            .ToList();


        bool genBomb = false;
        int bombMatches = 0;

        foreach (var group in tagGroups)
        {
            int count = group.Count();

            if (count >= minMatchForBomb)
            {
                bombMatches++;
                genBomb = true;
            }
        }

        // Count elements by tag
        foreach (var obj in matchFinderClass.currentMatch)
        {
            if (obj != null)
            {
                switch (obj.tag)
                {
                    case "element_01":
                        log.elem1++;
                        break;
                    case "element_02":
                        log.elem2++;
                        break;
                    case "element_03":
                        log.elem3++;
                        break;
                    case "element_04":
                        log.elem4++;
                        break;
                    case "element_05":
                        log.elem5++;
                        break;
                }
            }
        }

        CongratInfo(matchFinderClass.currentMatch.Count);

        int matchCount = matchFinderClass.currentMatch.Count;        

        //sound
        if (matchCount == 3)
            soundManagerClass.PlaySound(GetRandomClip(soundManagerClass.match3Clips));

        if (matchCount == 4)
            soundManagerClass.PlaySound(GetRandomClip(soundManagerClass.match4Clips));

        if (matchCount >= 5)
            soundManagerClass.PlaySound(GetRandomClip(soundManagerClass.match5Clips));

        //run bomb generation
        if (genBomb && filteredList.Count > 0)
            CheckToGenerateBombs(filteredList);        

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null)
                {                    
                    DestroyMatchesAt(i, j);
                    condition = true;
                }
            }            
        }

        //destroy for blockers
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (blockerCells[i, j] != null)
                {
                    SpecialElements blocker = blockerCells[i, j].GetComponent<SpecialElements>();

                    if (blocker != null && blocker.isMatched)
                    {
                        blocker.isMatched = false;
                        DamageBlockerAt(i, j);
                        condition = true;
                    }
                }
            }
        }

        // here start refill
        if (condition)
            StartCoroutine(DecreaseRowCo());
    }

    //for sound
    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[UnityEngine.Random.Range(0, clips.Length)];
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

        //delay
        yield return new WaitForSeconds(refillDelay);
       
        //step 2 refill
        if (condition)
            StartCoroutine(FillBoardCo());
    }

    public void RunParticles(ElementController element, int thisCol, int thisRow)
    {
        Vector3 elementPosition = allElements[thisCol, thisRow].transform.position;
       
        //simple row column
        if (element.isColumnBomb || element.isRowBomb)
        {
            Quaternion rotation = element.isRowBomb ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
            Vector3 particlePosition = elementPosition;

            fxManagerClass.InstantiateAndConfigureParticle(lineBombParticle, particlePosition, rotation);            
        }

        //wrap part
        if (element.isWrapBomb)
        {
            GameObject elementParticle = Instantiate(element.wrapBombParticle, elementPosition, Quaternion.identity);
            elementParticle.name = "wrap_part_" + "_" + thisCol + "_" + thisRow;
            elementParticle.transform.parent = gameArea.transform;
            
            //combo
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

    public void RunComboParticles(int posValue01, int posValue02, string type, Vector3 elementPosition)
    {
        Quaternion rotation = Quaternion.identity;

        bool isRow = false;

        //set rotation
        if (type == "row")
        {
            rotation = Quaternion.Euler(0, 0, 90); // horizontal
            isRow = true;
        }
        else if (type == "column")
        {
            rotation = Quaternion.identity;        // vertical (default)
        }

        Vector3 particlePosition = elementPosition;

        // Helper to update position based on bomb type
        Vector3 UpdatePosition(Vector3 originalPosition, bool row, int comboValue)
        {
            if (row == true)
            {
                // Row bomb affects Y
                originalPosition.y = comboValue;
            }
            else if (row == false)
            {
                // Column bomb affects X
                originalPosition.x = comboValue;
            }
            return originalPosition;
        }

        // Handle combo particles
        if (type == "column" || type == "row")
        {
            if (posValue01 != -1)
            {
                particlePosition = UpdatePosition(particlePosition, isRow, posValue01);
                fxManagerClass.InstantiateAndConfigureParticle(lineBombParticle, particlePosition, rotation);                
            }

            if (posValue02 != -1)
            {
                particlePosition = UpdatePosition(particlePosition, isRow, posValue02);
                fxManagerClass.InstantiateAndConfigureParticle(lineBombParticle, particlePosition, rotation);                
            }
        }

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
    public void DestroyMatchesAt(int thisColumn, int thisRow)
    {        

        if (allElements[thisColumn, thisRow].GetComponent<ElementController>().isMatched)
        {
            ElementController currentElement = allElements[thisColumn, thisRow].GetComponent<ElementController>();            

            //for bombs in match
            if (currentElement.isWrapBomb)
            {
                soundManagerClass.PlaySound(soundManagerClass.soundClips[5]);
                matchFinderClass.MatchWrapPieces(thisColumn, thisRow);
            }

            //colum bomb match
            if (currentElement.isColumnBomb)
            {
                soundManagerClass.PlaySound(GetRandomClip(soundManagerClass.lineBombVClips));
                matchFinderClass.MatchColPieces(thisColumn);
            }

            //row bomb
            if (currentElement.isRowBomb)
            {
                soundManagerClass.PlaySound(GetRandomClip(soundManagerClass.lineBombHClips));
                matchFinderClass.MatchRowPieces(thisRow);
            }

            //color
            if (currentElement.isColorBomb)
            {
                soundManagerClass.PlaySound(soundManagerClass.soundClips[6]);

                int randomIndex = UnityEngine.Random.Range(0, elements.Length);
                var randomElement = elements[randomIndex];
                string randomTag = randomElement.tag;
                matchFinderClass.MatchColorPieces(randomTag);
            }

            //destroy breakable
            DestroyBreakableAt(thisColumn, thisRow);

            //goal for dots
            if (goalManagerClass != null)
            {
                if (currentElement.isRowBomb || currentElement.isColumnBomb)
                {
                    goalManagerClass.CompareGoal("LineBomb", thisColumn, thisRow, true); //for line bombs 1
                }
                else if (currentElement.isWrapBomb)
                {
                    goalManagerClass.CompareGoal("WrapBomb", thisColumn, thisRow, true); //for Wrap bombs 2                    
                }
                else if (currentElement.isColorBomb)
                {
                    goalManagerClass.CompareGoal("ColorBomb", thisColumn, thisRow, true); //for Color bombs  3                  
                }
                else
                {                    
                    goalManagerClass.CompareGoal(allElements[thisColumn, thisRow].tag.ToString(), thisColumn, thisRow, true); //for usual dots 4
                }

                goalManagerClass.UpdateGoals();
            }

            //for lockers
            DamageLockers(thisColumn, thisRow);

            //for blockers
            DamageBlockers(thisColumn, thisRow);            

            //for expand
            DamageExpandable(thisColumn, thisRow);  

            //particles
            if (currentElement != null)
            {
                RunParticles(currentElement, thisColumn, thisRow);
            }

            //main score counter
            scoreManagerClass.IncreaseScore(baseValue); 

            //remove bombs
            if (bombsCells[thisColumn, thisRow] != null)
            {
                bombsCells[thisColumn, thisRow] = null;
            }

            currentElement.DestroyAnimation();

            //main destroy
            Destroy(allElements[thisColumn, thisRow]); //!Important
                        
            allElements[thisColumn, thisRow] = null;

            UpdateElementControllerCache(thisColumn, thisRow);

            //for colorbomb
            if (fxManagerClass.createdLines.Count > 0)
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

                    element.name = element.tag + "_c" + i + "_r" + j + "_" + currentTime +"_" + counter;

                    UpdateElementControllerCache(i, j);
                }
            }
        }
        
        matchState = MatchState.matching_inprogress;

        //find match 2
        matchFinderClass.FindAllMatches(); 
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
        RefillBoard(); //refil board

        //delay 02
        yield return new WaitForSeconds(refillDelay);

        matchState = MatchState.matching_inprogress;        

        //destroy2
        while (MatchesOnBoard())
        {
            streakValue++; //for score            
            //call #3 run decrease columns      
            DestroyMatches();      
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

        //run booster generator TIME
        int movesMade = initialMoves - endGameManagerClass.curCounterVal; // Moves used so far

        //TIME boosters
        if (movesMade % boosterValue == 0) // Run every 10 moves
        {
            if (colorBusterInUse || lineBusterInUse)
            {
                SetTimlessBuster();
            }
        }
    }


    //gen bombs part 3
    private MatchType ColumnOrRow(List<GameObject> matchGroup)
    {        
        matchTypeClass.type = 0;
        matchTypeClass.color = "";
        matchTypeClass.curElem = null;

        foreach (GameObject centerDot in matchGroup)
        {
            if (centerDot == null || usedObjects.Contains(centerDot)) continue;

            ElementController center = centerDot.GetComponent<ElementController>();
            if (center == null) continue;

            string color = centerDot.tag;
            int col = center.column;
            int row = center.row;

            List<GameObject> horizontalMatches = matchGroup.Where(obj =>
            {
                if (obj == null || obj == centerDot || !obj.CompareTag(color)) return false;
                ElementController ec = obj.GetComponent<ElementController>();
                return ec != null && ec.row == row;
            }).ToList();

            List<GameObject> verticalMatches = matchGroup.Where(obj =>
            {
                if (obj == null || obj == centerDot || !obj.CompareTag(color)) return false;
                ElementController ec = obj.GetComponent<ElementController>();
                return ec != null && ec.column == col;
            }).ToList();

            horizontalMatches.Add(centerDot);
            verticalMatches.Add(centerDot);

            bool hasRow = horizontalMatches.Count >= 3;
            bool hasCol = verticalMatches.Count >= 3;

            // WRAP: L or T shape
            if (hasRow && hasCol)
            {
                matchTypeClass.type = 2;
                matchTypeClass.curElem = centerDot;
                matchTypeClass.color = color;

                // mark used (optional: could merge both groups)
                foreach (GameObject obj in horizontalMatches.Concat(verticalMatches))
                {
                    if (!usedObjects.Contains(obj)) usedObjects.Add(obj);
                }

                return matchTypeClass;
            }

            // COLOR BOMB: Big match
            if (horizontalMatches.Count >= matchForColorBomb || verticalMatches.Count >= matchForColorBomb)
            {
                matchTypeClass.type = 1;
                matchTypeClass.curElem = centerDot;
                matchTypeClass.color = color;

                foreach (GameObject obj in horizontalMatches.Concat(verticalMatches))
                {
                    if (!usedObjects.Contains(obj)) usedObjects.Add(obj);
                }

                return matchTypeClass;
            }

            // LINE BOMB
            if (horizontalMatches.Count >= matchForLineBomb)
            {
                matchTypeClass.type = 3; // Horizontal line
                matchTypeClass.curElem = centerDot;
                matchTypeClass.color = color;

                foreach (GameObject obj in horizontalMatches)
                {
                    if (!usedObjects.Contains(obj)) usedObjects.Add(obj);
                }

                return matchTypeClass;
            }

            if (verticalMatches.Count >= matchForLineBomb)
            {
                matchTypeClass.type = 3; // Vertical line
                matchTypeClass.curElem = centerDot;
                matchTypeClass.color = color;

                foreach (GameObject obj in verticalMatches)
                {
                    if (!usedObjects.Contains(obj)) usedObjects.Add(obj);
                }

                return matchTypeClass;
            }
        }

        // No special match found
        matchTypeClass.type = 0;
        matchTypeClass.color = "";
        matchTypeClass.curElem = null;
        return matchTypeClass;
    }


    ElementController GetSafeOtherElement(ElementController element)
    {
        if (element == null || element.otherElement == null) return null;
        if (element.otherElement.Equals(null)) return null; // destroyed check
        return element.otherElement.GetComponent<ElementController>();
    }


    //gen bomb part 2 // GameObject sourceDot
    public void CheckToGenerateBombs(List<GameObject> bombCandidates)
    {
        // Group bomb candidates by tag (so each group has the same element type)
        var groupedCandidates = bombCandidates
            .GroupBy(obj => obj.tag);

        MatchType typeOfMatch = null;
        
        foreach (var group in groupedCandidates)
        {
            // Only one call to ColumnOrRow per group
            typeOfMatch = ColumnOrRow(group.ToList());
            
            if(typeOfMatch.type > 0)
            {
                BombConstructor(typeOfMatch, group.ToList());                               
            }
        }
    }

    private void BombConstructor(MatchType typeOfMatch, List<GameObject> matchGroup)
    {
        string groupNames = string.Join(", ", matchGroup
            .Where(obj => obj != null)
            .Select(obj => obj.name));

            bool isInMatchGroup = false;

            if (currentElement != null)
            {
                isInMatchGroup = matchGroup.Contains(currentElement.gameObject);
            }

            if (isInMatchGroup)
            {
                BombCreationSwitch(typeOfMatch);                
            }
            else
            {
                // Get all non-null GameObjects with ElementController
                var validElements = matchGroup
                    .Where(obj => obj != null && obj.GetComponent<ElementController>() != null)
                    .ToList();

                if (validElements.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, validElements.Count);
                    GameObject randomObj = validElements[randomIndex];

                    currentElement = randomObj.GetComponent<ElementController>();
                    currentElement.otherElement = null;

                    BombCreationSwitch(typeOfMatch);                    
                }
                else
                {
                    Debug.LogWarning("No valid elements found in matchGroup to assign as currentElement.");
                }
            }
    }

    private void BombCreationSwitch(MatchType typeOfMatch)
    {
        ElementController otherDot = GetSafeOtherElement(currentElement);

        bool currentDotMatched = currentElement != null && currentElement.isMatched && currentElement.tag == typeOfMatch.color;
        bool otherDotMatched = otherDot != null && otherDot.isMatched && otherDot.tag == typeOfMatch.color;

        // 2. Handle cascade fallback
        bool cascadeMode = !currentDotMatched && !otherDotMatched && typeOfMatch.curElem != null;
        ElementController fallbackDot = cascadeMode ? typeOfMatch.curElem.GetComponent<ElementController>() : null;

        switch (typeOfMatch.type)
        {
            case 1: // Color bomb
                if (currentDotMatched)
                    GenerateBomb(currentElement, e => e.GenerateColorBomb());
                else if (otherDotMatched)
                    GenerateBomb(otherDot, e => e.GenerateColorBomb());
                else if (cascadeMode)
                    GenerateBomb(fallbackDot, e => e.GenerateColorBomb());
                break;

            case 2: // Wrap bomb
                if (currentDotMatched)
                    GenerateBomb(currentElement, e => e.GenerateWrapBomb());
                else if (otherDotMatched)
                    GenerateBomb(otherDot, e => e.GenerateWrapBomb());
                else if (cascadeMode)
                    GenerateBomb(fallbackDot, e => e.GenerateWrapBomb());
                break;

            case 3: // Line bomb (horizontal/vertical)
                matchFinderClass.LineBombCheck(typeOfMatch);
                break;

            default:
                break;
        }
    }


    void GenerateBomb(ElementController dot, Action<ElementController> generator)
    {
        //avoid bomb creation bug
        DamageBlockers(dot.column, dot.row);
        DamageExpandable(dot.column, dot.row);

        dot.isMatched = false;
        generator(dot);
    }

    private void DamageLockers(int thisColumn, int thisRow)
    {
        //lockers
        if (lockedCells[thisColumn, thisRow] != null)
        {
            lockedCells[thisColumn, thisRow].TakeDamage(1);

            int hitPoint = lockedCells[thisColumn, thisRow].hitPoints;

            //Debug.Log("hitPoint: " + hitPoint);

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
    public void DamageBlockers(int column, int row)
    {
        try
        {
            DamageBlockerAt(column - 1, row);
            DamageBlockerAt(column + 1, row);
            DamageBlockerAt(column, row - 1);
            DamageBlockerAt(column, row + 1);
        }
        catch
        {
            log.WriteSysLog($"{className}DamageBlockers: errror!");
        }
       
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
                if (!blockerCells[thisColumn, thisRow].wasHitThisFrame)
                {
                    blockerCells[thisColumn, thisRow].TakeDamage(1);
                    blockerCells[thisColumn, thisRow].wasHitThisFrame = true;
                }

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
        else
        {
            //optional for debug
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
            GameObject currentDot = null;

            try
            {
                currentDot = allElements[column, row];
                // Continue processing currentDot if needed...
            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.LogWarning($"Index out of bounds: column={column}, row={row}. Exception: {ex.Message}");
            }


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
                if (allElements != null && allElements[valueX, valueY] != null)
                {
                    Destroy(allElements[valueX, valueY].gameObject);
                }
                else
                {
                    Debug.Log($"enPreloadLayout > No preload object at: {valueX} {valueY}");
                }

                try
                {
                    int index = preloadDict[kind];

                    if (index < 0 || index >= elements.Length)
                    {
                        Debug.LogError($"Invalid index {index} for kind {kind}. Elements array length: {elements.Length}");
                    }
                    else
                    {
                        GameObject preloadElements = Instantiate(elements[index], tempPos, Quaternion.identity);

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
                catch (KeyNotFoundException)
                {
                    Debug.LogError($"Kind '{kind}' not found in preloadDict.");
                }
                catch (IndexOutOfRangeException ex)
                {
                    Debug.LogError($"Index out of range for kind '{kind}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error during instantiation for kind '{kind}': {ex.Message}");
                }
            }
        }
    }

    // Initialize once -NEW
    private void CacheElementControllers()
    {
        elementControllers = new ElementController[column, row];

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] != null)
                {
                    elementControllers[i, j] = allElements[i, j].GetComponent<ElementController>();
                }
            }
        }
    }

    // Update cache when needed -NEW
    private void UpdateElementControllerCache(int i, int j)
    {
        // Safety check: ensure array is initialized
        if (elementControllers == null)
        {
            elementControllers = new ElementController[column, row];
        }

        // Update cache
        if (allElements[i, j] != null)
        {
            elementControllers[i, j] = allElements[i, j].GetComponent<ElementController>();
        }
        else
        {
            elementControllers[i, j] = null;
        }
    }

    // Ultra-optimized CheckForMatches
    private bool CheckForMatches()
    {
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                ElementController e0 = elementControllers[i, j];
                if (e0 == null) continue;

                // Check horizontal
                if (i < column - 2)
                {
                    ElementController e1 = elementControllers[i + 1, j];
                    ElementController e2 = elementControllers[i + 2, j];

                    if (e1 != null && e2 != null)
                    {
                        // Check bombs first (early exit if found)
                        if (HasAnyBomb(e0, e1, e2))
                            return true;

                        // Then check tags
                        if (allElements[i, j].CompareTag(allElements[i + 1, j].tag) &&
                            allElements[i, j].CompareTag(allElements[i + 2, j].tag))
                            return true;
                    }
                }

                // Check vertical
                if (j < row - 2)
                {
                    ElementController e1 = elementControllers[i, j + 1];
                    ElementController e2 = elementControllers[i, j + 2];

                    if (e1 != null && e2 != null)
                    {
                        // Check bombs first
                        if (HasAnyBomb(e0, e1, e2))
                            return true;

                        // Then check tags
                        if (allElements[i, j].CompareTag(allElements[i, j + 1].tag) &&
                            allElements[i, j].CompareTag(allElements[i, j + 2].tag))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    // Helper method for bomb checking -NEW
    private bool HasAnyBomb(ElementController e0, ElementController e1, ElementController e2)
    {
        return e0.isColorBomb || e0.isColumnBomb || e0.isRowBomb || e0.isWrapBomb ||
               e1.isColorBomb || e1.isColumnBomb || e1.isRowBomb || e1.isWrapBomb ||
               e2.isColorBomb || e2.isColumnBomb || e2.isRowBomb || e2.isWrapBomb;
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
        int targetCol = column + (int)direction.x;
        int targetRow = row + (int)direction.y;

        // Prevent out-of-bounds
        if (targetCol < 0 || targetCol >= this.column || targetRow < 0 || targetRow >= this.row)
            return false;

        // Prevent switch if either cell is locked
        if (lockedCells[column, row] != null || lockedCells[targetCol, targetRow] != null)
        {
            return false;
        }

        // Prevent switch if either cell is expandable
        if (expandCells[column, row] != null || expandCells[targetCol, targetRow] != null)
        {
            // Optional: Debug            
            return false;
        }

        // Perform switch
        SwitchPieces(column, row, direction);

        UpdateElementControllerCache(column, row);

        bool hasMatch = CheckForMatches();

        UpdateElementControllerCache(column, row);

        // Revert switch
        SwitchPieces(column, row, direction);

        UpdateElementControllerCache(column, row);

        return hasMatch;
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

            if (uiManagerClass != null)
                uiManagerClass.ShowInGameInfo("Mixed up", true, 0, ColorPalette.Colors["DarkBlue"]);
            else
                Debug.LogError("uiManagerClass is null! Cannot show info.");
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


    public void DamageExpandable(int thisColumn, int thisRow)
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

    //TIME boosters
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
