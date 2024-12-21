using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.Win32.SafeHandles;


[System.Serializable]
public class BlankGoalClass
{
    public int numberGoalsNeeded;
    public int numberCollectedGoals;
    public Sprite goalSprite;
    public string matchValue;

    // Method to clear the class
    public void Clear()
    {
        numberGoalsNeeded = 0;
        numberCollectedGoals = 0;
        goalSprite = null; // Set reference types to null
        matchValue = null; // Or you can set to an empty string "" if you prefer
    }
}

public class GoalManager : MonoBehaviour
{
    public BlankGoalClass[] levelGoals;
    public List<GoalPanel> currentGoals = new List<GoalPanel>();

    //UI
    public GameObject goalPrefab;
    //public GameObject goalIntroParent;
    public GameObject goalGameParent;

    //classes
    private GameBoard gameBoardClass;
    private EndGameManager endGameManagerClass;

    [Header("Final Text")]
    public GameObject finalTextPanel;
    public TMP_Text finalText;

    private float waitingTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        endGameManagerClass = GameObject.FindWithTag("EndGameManager").GetComponent<EndGameManager>();

        GetGoals();
        SetupIntroGoals();
    }

    void GetGoals()
    {
        if (gameBoardClass != null)
        {
            if (gameBoardClass.level < gameBoardClass.worldClass.levels.Length)
            {
                if (gameBoardClass.worldClass.levels[gameBoardClass.level] != null)
                {
                    levelGoals = gameBoardClass.worldClass.levels[gameBoardClass.level].levelGoals;

                    //reset goals
                    for (int i = 0; i < levelGoals.Length; i++)
                    {
                        levelGoals[i].numberCollectedGoals = 0;
                    }
                }
            }
        }
    }

    void SetupIntroGoals()
    {
        for (int i = 0; i < levelGoals.Length; i++)
        {
/*            //intro prefabs
            GameObject introGoal = Instantiate(goalPrefab, goalIntroParent.transform.position, Quaternion.identity);
            introGoal.transform.SetParent(goalIntroParent.transform);
            introGoal.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            GoalPanel introPanel = introGoal.GetComponent<GoalPanel>();
            introPanel.thisSprite = levelGoals[i].goalSprite;
            introPanel.thisString = "" + levelGoals[i].numberGoalsNeeded; //goals */

            //ingame
            GameObject ingameGoal = Instantiate(goalPrefab, goalGameParent.transform.position, Quaternion.identity);
            ingameGoal.transform.SetParent(goalGameParent.transform);
            ingameGoal.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            GoalPanel gamePanel = ingameGoal.GetComponent<GoalPanel>();

            currentGoals.Add(gamePanel);

            gamePanel.thisSprite = levelGoals[i].goalSprite;
            gamePanel.thisString = "" + levelGoals[i].numberGoalsNeeded; //goals 
        }
    }

    public void UpdateGoals()
    {
        int goalsCompleted = 0;

        Canvas pnlC = goalGameParent.GetComponent<Canvas>();

        pnlC.overrideSorting = false;

        for (int i = 0; i < levelGoals.Length; i++)
        {           
            //count on item
            currentGoals[i].thisText.text = "" + (levelGoals[i].numberGoalsNeeded - levelGoals[i].numberCollectedGoals);

            //turn on check mark
            if (levelGoals[i].numberCollectedGoals >= levelGoals[i].numberGoalsNeeded)
            {
                goalsCompleted++;         
                currentGoals[i].thisText.text = "";
                currentGoals[i].thisCheck.enabled = true; //turn check ON               
            }
        }

        //state for run
        bool runState = false;

        if (gameBoardClass.matchState == GameState.matching_stop && gameBoardClass.currentState != GameState.wait)
            runState = true;


        //end game procedure only when match is stop: goals=yes,
        if (goalsCompleted >= levelGoals.Length && runState)
        {
            if (endGameManagerClass != null)
            {
                StartCoroutine(DelayedWin());
            }
        }

        //for end game: moves = 0, goals=no
        if (endGameManagerClass.curCounterVal <= 0 && goalsCompleted < levelGoals.Length && runState)
        {
            pnlC.overrideSorting = true;
            StartCoroutine(DelayedLose());
        }
    }

    public void ShowInGameInfo(string infoText, bool showPanel)
    {
        if (showPanel)
        {
            finalTextPanel.SetActive(true);
            finalText.text = infoText;
        }
        else
        {
            finalTextPanel.SetActive(false);
            finalText.text = "";
        }

        //hide panel
        if (finalTextPanel != null && finalTextPanel.activeSelf)
        {
            StartCoroutine(HidePanelCoroutine(waitingTime));
        }
    }

    private IEnumerator HidePanelCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay        

        finalTextPanel.SetActive(false); // Hide the panel
    }

    private IEnumerator DelayedWin()
    {
        // Code to run before WinGame
        gameBoardClass.currentState = GameState.win;

        ShowInGameInfo("Level Completed!", true);

        yield return new WaitForSeconds(waitingTime); // Wait for ... second

        // Call WinGame after delay
        endGameManagerClass.WinGame();
        
    }

    private IEnumerator DelayedLose()
    {
        // Code to run before LoseGame
        gameBoardClass.currentState = GameState.lose;

        ShowInGameInfo("Out of moves!", true);

        yield return new WaitForSeconds(waitingTime); // Wait for ... second

        // Call LoseGame after delay
        endGameManagerClass.LoseGame();        
    }

    private void PanelActivator()
    {
        finalTextPanel.SetActive(false); // Completely disable the panel
    }

    public void CompareGoal(string goalToCompare)
    {
        for (int i = 0; i < levelGoals.Length; i++)
        {
            if (goalToCompare == levelGoals[i].matchValue)
            {
                levelGoals[i].numberCollectedGoals++;
            }
        }
    }
}
