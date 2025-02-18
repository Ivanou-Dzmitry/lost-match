using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ElementController : MonoBehaviour
{
    public GameObject otherElement;

    private Vector2 firstTouchPos = Vector2.zero; //default
    private Vector2 finalTouchPos = Vector2.zero;

    [Header("Board Variables")]
    public bool isMatched = false;
    public int previousColumn, previousRow;
    public int column, row;
    public int targetX, targetY;


    [Header("Swipe Stuff")]
    public float swipeAngle = 0;
    public float swipeResist = 0.5f;

    //classes
    private GameBoard gameBoardClass;
    private MatchFinder matchFinderClass;
    private EndGameManager endGameManagerClass;
    private BonusShop bonusShopClass;
    private HintManager hintManagerClass;
    private GoalManager goalManagerClass;
    private UIManager uiManagerClass;
    private FXManager fxManagerClass;

    private float movementSpeed = 20.0f;

    //bomb
    [Header("PowerUps")]
    public bool isColumnBomb;
    public bool isRowBomb;
    public bool isColorBomb;
    public bool isWrapBomb;

    [Header("Combo")]
    public bool isCombo;
    public int comboE1; //element 1 for combo
    public int comboE2; //element 2 for combo

    [Header("Color")]
    public Color elementColor; //for colorize wrap

    [Header("Bombs Stuff")]
    public Sprite lineBombSprite;
    public Sprite columnBombSprite;
    public Sprite wrapBombSprite;
    public Sprite colorBombSprite;
    public SpriteRenderer bombLayer;


    [Header("Sound")]
    public AudioClip elementSound;
    public AudioClip lineBombSound;
    public AudioClip colorBombSound;
    public AudioClip wrapBombSound;

    [Header("Particles")]
    public GameObject destroyParticle;
    public GameObject lineBombParticle;
    public GameObject wrapBombParticle;
    public GameObject colorBombParticle;

    [Header("Animation")]
    private Animator animatorElement;

    //for color bomb
    private List<Vector2> colorBombElements;    

    private float lastCallTime; // Track the last time the function was called


    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        matchFinderClass = GameObject.FindWithTag("MatchFinder").GetComponent<MatchFinder>();
        endGameManagerClass = GameObject.FindWithTag("EndGameManager").GetComponent<EndGameManager>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        hintManagerClass = GameObject.FindWithTag("HintManager").GetComponent<HintManager>();
        goalManagerClass = GameObject.FindWithTag("GoalManager").GetComponent<GoalManager>();
        uiManagerClass = GameObject.FindWithTag("UIManager").GetComponent<UIManager>();
        fxManagerClass = GameObject.FindWithTag("FXManager").GetComponent<FXManager>();

        animatorElement = GetComponent<Animator>();

        //combo stuff
        isCombo = false;
        comboE1 = -1;
        comboE2 = -1;
    }

    //step 1
    private void OnMouseDown()
    {
       //destroy hint
        if (hintManagerClass != null)
        {
            hintManagerClass.DestroyHint();
        }

        //animation
        if (animatorElement != null)
        {
            animatorElement.SetBool("Touched", true);
        }

        if (gameBoardClass.currentState == GameState.move)
        {
            firstTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        //run using bonus
        if(bonusShopClass.bonusSelected != -1 && gameBoardClass.currentState == GameState.move)
        {
            UseBonus();
        }
        
    }

    //use selected bonus
    private void UseBonus()
    {
        switch (bonusShopClass.bonusSelected)
        {
            case 0:
                gameBoardClass.ShuffleBoard();
                uiManagerClass.ShowInGameInfo("Mixed up", true, 0, ColorPalette.Colors["DarkBlue"]); //show panel with text
                break;
/*            case 1:
                this.isColorBomb = true;
                GenerateColorBomb();
                this.isMatched = true;
                break;*/

            case 2:
                this.isWrapBomb = true;
                matchFinderClass.MatchWrapPieces(column, row);
                GenerateWrapBomb();
                this.isMatched = true;
                break;

            case 3:
                this.isRowBomb = true;                
                matchFinderClass.MatchRowPieces(row);                
                GenerateRowBomb();
                this.isMatched = true;
                break;

            case 4:
                this.isColumnBomb = true;                
                matchFinderClass.MatchColPieces(column);
                GenerateColumnBomb();
                this.isMatched = true;
                break;

            default:
                Debug.LogWarning("Invalid bonus selected.");
                break;
        }

        bonusShopClass.bonusSelected = -1;
        bonusShopClass.bonusDescPanel.SetActive(false);

        bonusShopClass.shopState = BonusShop.ShopState.Game;

        gameBoardClass.currentState = GameState.wait;
        gameBoardClass.DestroyMatches();
    }


    //step 2
    private void OnMouseUp()
    {
        //animation
        if (animatorElement != null)
        {
            animatorElement.SetBool("Touched", false);
        }

        if (gameBoardClass.currentState == GameState.move)
        {
            finalTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CalculateAngle(); //step 3
        }
    }

    //step 4
    private void CalculateAngle()
    {
        //work with swipe only
        if (Mathf.Abs(finalTouchPos.y - firstTouchPos.y) > swipeResist || Mathf.Abs(finalTouchPos.x - firstTouchPos.x) > swipeResist)
        {
            //state
            gameBoardClass.currentState = GameState.wait;

            swipeAngle = Mathf.Atan2(finalTouchPos.y - firstTouchPos.y, finalTouchPos.x - firstTouchPos.x) * 180 / Mathf.PI;

            MoveElement(); // work with element part 1

            gameBoardClass.currentElement = this;  // current 1
        }
        else
        {
            gameBoardClass.currentState = GameState.move; 
        }
    }

    //step 5. Direction based on angle
    void MoveElement()
    {
        if (swipeAngle > -45 && swipeAngle <= 45 && column < gameBoardClass.column - 1)
        {            
            MoveElementMechanics(Vector2.right); //right swipe
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && row < gameBoardClass.row - 1)
        {            
            MoveElementMechanics(Vector2.up); //up swipe
        }
        else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        {            
            MoveElementMechanics(Vector2.left); //left swipe
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0)
        {            
            MoveElementMechanics(Vector2.down);//down swipe
        }
        else
        {
            gameBoardClass.currentState = GameState.move;
        }
    }



    // step 6
    void MoveElementMechanics(Vector2 direction)
    {
        //set otherElement based on direction - !!!        
        otherElement = gameBoardClass.allElements[column + (int)direction.x, row + (int)direction.y];

        //set rows
        previousRow = row;
        previousColumn = column;

        if (gameBoardClass.lockedCells[column, row] == null && gameBoardClass.lockedCells[column + (int)direction.x, row + (int)direction.y] == null)
        {
            if (otherElement != null)
            {
                otherElement.GetComponent<ElementController>().column += -1 * (int)direction.x;
                otherElement.GetComponent<ElementController>().row += -1 * (int)direction.y;

                column += (int)direction.x;
                row += (int)direction.y;

                StartCoroutine(CheckMoveCo()); // work with element part 4
            }
            else
            {
                gameBoardClass.currentState = GameState.move;
            }
        }
        else
        {
            gameBoardClass.currentState = GameState.move;
        }

    }

    private void ColorBombRaysCooker(Vector2 startPoint)
    {
        if (colorBombElements.Count > 0)
        {
            for (int i = 0; i < colorBombElements.Count; i++)
            {
                fxManagerClass.CreateColorBombLines(startPoint, colorBombElements[i], Color.red, 0.5f);
            }
        }
    }

    //setp 7
    public IEnumerator CheckMoveCo()
    {
        //for color bomb
        Vector2 startPoint = new Vector2(this.column, this.row);
        fxManagerClass.createdLines.Clear();

        isCombo = false;
        comboE1 = -1;
        comboE2 = -1;

        //get other element
        ElementController otherElem = otherElement.GetComponent<ElementController>();
        ElementController thisElement = this.GetComponent<ElementController>();

        //for color bobmb 1
        if (isColorBomb && !(otherElem.isRowBomb || otherElem.isColumnBomb || otherElem.isWrapBomb || otherElem.isColorBomb))
        {
            colorBombElements = matchFinderClass.MatchColorPieces(otherElement.tag); //for colorbomb
            isMatched = true;

            //color bomb rays
            ColorBombRaysCooker(startPoint);
        }
        else if (otherElem.isColorBomb && !(isRowBomb || isColumnBomb || isWrapBomb || isColorBomb))
        {
            colorBombElements = matchFinderClass.MatchColorPieces(this.gameObject.tag); //for colorbomb
            otherElem.isMatched = true;

            //color bomb rays
            ColorBombRaysCooker(startPoint);
        }

        //row
        if (isRowBomb && !otherElem.isRowBomb)
        {
            matchFinderClass.MatchRowPieces(row);
            isMatched = true;
        }
 
        //column
        if (isColumnBomb && !otherElem.isColumnBomb)
        {
            matchFinderClass.MatchColPieces(column);
            isMatched = true;
        }

        //wrap
        if (isWrapBomb && !otherElem.isWrapBomb)
        {
            matchFinderClass.MatchWrapPieces(column, row);
            isMatched = true;            
        }         
  
        //check combos
        CheckBombCombinations(thisElement, otherElem);

        yield return new WaitForSeconds(.1f);

        //for all elements
        if (otherElement != null)
        {
            //for other element if this bomb         
            if(otherElem.isRowBomb && !isRowBomb)
            {                
                matchFinderClass.MatchRowPieces(row);
                otherElem.isMatched = true;
            }

            if (otherElem.isColumnBomb && !isColumnBomb)
            {
                matchFinderClass.MatchColPieces(column);
                otherElem.isMatched = true;
            }

            if (otherElem.isWrapBomb && !isWrapBomb)
            {
                matchFinderClass.MatchWrapPieces(column, row);
                otherElem.isMatched = true;
            } 

            if (!isMatched && !otherElement.GetComponent<ElementController>().isMatched)
            {
                //get column and row
                otherElement.GetComponent<ElementController>().row = row;
                otherElement.GetComponent<ElementController>().column = column;

                row = previousRow;
                column = previousColumn;

                //yield return new WaitForSeconds(.1f);

                gameBoardClass.currentElement = null; // current 2
                gameBoardClass.currentState = GameState.move;
            }
            else
            {
                //score
                if (endGameManagerClass != null)
                {
                    if (endGameManagerClass.EndGameReqClass.gameType == GameType.Moves)
                    {                        
                        endGameManagerClass.DecreaseCounterVal();
                    }
                }

                gameBoardClass.DestroyMatches();  //destroy 2  -- step 8                                                  
            }
        }
    }

    private void RowCombo(int[,] directions, int infoIndex = -1)
    {
        // Define a HashSet to store processed columns
        HashSet<int> processedRows = new HashSet<int>();

        // Loop through each direction
        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int newRow = row + directions[i, 1];

             if (newRow >= 0 && newRow <= gameBoardClass.row-1)
                {
                // Check bounds and call the method if valid
                if (!processedRows.Contains(newRow))
                {
                    processedRows.Add(newRow);
                    matchFinderClass.MatchRowPieces(newRow);
                }
            }
        }

        isMatched = true;

        //combo stuff
        processedRows.Remove(this.row);

        // Convert HashSet to List
        List<int> processedRowsList = processedRows.ToList();

        if(processedRows.Count>0)
            isCombo = true;

        // Assign values to variables by index
        comboE1 = processedRowsList.Count > 0 ? processedRowsList[0] : -1; // Default value if empty
        comboE2 = processedRowsList.Count > 1 ? processedRowsList[1] : -1; // Default value if empty
    }

    //combos
    private void ColumnCombo(int[,] directions, int infoIndex = -1)
    {
        // Define a HashSet to store processed columns
        HashSet<int> processedColumns = new HashSet<int>();

        // Loop through each direction
        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int newColumn = column + directions[i, 0];

            // If the column has not been processed yet
             if (newColumn >= 0 && newColumn <= gameBoardClass.column-1)
             {                
                if (!processedColumns.Contains(newColumn))
                {
                    // Add the column to the HashSet
                    processedColumns.Add(newColumn);

                    // Call the method
                    matchFinderClass.MatchColPieces(newColumn);
                }
            }
        }

        isMatched = true;

        processedColumns.Remove(this.column);

        // Convert HashSet to List
        List<int> processedColumnsList = processedColumns.ToList();

        if (processedColumns.Count > 0)
            isCombo = true;

        // Assign values to variables by index
        comboE1 = processedColumnsList.Count > 0 ? processedColumnsList[0] : -1; // Default value if empty
        comboE2 = processedColumnsList.Count > 1 ? processedColumnsList[1] : -1; // Default value if empty
    }

    private void WrapCombo(int[,] directions, int infoIndex = -1)
    {
        // HashSet to track visited positions
        HashSet<(int, int)> visitedPositions = new HashSet<(int, int)>();

        // Loop through each direction
        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int newColumn = column + directions[i, 0];
            int newRow = row + directions[i, 1];

            // Check bounds and call the method if valid
            if (newColumn >= 0 && newColumn <= gameBoardClass.column - 1 && newRow >= 0 && newRow <= gameBoardClass.row - 1)
            {
                // Check if this position has already been processed
                if (!visitedPositions.Contains((newColumn, newRow)))
                {
                    // Add the position to the HashSet
                    visitedPositions.Add((newColumn, newRow));

                    // Call the method
                    matchFinderClass.MatchWrapPieces(newColumn, newRow);
                }
            }
        }

        isMatched = true;

        if (visitedPositions.Count > 0)
            isCombo = true;
    }

    void CheckBombCombinations(ElementController thisElem, ElementController otherElem)
    {
        // HashSet to track executed combinations
        HashSet<string> executedCombinations = new HashSet<string>();

        // Mapping of bomb types to their property names
        Dictionary<string, Func<ElementController, bool>> bombTypes = new Dictionary<string, Func<ElementController, bool>>()
    {
        { "isRowBomb", e => e.isRowBomb },
        { "isColumnBomb", e => e.isColumnBomb },
        { "isWrapBomb", e => e.isWrapBomb },
        { "isColorBomb", e => e.isColorBomb }
    };

        // Iterate over all combinations of thisElem and otherElem properties
        foreach (var thisBomb in bombTypes)
        {
            foreach (var otherBomb in bombTypes)
            {
                // Check if both bomb types are true
                if (thisBomb.Value(thisElem) && otherBomb.Value(otherElem))
                {
                    // Normalize the combination by sorting the keys alphabetically
                    string combination = string.Compare(thisBomb.Key, otherBomb.Key) <= 0
                        ? $"{thisBomb.Key}/{otherBomb.Key}"
                        : $"{otherBomb.Key}/{thisBomb.Key}";

                    // Check if this combination has already been processed
                    if (!executedCombinations.Contains(combination))
                    {
                        // Add the combination to the HashSet
                        executedCombinations.Add(combination);
                        // Call the desired function
                        ExecuteComboAction(combination);
                    }
                }
            }
        }
    }

    void ExecuteComboAction(string combination)
    {
        //directions
        int[,] directions = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        //support for combo
        int randomIndex = UnityEngine.Random.Range(0, gameBoardClass.elements.Length);
        var randomElement = gameBoardClass.elements[randomIndex];
        string randomTag = randomElement.tag;
        Vector2 startPoint = new Vector2(this.column, this.row);

        // Add logic based on the combination string
        switch (combination)
        {
            case "isRowBomb/isRowBomb":         //row-row 1
                RowCombo(directions);
                break;
            case "isColumnBomb/isRowBomb":      //col-row 2
                RowCombo(directions);
                ColumnCombo(directions);
                break;
            case "isRowBomb/isWrapBomb":        //row-wrap 3
                RowCombo(directions);
                WrapCombo(directions);
                break;
            case "isColumnBomb/isWrapBomb":     //col-wrap 4
                ColumnCombo(directions);
                WrapCombo(directions);
                break;
            case "isColumnBomb/isColumnBomb":   //col-col 5
                ColumnCombo(directions);
                break;
            case "isWrapBomb/isWrapBomb":       //wrap-wrap 6
                WrapCombo(directions);
                break;
            case "isColorBomb/isColumnBomb":    //color-col 7
                ColumnCombo(directions,0);
                colorBombElements = matchFinderClass.MatchColorPieces(randomTag); //for colorbomb
                ColorBombRaysCooker(startPoint);                
                break;
            case "isColorBomb/isRowBomb":       //color-row 8
                RowCombo(directions, 0);
                colorBombElements = matchFinderClass.MatchColorPieces(randomTag); //for colorbomb
                ColorBombRaysCooker(startPoint);
                break;
            case "isColorBomb/isWrapBomb":      //color-wrap 9
                WrapCombo(directions, 0);
                colorBombElements = matchFinderClass.MatchColorPieces(randomTag); //for colorbomb
                ColorBombRaysCooker(startPoint);
                break;
            case "isColorBomb/isColorBomb":      //color-color 10
                colorBombElements = matchFinderClass.MatchColorPieces(randomTag);
                ColorBombRaysCooker(startPoint);

                ColumnCombo(directions,0);
                WrapCombo(directions,0);
                RowCombo(directions,0);
                break;
            // Add other cases as needed
            default:
                Debug.Log("Executing Default Combination Logic");
                break;
        }
    }


    // Update is called once per frame
    void Update()
    {
        targetX = column;
        targetY = row;        

        Vector2 targetPosition = new Vector2(targetX, targetY);

       // Move towards the target position
        if (Vector2.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector2.Lerp(transform.position, targetPosition, movementSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }

        if (gameBoardClass.allElements[column, row] != this.gameObject)
        {
            gameBoardClass.allElements[column, row] = this.gameObject;

            matchFinderClass.FindAllMatches(); //find match 1
        }
    }


    public void GenerateRowBomb()
    {
        if (!isColumnBomb && !isColorBomb && !isWrapBomb)
        {
            isRowBomb = true;
            tag = "no_tag";
            elementSound = lineBombSound;

            this.GetComponent<SpriteRenderer>().sprite = null;

            //add sprite
            bombLayer.sprite = lineBombSprite;

            this.GetComponent<ElementController>().otherElement = null;
        }            
    }

    public void GenerateColumnBomb()
    {
        if (!isRowBomb && !isColorBomb && !isWrapBomb)
        {
            isColumnBomb = true;
            tag = "no_tag";
            elementSound = lineBombSound;

            this.GetComponent<SpriteRenderer>().sprite = null;

            //add sprite
            bombLayer.sprite = columnBombSprite;

            this.GetComponent<ElementController>().otherElement = null;
        }            
    }


    public void GenerateColorBomb()
    {
        if (!isColumnBomb && !isRowBomb && !isWrapBomb)
        {
            isColorBomb = true;
            this.tag = "no_tag";
            elementSound = colorBombSound;

            this.GetComponent<SpriteRenderer>().sprite = null;

            //add sprite
            bombLayer.sprite = colorBombSprite;

            this.GetComponent<ElementController>().otherElement = null;
        }            
    }

    public void GenerateWrapBomb()
    {
        if (!isColumnBomb && !isRowBomb && !isColorBomb)
        {
            isWrapBomb = true;
            tag = "no_tag";
            elementSound = wrapBombSound;

            this.GetComponent<SpriteRenderer>().sprite = null;

            //add sprite
            bombLayer.sprite = wrapBombSprite;

            this.GetComponent<ElementController>().otherElement = null;
        }            
    }

    public void DestroyAnimation()
    {
        if (animatorElement != null)
        {
            animatorElement.SetBool("Destroy", true);
        }
    }


}
