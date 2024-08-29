using System.Collections;
using System.Collections.Generic;
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

    private float movementSpeed = 20.0f;

    //bomb
    [Header("PowerUps")]
    public bool isColumnBomb;
    public bool isRowBomb;
    public bool isColorBomb;
    public bool isWrapBomb;

    [Header("Color")]
    public Color elementColor; //for colorize wrap

    [Header("Bombs Stuff")]
    public Sprite lineBombSprite;
    public Sprite wrapBombSprite;
    public Sprite colorBombSprite;
    public SpriteRenderer bombLayer;


    [Header("Sound")]
    public AudioClip elementSound;

    [Header("Particles")]
    public GameObject destroyParticle;
    public GameObject lineBombParticle;
    public GameObject wrapBombParticle;


    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        matchFinderClass = GameObject.FindWithTag("MatchFinder").GetComponent<MatchFinder>();
        endGameManagerClass = GameObject.FindWithTag("EndGameManager").GetComponent<EndGameManager>();
    }

    //step 1
    private void OnMouseDown()
    {
        if (gameBoardClass.currentState == GameState.move)
        {
            firstTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    //step 2
    private void OnMouseUp()
    {
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

    //setp 7
    public IEnumerator CheckMoveCo()
    {
        //for color bobmb 1
        if (isColorBomb)
        {
            matchFinderClass.MatchColorPieces(otherElement.tag);
            isMatched = true;
        }
        else if (otherElement.GetComponent<ElementController>().isColorBomb)
        {
            matchFinderClass.MatchColorPieces(this.gameObject.tag);
            otherElement.GetComponent<ElementController>().isMatched = true;
        }

        yield return new WaitForSeconds(.1f);

        //for all elements
        if (otherElement != null)
        {
            if (!isMatched && !otherElement.GetComponent<ElementController>().isMatched)
            {
                //get column and row
                otherElement.GetComponent<ElementController>().row = row;
                otherElement.GetComponent<ElementController>().column = column;

                row = previousRow;
                column = previousColumn;

                yield return new WaitForSeconds(.1f);

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
            
            //add sprite
            bombLayer.sprite = lineBombSprite;
        }            
    }

    public void GenerateColumnBomb()
    {
        if (!isRowBomb && !isColorBomb && !isWrapBomb)
        {
            isColumnBomb = true;

            //add sprite
            bombLayer.sprite = lineBombSprite;
            bombLayer.transform.eulerAngles = new Vector3 (0, 0, 90);
        }            
    }


    public void GenerateColorBomb()
    {
        if (!isColumnBomb && !isRowBomb && !isWrapBomb)
        {
            isColorBomb = true;

            this.GetComponent<SpriteRenderer>().sprite = null;

            //add sprite
            bombLayer.sprite = colorBombSprite;
        }            
    }

    public void GenerateWrapBomb()
    {
        if (!isColumnBomb && !isRowBomb && !isColorBomb)
        {
            isWrapBomb = true;

            //add sprite
            bombLayer.sprite = wrapBombSprite;
        }            
    }
}
