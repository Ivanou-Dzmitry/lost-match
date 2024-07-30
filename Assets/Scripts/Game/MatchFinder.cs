using System.Collections;
using System.Collections.Generic;
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

    //for match
    public void FindAllMatches()
    {
        StartCoroutine(FindAllMatchesCo());
    }

    private void AddToListMatch(GameObject element)
    {
        if (!currentMatch.Contains(element))
        {
            currentMatch.Add(element); //add to match list
        }
     
        //mark as matched
        element.GetComponent<ElementController>().isMatched = true;

        element.GetComponent<SpriteRenderer>().color = new Color(0, 1f, 0, 0.75f); //for debug

        // Remove all null elements
        currentMatch.RemoveAll(item => item == null);
    }

    private void GetNearbyPieces(GameObject element1, GameObject element2, GameObject element3)
    {
        if (element1 != null)
            AddToListMatch(element1);

        if (element2 != null)
            AddToListMatch(element2);

        if (element3 != null)
            AddToListMatch(element3);
    }

    private IEnumerator FindAllMatchesCo()
    {
        yield return null;

        for (int i = 0; i < gameBoardClass.column; i++)
        {
            for (int j = 0; j < gameBoardClass.row; j++)
            {
                GameObject currentElement = gameBoardClass.allElements[i, j]; //store board

                if (currentElement != null)
                {
                      //horizontal
                    if (i > 0 && i < gameBoardClass.column - 1)
                    {
                        //get neiborhood
                        GameObject leftElement = gameBoardClass.allElements[i - 1, j];
                        GameObject rightElement = gameBoardClass.allElements[i + 1, j];

                        if (leftElement != null && rightElement != null)
                        {
                            if (leftElement != null && rightElement != null)
                            {
                                if (leftElement.tag == currentElement.tag && rightElement.tag == currentElement.tag) //compare tags form lr dots
                                {
                                    GetNearbyPieces(leftElement, currentElement, rightElement);
                                }
                            }
                        }
                    }

                    //vertical
                    if (j > 0 && j < gameBoardClass.row - 1)
                    {
                        GameObject upElement = gameBoardClass.allElements[i, j + 1];
                        GameObject downElement = gameBoardClass.allElements[i, j - 1];

                        if (upElement != null && downElement != null)
                        {
                            if (upElement != null && downElement != null)
                            {
                                if (upElement.tag == currentElement.tag && downElement.tag == currentElement.tag)
                                {
                                    GetNearbyPieces(upElement, currentElement, downElement);
                                }
                            }
                        }
                    }
                }
            }
        }
    }



}
