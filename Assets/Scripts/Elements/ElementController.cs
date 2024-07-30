using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

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

    [Header("Sound")]
    public AudioClip elementSound;
    public GameObject destroyParticle;


    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        matchFinderClass = GameObject.FindWithTag("MatchFinder").GetComponent<MatchFinder>();
        endGameManagerClass = GameObject.FindWithTag("EndGameManager").GetComponent<EndGameManager>();
    }


    private void OnMouseDown()
    {
        if (gameBoardClass.currentState == GameState.move)
        {
            firstTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void OnMouseUp()
    {
        if (gameBoardClass.currentState == GameState.move)
        {
            finalTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CalculateAngle();
        }
    }

    private void CalculateAngle()
    {
        //work with swipe only
        if (Mathf.Abs(finalTouchPos.y - firstTouchPos.y) > swipeResist || Mathf.Abs(finalTouchPos.x - firstTouchPos.x) > swipeResist)
        {
            swipeAngle = Mathf.Atan2(finalTouchPos.y - firstTouchPos.y, finalTouchPos.x - firstTouchPos.x) * 180 / Mathf.PI;

            MoveElement();

            gameBoardClass.currentElement = this;
        }
        else
        {
            gameBoardClass.currentState = GameState.move; 
        }
    }

    //direction based on angle
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

    void MoveElementMechanics(Vector2 direction)
    {
        //set other based on direction
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

            StartCoroutine(CheckMoveCo());
        }
        else
        {
            gameBoardClass.currentState = GameState.move;
        }
    }

    public IEnumerator CheckMoveCo()
    {
        yield return new WaitForSeconds(.1f);

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

                gameBoardClass.currentElement = null;
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

                matchFinderClass.FindAllMatches();
                gameBoardClass.DestroyMatches();                
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
            matchFinderClass.FindAllMatches();
        }
    }
}
