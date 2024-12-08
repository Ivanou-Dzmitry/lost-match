using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchFinder : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;

    //List for match
    public List<GameObject> currentMatch = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
    }

    //for match - step 1 - run coroutine
    public void FindAllMatches()
    {
        StartCoroutine(FindAllMatchesCo());
    }

    //list of matches
    private void AddToListMatch(GameObject element)
    {
        if (!currentMatch.Contains(element))
        {
            currentMatch.Add(element); //add to match list
        }

        //mark as matched
        element.GetComponent<ElementController>().isMatched = true;

        //element.GetComponent<SpriteRenderer>().color = element.GetComponent<ElementController>().elementColor; //tint for debug
        //element.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 1f); //tint for debug

        // Remove all null elements
        currentMatch.RemoveAll(item => item == null);
    }

    //get 3 usual peaces - match 3
    private void GetNearbyPieces(GameObject element1, GameObject element2, GameObject element3)
    {
        if (element1 != null)
            AddToListMatch(element1);

        if (element2 != null)
            AddToListMatch(element2);

        if (element3 != null)
            AddToListMatch(element3);
    }

    //main checker logic
    private IEnumerator FindAllMatchesCo()
    {
        yield return null;

        gameBoardClass.matchState = GameState.matching_inprogress;

        for (int i = 0; i < gameBoardClass.column; i++)
        {
            for (int j = 0; j < gameBoardClass.row; j++)
            {
                GameObject currentElement = gameBoardClass.allElements[i, j]; //store board

                if (currentElement != null)
                {
                    //get element Controller
                    ElementController curElemGet = currentElement.GetComponent<ElementController>(); //get first dot

                    //horizontal check
                    if (i > 0 && i < gameBoardClass.column - 1)
                    {
                        //get neiborhood L and R
                        GameObject leftElement = gameBoardClass.allElements[i - 1, j];
                        GameObject rightElement = gameBoardClass.allElements[i + 1, j];

                        if (leftElement != null && rightElement != null)
                        {
                            //get neiborhood elements Controllers
                            ElementController leftElemGet = leftElement.GetComponent<ElementController>(); //get 2nd dot
                            ElementController rightElemGet = rightElement.GetComponent<ElementController>(); //get 3rd dot

                            //compare tags
                            if (leftElement != null && rightElement != null)
                            {
                                if (leftElement.tag == currentElement.tag && rightElement.tag == currentElement.tag) //compare tags form lr dots
                                {
                                    //row bomb - step 1
                                    currentMatch.AddRange(IsRowBomb(leftElemGet, curElemGet, rightElemGet));

                                    //column bomb
                                    currentMatch.AddRange(IsColumnBomb(leftElemGet, curElemGet, rightElemGet));

                                    //wrap bomb
                                    currentMatch.AddRange(IsWrapBomb(leftElemGet, curElemGet, rightElemGet));

                                    //std peaces - match 3
                                    GetNearbyPieces(leftElement, currentElement, rightElement);
                                }
                            }
                        }
                    }

                    //vertical check
                    if (j > 0 && j < gameBoardClass.row - 1)
                    {
                        ////get neiborhood elements GO
                        GameObject upElement = gameBoardClass.allElements[i, j + 1];
                        GameObject downElement = gameBoardClass.allElements[i, j - 1];

                        if (upElement != null && downElement != null)
                        {
                            //get neiborhood elements Controllers
                            ElementController upElemGet = upElement.GetComponent<ElementController>();
                            ElementController downElemGet = downElement.GetComponent<ElementController>();

                            //compare elements
                            if (upElement != null && downElement != null)
                            {
                                if (upElement.tag == currentElement.tag && downElement.tag == currentElement.tag)
                                {
                                    //column bobm
                                    currentMatch.AddRange(IsColumnBomb(upElemGet, curElemGet, downElemGet));

                                    //row bomb
                                    currentMatch.AddRange(IsRowBomb(upElemGet, curElemGet, downElemGet));

                                    //wrap bomb
                                    currentMatch.AddRange(IsWrapBomb(upElemGet, curElemGet, downElemGet));

                                    //std peaces - match 3
                                    GetNearbyPieces(upElement, currentElement, downElement);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //bomb gen part 4
    public void LineBombCheck(MatchType matchType)
    {
        //move or not move?
        if (gameBoardClass.currentElement != null)
        {           
            if (gameBoardClass.currentElement.isMatched && gameBoardClass.currentElement.tag == matchType.color)
            {
                //unmatch
                gameBoardClass.currentElement.isMatched = false;

                float angle1 = 0;

                angle1 = gameBoardClass.currentElement.swipeAngle;

                if(angle1 == 0)
                {
                    angle1 = UnityEngine.Random.Range(-135f, 135);
                }

                //for swipe
                if ((angle1 > -45 && angle1 <= 45) || (angle1 < -135 || angle1 >= 135))
                {
                    gameBoardClass.currentElement.GenerateRowBomb();                    
                }
                else
                {
                    gameBoardClass.currentElement.GenerateColumnBomb();
                }

            }
            else if (gameBoardClass.currentElement.otherElement != null)
            {
                ElementController otherDot = gameBoardClass.currentElement.otherElement.GetComponent<ElementController>();

                //if other dots matched
                if (otherDot.isMatched && otherDot.tag == matchType.color)
                {
                    otherDot.isMatched = false;

                    float angle2 = 0;

                    angle2 = gameBoardClass.currentElement.swipeAngle;

                    if (angle2 == 0)
                    {
                        angle2 = UnityEngine.Random.Range(-135f, 135);
                    }

                    //for swipe
                    if ((angle2 > -45 && angle2 <= 45) || (angle2 < -135 || angle2 >= 135))
                    {
                        otherDot.GenerateRowBomb();
                    }
                    else
                    {
                        otherDot.GenerateColumnBomb();
                    }
                }
            }
            else
            {
                int Random = UnityEngine.Random.Range(0, 2);

                gameBoardClass.currentElement.isMatched = false;

                if (Random == 0)
                {
                    gameBoardClass.currentElement.GenerateColumnBomb();
                }
                else
                {
                    gameBoardClass.currentElement.GenerateRowBomb();
                }
            }
        }
    }

    //simple bomb logic
    public void MatchRowPieces(int row)
    {
        currentMatch.AddRange(GetRowPieces(row));
    }

    public void MatchColPieces(int col)
    {
        currentMatch.AddRange(GetColumnPieces(col));
    }

    public void MatchWrapPieces(int col, int row)
    {
        currentMatch.AddRange(GetWrapPieces(col, row));
    }

    //for row bobm - step 3
    List<GameObject> GetRowPieces(int row)
    {
        List<GameObject> elements = new List<GameObject>();

        for (int i = 0; i < gameBoardClass.column; i++)
        {
            if (gameBoardClass.allElements[i, row] != null)
            {

                ElementController localElement = gameBoardClass.allElements[i, row].GetComponent<ElementController>();

                if (localElement.isColumnBomb)
                {
                    elements.Union(GetColumnPieces(i)).ToList();
                }

                elements.Add(gameBoardClass.allElements[i, row]);

                localElement.isMatched = true; //match here
            }

            //add for blockers
            if (gameBoardClass.blockerCells[i, row] != null)
            {
                gameBoardClass.DamageBlockerAt(i, row);
            }
        }

        return elements;
    }

    //row bomb list - step 2
    private List<GameObject> IsRowBomb(ElementController element01, ElementController element02, ElementController element03)
    {
        List<GameObject> currentElements = new List<GameObject>();

        if (element01.isRowBomb)
        {
            currentMatch.AddRange(GetRowPieces(element01.row));
            gameBoardClass.BombRow(element01.row);
        }

        if (element02.isRowBomb)
        {
            currentMatch.AddRange(GetRowPieces(element02.row));
            gameBoardClass.BombRow(element02.row);
        }

        if (element03.isRowBomb)
        {
            currentMatch.AddRange(GetRowPieces(element03.row));
            gameBoardClass.BombRow(element03.row);
        }

        return currentElements;
    }

    //for column bomb part 1
    List<GameObject> GetColumnPieces(int column)
    {
        List<GameObject> elements = new List<GameObject>();

        for (int i = 0; i < gameBoardClass.row; i++)
        {
            if (gameBoardClass.allElements[column, i] != null)
            {

                ElementController localElement = gameBoardClass.allElements[column, i].GetComponent<ElementController>();


                if (localElement.isRowBomb)
                {
                    elements.Union(GetRowPieces(i)).ToList();
                }

                elements.Add(gameBoardClass.allElements[column, i]);

                localElement.isMatched = true; //match here

            }

            //add for blockers
            if (gameBoardClass.blockerCells[column, i] != null)
            {               
                gameBoardClass.DamageBlockerAt(column, i);
            }
        }

        return elements;
    }

    //column bomb list part 2
    private List<GameObject> IsColumnBomb(ElementController element01, ElementController element02, ElementController element03)
    {
        List<GameObject> currentElements = new List<GameObject>();


        if (element01.isColumnBomb)
        {
            currentMatch.AddRange(GetColumnPieces(element01.column));
            gameBoardClass.BombColumn(element01.column);
        }

        if (element02.isColumnBomb)
        {
            currentMatch.AddRange(GetColumnPieces(element02.column));
            gameBoardClass.BombColumn(element02.column);
        }

        if (element03.isColumnBomb)
        {
            currentMatch.AddRange(GetColumnPieces(element03.column));
            gameBoardClass.BombColumn(element03.column);
        }

        return currentElements;
    }

    //list for wrap part 1
    private List<GameObject> IsWrapBomb(ElementController element01, ElementController element02, ElementController element03)
    {
        List<GameObject> currentElement = new List<GameObject>();


        if (element01.isWrapBomb)
        {
            currentMatch.AddRange(GetWrapPieces(element01.column, element01.row));
        }

        if (element02.isWrapBomb)
        {
            currentMatch.AddRange(GetWrapPieces(element02.column, element02.row));
        }

        if (element03.isWrapBomb)
        {
            currentMatch.AddRange(GetWrapPieces(element03.column, element03.row));
        }

        return currentElement;
    }

    //wpap bomb part 2
    List<GameObject> GetWrapPieces(int column, int row)
    {
        List<GameObject> elements = new List<GameObject>();

        //only around
        for (int i = column - 1; i <= column+1; i++)
        {
            for (int j = row - 1; j <= row+1 ; j++)
            {
                //for border
                if(i >= 0 && i < gameBoardClass.column && j>= 0 && j < gameBoardClass.row)
                {
                    //fix bug
                    if (gameBoardClass.allElements[i,j] != null)
                    {
                        elements.Add(gameBoardClass.allElements[i, j]);
                        gameBoardClass.allElements[i, j].GetComponent<ElementController>().isMatched = true;
                    }

                    //add for blockers
                    if (gameBoardClass.blockerCells[i, j] != null)
                    {
                        gameBoardClass.DamageBlockerAt(i, j);
                    }
                }
            }
        }

        return elements;
    }

    //color bobmb part 2
    public void MatchColorPieces(string color)
    {
        for (int i = 0; i < gameBoardClass.column; i++)
        {
            for (int j = 0; j < gameBoardClass.row; j++)
            {
                if (gameBoardClass.allElements[i, j] != null)
                {
                    if (gameBoardClass.allElements[i, j].tag == color)
                    {
                        gameBoardClass.allElements[i, j].GetComponent<ElementController>().isMatched = true;
                    }
                }
            }
        }
    }
}
