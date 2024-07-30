using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class BlankGoalClass
{
    public int numberGoalsNeeded;
    public int numberCollectedGoals;
    public Sprite goalSprite;
    public string matchValue;
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

        for (int i = 0; i < levelGoals.Length; i++)
        {            
            currentGoals[i].thisText.text = "" + (levelGoals[i].numberGoalsNeeded - levelGoals[i].numberCollectedGoals);

            if (levelGoals[i].numberCollectedGoals >= levelGoals[i].numberGoalsNeeded)
            {
                goalsCompleted++;         
                currentGoals[i].thisText.text = "";
                currentGoals[i].thisCheck.enabled = true; //turn check ON               
            }

            if (goalsCompleted >= levelGoals.Length)
            {
                if (endGameManagerClass != null)
                {
                    endGameManagerClass.WinGame();
                }
            }
        }
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
