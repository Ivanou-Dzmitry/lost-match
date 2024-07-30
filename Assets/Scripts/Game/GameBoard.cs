using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

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
    empty,
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

/*    [Header("Prefabs")]
    public GameObject elementPrefab;
    public GameObject break01Prefab;
    public GameObject break02Prefab;
    public GameObject blocker01Prefab;
    public GameObject blocker02Prefab;
    public GameObject expand01Prefab;
    public GameObject locker01Prefab;*/

    //for blank
    private bool[,] emptyCells;
    //for lock
    public ElementController[,] lockedCells;

    private AudioClip audioClip;

    private void Awake()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        level = gameDataClass.saveData.levelToLoad; //load level number

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

        //init type of objects
        emptyCells = new bool[column, row];

        //all dots on board
        allElements = new GameObject[column, row];

        lockedCells = new ElementController[column, row];

        //setup board
        SetUpBoard();

        //Set Framerate
        Application.targetFrameRate = 30;

        //set resoluton
        Screen.SetResolution(1920, 1080, true);
        Screen.SetResolution((int)Screen.width, (int)Screen.height, true);
    }


    private void SetUpBoard()
    {

        //for naming
        int namingCounter = 0;

        //fill board with elements
        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < row; j++)
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
                if (allElements[i, j] == null)
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
            if (goalManagerClass != null)
            {
                goalManagerClass.CompareGoal(allElements[column, row].tag.ToString());

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
                if (allElements[i, j] == null)
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
       
        RefillBoard();

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

}
