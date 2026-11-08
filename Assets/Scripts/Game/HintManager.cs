using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;

    public float hintDelay;
    private float hintDelaySec;
    public GameObject hintParticle;
    public GameObject currentHint;          // 1st step: the piece to move
    private GameObject partnerHint;         // 2nd step: the piece to swap it with
    private Coroutine hintCoroutine;

    private bool hasMove;
    private GameBoard.HintMove shownMove;
    private float partnerDelaySec;
    private Vector2Int lastHintCell = new Vector2Int(-1, -1);

    // a hint is taken from the moves whose score is not far from the best one
    private const float GoodMoveShare = 0.85f;
    private const int MaxChoices = 3;

    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        hintCoroutine = StartCoroutine(HintChecker());
    }

    private bool BoardIsQuiet()
    {
        return gameBoardClass.currentState == GameState.move && gameBoardClass.matchState == MatchState.matching_stop;
    }

    //pick a good move: the best ones for the goals of the level, not always the same one
    private bool PickMove(out GameBoard.HintMove move)
    {
        move = default;

        if (!BoardIsQuiet())
            return false;

        List<GameBoard.HintMove> moves = gameBoardClass.FindHintMoves();
        if (moves.Count == 0)
            return false;

        moves.Sort((a, b) => b.score.CompareTo(a.score));

        float limit = moves[0].score * GoodMoveShare;
        List<GameBoard.HintMove> choices = moves.Where(m => m.score >= limit).Take(MaxChoices).ToList();

        // not the same piece as the last hint, if there is a choice
        List<GameBoard.HintMove> fresh = choices.Where(m => m.column != lastHintCell.x || m.row != lastHintCell.y).ToList();
        if (fresh.Count > 0)
            choices = fresh;

        move = choices[UnityEngine.Random.Range(0, choices.Count)];
        return true;
    }

    private GameObject ShowAt(int column, int row, string label)
    {
        GameObject element = gameBoardClass.allElements[column, row];
        if (element == null || hintParticle == null)
            return null;

        GameObject hint = Instantiate(hintParticle, element.transform.position, Quaternion.identity);
        hint.name = "hint_" + label + "_" + DateTime.Now.ToString("mmss");
        hint.transform.parent = gameBoardClass.gameArea.transform;
        return hint;
    }

    //create hint: first the piece to move, a little later the piece to swap it with
    private void MarkHint()
    {
        if (!PickMove(out GameBoard.HintMove move))
            return;

        currentHint = ShowAt(move.column, move.row, "a");
        if (currentHint == null)
            return;

        shownMove = move;
        hasMove = true;
        lastHintCell = new Vector2Int(move.column, move.row);
        partnerDelaySec = hintDelay;
    }

    private void ShowPartner()
    {
        if (!hasMove || partnerHint != null)
            return;

        int column = shownMove.column + shownMove.direction.x;
        int row = shownMove.row + shownMove.direction.y;

        if (column < gameBoardClass.column && row < gameBoardClass.row)
            partnerHint = ShowAt(column, row, "b");
    }

    public void DestroyHint()
    {
        if (currentHint != null)
        {
            Destroy(currentHint);
            currentHint = null;
            hintDelaySec = hintDelay;
        }

        if (partnerHint != null)
        {
            Destroy(partnerHint);
            partnerHint = null;
        }

        hasMove = false;
    }


    IEnumerator HintChecker()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f); // check every 250ms instead of every frame

            // a hint that is on the screen: the board must stay quiet, and after a while the second piece is shown
            if (currentHint != null)
            {
                if (!BoardIsQuiet())
                {
                    DestroyHint();
                }
                else if (partnerHint == null)
                {
                    partnerDelaySec -= 0.25f;
                    if (partnerDelaySec <= 0)
                        ShowPartner();
                }

                continue;
            }

            if (hintDelaySec > 0)
            {
                hintDelaySec -= 0.25f;
            }

            if (hintDelaySec <= 0)
            {
                if (BoardIsQuiet())
                {
                    MarkHint();
                }
                else
                {
                    DestroyHint();
                }

                hintDelaySec = hintDelay;
            }
        }
    }

}
