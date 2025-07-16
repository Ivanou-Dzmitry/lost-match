using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameObjectUtils
{
    public static List<GameObject> RemoveDuplicatesByName(List<GameObject> objects)
    {
        // Remove null objects first
        objects = objects.Where(obj => obj != null).ToList();

        // Remove duplicates by name
        return objects
            .GroupBy(obj => obj.name)
            .Select(group => group.First())
            .ToList();
    }
}

public class MatchFinder : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;

    //List for match
    public List<GameObject> currentMatch = new List<GameObject>();

    //list for color
    private List<Vector2> colorBombElements; // Declare a List of Vector2

    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        //list for color bomb
        colorBombElements = new List<Vector2>();
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

        //log
/*        if (element != null)
        {
            switch (element.tag)
            {
                case "element_01":
                    gameBoardClass.log.elem1++;
                    break;
                case "element_02":
                    gameBoardClass.log.elem2++;
                    break;
                case "element_03":
                    gameBoardClass.log.elem3++;
                    break;
                case "element_04":
                    gameBoardClass.log.elem4++;
                    break;
                case "element_05":
                    gameBoardClass.log.elem5++;
                    break;
            }
        }*/


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

        //important
        gameBoardClass.matchState = MatchState.matching_inprogress;

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

        gameBoardClass.matchState = MatchState.matching_stop;
    }

    //bomb gen part 4
    public void LineBombCheck(MatchType matchType)
    {
        //Debug.Log($"LineBombCheck. Current: {gameBoardClass.currentElement}, Other: {gameBoardClass.currentElement.otherElement}");
        //move or not move?
        if (gameBoardClass.currentElement != null)
        {           
            if (gameBoardClass.currentElement.isMatched && gameBoardClass.currentElement.tag == matchType.color)
            {
                Debug.Log($"V1.step1");
                
                //unmatch
                gameBoardClass.currentElement.isMatched = false;

                float angle1 = 0;

                angle1 = gameBoardClass.currentElement.swipeAngle;

                if(angle1 == 0)
                {
                    Debug.Log($"V1.step2");
                    angle1 = UnityEngine.Random.Range(-135f, 135);                    
                }

                //for swipe
                if ((angle1 > -45 && angle1 <= 45) || (angle1 < -135 || angle1 >= 135))
                {
                    Debug.Log($"V1.step3");
                    gameBoardClass.currentElement.GenerateRowBomb();                    
                }
                else
                {
                    Debug.Log($"V1.step4");
                    gameBoardClass.currentElement.GenerateColumnBomb();
                }

            }
            else if (gameBoardClass.currentElement.otherElement != null)
            {
                Debug.Log($"V2.step1");
                ElementController otherDot = gameBoardClass.currentElement.otherElement.GetComponent<ElementController>();

                //if other dots matched
                if (otherDot.isMatched && otherDot.tag == matchType.color)
                {
                    otherDot.isMatched = false;

                    float angle2 = 0;

                    angle2 = gameBoardClass.currentElement.swipeAngle;

                    if (angle2 == 0)
                    {
                        Debug.Log($"V2.step2");
                        angle2 = UnityEngine.Random.Range(-135f, 135);
                    }

                    //for swipe
                    if ((angle2 > -45 && angle2 <= 45) || (angle2 < -135 || angle2 >= 135))
                    {
                        Debug.Log($"V2.step3");
                        otherDot.GenerateRowBomb();
                    }
                    else
                    {
                        Debug.Log($"V2.step4");
                        otherDot.GenerateColumnBomb();
                    }
                }
            }
            else
            {
                Debug.Log($"V3.step1");
                int Random = UnityEngine.Random.Range(0, 2);

                gameBoardClass.currentElement.isMatched = false;

                if (Random == 0)
                {
                    Debug.Log($"V3.step2");
                    gameBoardClass.currentElement.GenerateColumnBomb();
                }
                else
                {
                    Debug.Log($"V3.step3");
                    gameBoardClass.currentElement.GenerateRowBomb();
                }
            }
        }

        //Debug.Break();
    }

    //simple bomb logic
    public void MatchRowPieces(int row)
    {
        currentMatch.AddRange(GetRowPieces(row));
    }

    public void MatchColPieces(int col)
    {
        currentMatch.AddRange(GetColumnPieces(col));
        //Debug.Log($"MatchColPieces: {col}");
    }

    public void MatchWrapPieces(int col, int row)
    {
        currentMatch.AddRange(GetWrapPieces(col, row));
    }

    //for row bobm - step 3
    List<GameObject> GetRowPieces(int row)
    {
        List<GameObject> elements = new List<GameObject>();

        int rowBombCounter = 0;

        for (int i = 0; i < gameBoardClass.column; i++)
        {
            if (gameBoardClass.allElements[i, row] != null)
            {

                ElementController localElement = gameBoardClass.allElements[i, row].GetComponent<ElementController>();

                if (localElement.isColumnBomb)
                {
                    elements.Union(GetColumnPieces(i)).ToList();
                }

                //avoid bomb bug when many row bomb in 1 row Not combo!
                if (localElement.isRowBomb)
                {
                    rowBombCounter++;

                    if (rowBombCounter > 1)
                        localElement.isRowBomb = false;                    
                }

                elements.Add(gameBoardClass.allElements[i, row]);

                localElement.isMatched = true; //match here
                localElement.matchedByBomb = true;
            }
            else
            {
                if (gameBoardClass.blockerCells[i, row] != null)
                {
                    SpecialElements localBlocker = gameBoardClass.blockerCells[i, row].GetComponent<SpecialElements>();
                    localBlocker.isMatched = true;
                    //Debug.Log("isMatched Row blocker");
                }
            }

            //add for blockers
            /*            if (gameBoardClass.blockerCells[i, row] != null)
                        {
                            gameBoardClass.DamageBlockerAt(i, row);
                            Debug.Log("Damage blocker at GetRowPieces");
                        }*/
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

        int colBombCounter = 0;

        for (int i = 0; i < gameBoardClass.row; i++)
        {
            if (gameBoardClass.allElements[column, i] != null)
            {

                ElementController localElement = gameBoardClass.allElements[column, i].GetComponent<ElementController>();

                if (localElement.isRowBomb)
                {
                    elements.Union(GetRowPieces(i)).ToList();
                }

                //avoid bug with manu bombs in column
                if (localElement.isColumnBomb)
                {
                    colBombCounter++;

                    if (colBombCounter > 1)
                        localElement.isColumnBomb = false;
                }

                elements.Add(gameBoardClass.allElements[column, i]);

                localElement.isMatched = true; //match here
                localElement.matchedByBomb = true;

            }
            else
            {
                if (gameBoardClass.blockerCells[column, i] != null)
                {
                    SpecialElements localBlocker = gameBoardClass.blockerCells[column, i].GetComponent<SpecialElements>();
                    localBlocker.isMatched = true;
                    //Debug.Log("isMatched column blocker");
                }
            }


            //add for blockers
                /*            if (gameBoardClass.blockerCells[column, i] != null)
                            {               
                                gameBoardClass.DamageBlockerAt(column, i);
                                Debug.Log("Damage at GetColumnPieces");
                            }*/
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

                        ElementController localElement = gameBoardClass.allElements[i, j].GetComponent<ElementController>();
                        
                        //match
                        localElement.isMatched = true;
                        localElement.matchedByBomb = true;
                    }

                    //add for blockers
/*                    if (gameBoardClass.blockerCells[i, j] != null)
                    {
                        gameBoardClass.DamageBlockerAt(i, j);
                        Debug.Log("Damage at GetWrapPieces");
                    }*/
                }
            }
        }

        return elements;
    }

    //color bobmb part 2
    public List<Vector2> MatchColorPieces(string color)
    {
        colorBombElements.Clear();

        for (int i = 0; i < gameBoardClass.column; i++)
        {
            for (int j = 0; j < gameBoardClass.row; j++)
            {
                if (gameBoardClass.allElements[i, j] != null)
                {
                    if (gameBoardClass.allElements[i, j].tag == color)
                    {
                        ElementController elemControl = gameBoardClass.allElements[i, j].GetComponent<ElementController>();

                        //match
                        elemControl.isMatched = true;
                        elemControl.matchedByBomb = true;

                        Vector2 endPoint = new Vector2(elemControl.column, elemControl.row);                        
                        
                        //add to list
                        colorBombElements.Add(endPoint);

                        AddToListMatch(gameBoardClass.allElements[i, j].gameObject);
                    }
                }
            }
        }

        //Debug.Log(colorBombElements.Count);

        return colorBombElements; // Return the list
    }
}
