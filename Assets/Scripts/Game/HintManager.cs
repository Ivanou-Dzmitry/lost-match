using System;
using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;

    public float hintDelay;
    private float hintDelaySec;
    public GameObject hintParticle;
    public GameObject currentHint;

    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        hintDelaySec = hintDelay;
    }

    void Update()
    {
        hintDelaySec -= Time.deltaTime;

        if (hintDelaySec <= 0 && currentHint == null)
        {
            if (gameBoardClass.matchState == MatchState.matching_stop && gameBoardClass.currentState == GameState.move)
            {
                MarkHint();
                hintDelaySec = hintDelay;
            }
            else
            {
                DestroyHint();
                hintDelaySec = hintDelay;
            }
        }            
    }

    //all posible matches
    List<GameObject> FindAllMatches()
    {
        List<GameObject> possibleMatches = new List<GameObject>();

        for (int i = 0; i < gameBoardClass.column; i++)
        {
            for (int j = 0; j < gameBoardClass.row; j++)
            {
                if (gameBoardClass.allElements[i, j] != null)
                {
                    if (i < gameBoardClass.column - 1)
                    {
                        if (gameBoardClass.SwithAndCheck(i, j, Vector2.right))
                        {
                            possibleMatches.Add(gameBoardClass.allElements[i, j]);
                        }

                    }

                    if (j < gameBoardClass.row - 1)
                    {
                        if (gameBoardClass.SwithAndCheck(i, j, Vector2.up))
                        {
                            possibleMatches.Add(gameBoardClass.allElements[i, j]);
                        }
                    }
                }
            }
        }

        return possibleMatches;

    }

    //pick match
    GameObject PickRandomMatch()
    {
        List<GameObject> possibleMoves = new List<GameObject>();

        possibleMoves.Clear();

        if (gameBoardClass.matchState == MatchState.matching_stop)
            possibleMoves = FindAllMatches();

        if (possibleMoves.Count > 0 && gameBoardClass.currentState == GameState.move && gameBoardClass.matchState == MatchState.matching_stop)
        {
            int pieceToUse = UnityEngine.Random.Range(0, possibleMoves.Count);

            return possibleMoves[pieceToUse];
        }

        return null;
    }

    //create hint
    private void MarkHint()
    {
        GameObject move = PickRandomMatch();

        string currentTime = DateTime.Now.ToString("mmss");

        if (move != null && gameBoardClass.currentState == GameState.move && gameBoardClass.matchState == MatchState.matching_stop)
        {
            currentHint = Instantiate(hintParticle, move.transform.position, Quaternion.identity);

            currentHint.name = "hint_" + currentTime;

            //set properties
            currentHint.transform.parent = gameBoardClass.gameArea.transform;
        }
    }

    public void DestroyHint()
    {
        if (currentHint != null)
        {
            Destroy(currentHint);
            currentHint = null;
            hintDelaySec = hintDelay;
        }
    }

}
