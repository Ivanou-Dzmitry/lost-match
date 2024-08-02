using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum GameState
{
    wait,
    move,
    win,
    lose,
    pause
}

public enum TileKind
{
    element_01,
    element_02,
    element_03,
    element_04,
    element_05,
    Empty,
    locked
}

//type of matches
[System.Serializable]
public class MatchType
{
    public int type;
    public string color;
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
    public int level;

    public GameState currentState = GameState.move;

    [Header("Size")]
    public int column;
    public int row;

    public float refillDelay = 0.3f;
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

    //arrays
    public GameObject[] elements;
    public GameObject[,] allElements;

    [Header("Match Suff")]
    public MatchType matchTypeClass;
    public ElementController currentElement;

    //for score
    public int baseValue = 1;
    public int streakValue = 1;
    public int[] scoreGoals;

    //for blank
    private bool[,] emptyElement;

    /*    [Header("Prefabs")]
        public GameObject elementPrefab;
        public GameObject break01Prefab;
        public GameObject break02Prefab;
        public GameObject blocker01Prefab;
        public GameObject blocker02Prefab;
        public GameObject expand01Prefab;
        public GameObject locker01Prefab;*/

    //for lock
    public ElementController[,] lockedCells;

    private AudioClip audioClip;

    //bombs values    
    private int minMatchCount = 3;
    public int minMatchForBomb = 4;

    private int matchForLineBomb = 4;
    private int matchForWrapBomb = 2;
    private int matchForColorBomb = 3;

    public bool autoBombGen=false;

    private void Awake()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        if (gameDataClass != null)
        {
            gameDataClass.LoadFromFile();
            level = gameDataClass.saveData.levelToLoad; //load level number
        }

        if (worldClass != null)
        {
            if (level < worldClass.levels.Length)
            {
                if (worldClass.levels[level] != null)
                {
                    column = worldClass.levels[level].columns;

                    row = worldClass.levels[level].rows;

                    elements = worldClass.levels[level].element;

                    scoreGoals = worldClass.levels[level].scoreGoals;

                    gameBoardBack = worldClass.levels[level].elementsBack; //back

                    boardLayout = worldClass.levels[level].boardLayout;

                    preloadBoardLayout = worldClass.levels[level].preloadBoardLayout;
                }
            }
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

        //all dots on board
        allElements = new GameObject[column, row];

        lockedCells = new ElementController[column, row];

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

    }

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


    private void SetUpBoard()
    {
        GenerateEmptyElements();

        //for naming
        int namingCounter = 0;

        //fill board with elements
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (!emptyElement[i, j])
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

                    maxItertion = 0;

                    //instance element
                    GameObject element = Instantiate(elements[elementNumber], elementPosition, Quaternion.identity);

                    //set position
                    element.GetComponent<ElementController>().column = i;
                    element.GetComponent<ElementController>().row = j;

                    //set properties
                    element.transform.parent = gameArea.transform;

                    namingCounter++;

                    //dots naming
                    element.name = element.tag + "-" + namingCounter + "-" + i + "-" + j;

                    //add elements to array
                    allElements[i, j] = element;
                }
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


    public void DestroyMatches()
    {
        //bomb gen part 1
        if (matchFinderClass.currentMatch.Count >= minMatchForBomb)
        {
            autoBombGen = false;
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
        }

        // here start refill
        StartCoroutine(DecreaseRowCo());
    }

    private IEnumerator DecreaseRowCo()
    {
        yield return new WaitForSeconds(refillDelay);

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] == null && !emptyElement[i, j])
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
        }

        yield return new WaitForSeconds(refillDelay);
        //step 2 refill
        StartCoroutine(FillBoardCo());
    }

    private void PlaySound(AudioClip sound)
    {
        //sound
        if (soundManagerClass != null)
        {
            audioClip = sound;

            if (SoundManager.soundManager != null)
            {
                SoundManager.soundManager.PlaySound(audioClip);
            }
        }
    }

    private void DestroyMatchesAt(int column, int row)
    {
        if (allElements[column, row].GetComponent<ElementController>().isMatched)
        {
            ElementController currentElement = allElements[column, row].GetComponent<ElementController>();


            //goal for dots
            //goal for dots
            if (goalManagerClass != null)
            {
                if (currentElement.isRowBomb || currentElement.isColumnBomb)
                {
                    goalManagerClass.CompareGoal("LineBomb"); //for line bombs
                }
                else if (currentElement.isWrapBomb)
                {
                    goalManagerClass.CompareGoal("WrapBomb"); //for Wrap bombs                    
                }
                else
                {
                    goalManagerClass.CompareGoal(allElements[column, row].tag.ToString()); //for usual dots
                }

                goalManagerClass.UpdateGoals();
            }

            //sound
            if (currentElement.elementSound != null)
            {
                PlaySound(currentElement.elementSound);
            }

            //particles
            if (currentElement.destroyParticle != null)
            {
                GameObject elementParticle = Instantiate(currentElement.destroyParticle, allElements[column, row].transform.position, Quaternion.identity);
                Destroy(elementParticle, .9f);
            }

            scoreManagerClass.IncreaseScore(baseValue); //score

            Destroy(allElements[column, row]);
            allElements[column, row] = null;

            //clear match list
            matchFinderClass.currentMatch.Clear();
        }       
    }

    private void RefillBoard()
    {
        int counter = 0;
        string currentTime = DateTime.Now.ToString("mmss");

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
            {
                if (allElements[i, j] == null && !emptyElement[i, j])
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

                    element.name = $"{element.tag}_{currentTime}_{counter}";
                }
            }
        }

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
                        
/*                        //bomb gen part 1-2 MY ADD
                        if (matchFinderClass.currentMatch.Count >= minMatchForBomb)
                        {
                            autoBombGen = true;
                            CheckToGenerateBombs(); //my addd
                        }

                        autoBombGen = false;*/

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
       
        RefillBoard();

        if (matchFinderClass.currentMatch.Count >= minMatchForBomb)
        {
            autoBombGen = true;
            CheckToGenerateBombs(); //my addd
        }


        yield return new WaitForSeconds(refillDelay);

        while (MatchesOnBoard())
        {
            streakValue++; //for score            
            yield return new WaitForSeconds(1f);            
            DestroyMatches();
            yield break;
        }

        currentElement = null;

        if (currentState != GameState.pause)
            currentState = GameState.move;
    }

    //gen bombs part 3
    private MatchType ColumnOrRow()
    {
        // Copy of the current match
        List<GameObject> matchCopy = new List<GameObject>(matchFinderClass.currentMatch);

        matchTypeClass.type = 0;
        matchTypeClass.color = "";

        // Iterate through each dot in the match
        foreach (GameObject matchObject in matchCopy)
        {
            if (matchObject != null)
            {
                ElementController thisElement = matchObject.GetComponent<ElementController>();


                string color = matchObject.tag;  // Get the color from the tag

                int column = thisElement.column;
                int row = thisElement.row;

                int columnMatch = 0;
                int rowMatch = 0;

                // Compare with other dots in the match
                foreach (GameObject otherMatchObject in matchCopy)
                {
                    if (otherMatchObject != null)
                    {
                        if (otherMatchObject == matchObject)
                        {
                            continue;
                        }

                        ElementController nextDot = otherMatchObject.GetComponent<ElementController>();

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
                    return matchTypeClass;
                }
                else if (columnMatch == matchForWrapBomb && rowMatch == matchForWrapBomb)
                {
                    matchTypeClass.type = 2;
                    matchTypeClass.color = color;
                    return matchTypeClass;
                }
                else if (columnMatch == matchForColorBomb || rowMatch == matchForColorBomb)
                {
                    matchTypeClass.type = 3;
                    matchTypeClass.color = color;
                    return matchTypeClass;
                }

            }
        }

        // If no match type found, return default
        matchTypeClass.type = 0;
        matchTypeClass.color = "";
        return matchTypeClass;
    }

    //gen bomb part 2
    public void CheckToGenerateBombs()
    {
        if (matchFinderClass.currentMatch.Count > minMatchCount)
        {
            // Determine match type
            MatchType typeOfMatch = ColumnOrRow();

            //for refill               
            if (currentElement == null && typeOfMatch.type != 0 && autoBombGen == true)
            {
                currentElement = matchFinderClass.currentMatch[0].GetComponent<ElementController>();

                switch (typeOfMatch.type)
                {
                    case 1:
                        currentElement.GenerateColorBomb();
                        currentElement.isMatched = false;
                        Debug.Log("Auto Color");
                        break;
                    case 2:
                        currentElement.GenerateWrapBomb();
                        currentElement.isMatched = false;
                        Debug.Log("Auto Wrap");
                        break;
                    case 3:
                        currentElement.GenerateColumnBomb();
                        currentElement.isMatched = false;
                        Debug.Log("Auto Column");
                        break;
                    default:
                        break;
                }
            }  

            //for move
            if (currentElement != null && autoBombGen == false)
            {
                bool curElemMatched = currentElement.isMatched && currentElement.tag == typeOfMatch.color;
                ElementController otherElem = currentElement.otherElement != null ? currentElement.otherElement.GetComponent<ElementController>() : null;
                bool otherElemMatched = otherElem != null && otherElem.isMatched && otherElem.tag == typeOfMatch.color;

                switch (typeOfMatch.type)
                {
                    case 1:
                        // Color bomb
                        if (curElemMatched)
                        {
                            currentElement.isMatched = false;
                            currentElement.GenerateColorBomb();                            
                        }
                        else if (otherElemMatched)
                        {
                            otherElem.isMatched = false;
                            otherElem.GenerateColorBomb();
                        }
                        break;
                    case 2:
                        // Wrap bomb
                        if (curElemMatched)
                        {
                            currentElement.isMatched = false;
                            currentElement.GenerateWrapBomb();
                        }
                        else if (otherElemMatched)
                        {
                            otherElem.isMatched = false;
                            otherElem.GenerateWrapBomb();
                        }
                        break;
                    case 3:
                            matchFinderClass.LineBombCheck(typeOfMatch);                        
                        break;
                    default:
                        break;
                }
            }
        }
    }

    //for bomb and blockers
    public void BombRow(int row)
    {


    }
    
    //for bomb and blockers
    public void BombColumn(int column)
    {

    }

}
