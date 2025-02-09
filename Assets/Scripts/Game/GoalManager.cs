using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.Win32.SafeHandles;
using UnityEngine.UI;
using System.Net;
using System.Xml.Linq;
using Unity.VisualScripting;
using System;


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

    private Canvas pnlGoalItems;

    //classes
    private GameBoard gameBoardClass;
    private EndGameManager endGameManagerClass;
    private SoundManager soundManagerClass;
    private UIManager uiManagerClass;

    public GameObject flyParticles;

    public AudioClip goalSound;

    private float waitingTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        //classes
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        endGameManagerClass = GameObject.FindWithTag("EndGameManager").GetComponent<EndGameManager>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();
        uiManagerClass = GameObject.FindWithTag("UIManager").GetComponent<UIManager>();

        //get canvas
        pnlGoalItems = goalGameParent.GetComponent<Canvas>();
        pnlGoalItems.overrideSorting = false;

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
            //ingame
            GameObject ingameGoal = Instantiate(goalPrefab, goalGameParent.transform.position, Quaternion.identity);
            ingameGoal.transform.SetParent(goalGameParent.transform);
            ingameGoal.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            ingameGoal.name = "goalprefab_" + i;

            GoalPanel gamePanel = ingameGoal.GetComponent<GoalPanel>();

            currentGoals.Add(gamePanel);

            gamePanel.thisSprite = levelGoals[i].goalSprite;
            gamePanel.thisString = "" + levelGoals[i].numberGoalsNeeded; //goals 
        }
    }

    public void UpdateGoals()
    {
        int goalsCompleted = 0;

        //hide and show goals        
        pnlGoalItems.overrideSorting = false;

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

        if (gameBoardClass.matchState == MatchState.matching_stop && gameBoardClass.currentState != GameState.wait)
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
            pnlGoalItems.overrideSorting = true;
            StartCoroutine(DelayedLose());
        }
    }

    private IEnumerator DelayedWin()
    {
        // Code to run before WinGame
        gameBoardClass.currentState = GameState.win;

        // info
        uiManagerClass.ShowInGameInfo("Level Completed!", true, 1, ColorPalette.Colors["GreenSaturate"]);

        yield return new WaitForSeconds(waitingTime); // Wait for ... second

        // Call WinGame after delay
        endGameManagerClass.WinGame();
        
    }

    private IEnumerator DelayedLose()
    {
        // Code to run before LoseGame
        gameBoardClass.currentState = GameState.lose;

        // info
        uiManagerClass.ShowInGameInfo("Out of moves!", true, 2, ColorPalette.Colors["VioletMed"]);

        yield return new WaitForSeconds(waitingTime); // Wait for ... second

        // Call LoseGame after delay
        endGameManagerClass.LoseGame();        
    }

    public void CompareGoal(string goalToCompare, int Column = -1, int Row = -1)
    {
        for (int i = 0; i < levelGoals.Length; i++)
        {
            if (goalToCompare == levelGoals[i].matchValue)
            {
                levelGoals[i].numberCollectedGoals++;

                //play sound
                if(goalSound != null)
                    soundManagerClass.PlaySound(goalSound);

                //run text animation
                StartCoroutine(SmoothScaleText(currentGoals[i].thisText.transform));

                //for vfx
                InstantiateAndMove(i, Column, Row);
            }
        }
    }

    public void InstantiateAndMove(int prefabNumber, int Column = -1, int Row = -1)
    {
        //Debug.Log(Column + "/" + Row);
        
        //get position
        Vector3 startPoint = new Vector3(Column, Row, 0);
        Vector3 endPoint = new Vector3(Column, 16, 0);

        //generate empty object
        GameObject flyingGoal = new GameObject("EmptyObject");
        flyingGoal.transform.position = startPoint; // Set to origin
        flyingGoal.name = "part_" + currentGoals[prefabNumber].name +"_"+ levelGoals[prefabNumber].numberCollectedGoals;
        flyingGoal.layer = LayerMask.NameToLayer("Default");

        //get and setup sprite
        SpriteRenderer spriteRenderer = flyingGoal.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = currentGoals[prefabNumber].thisSprite;
        spriteRenderer.sortingLayerName = "Elements"; // Replace with your sorting layer name
        spriteRenderer.sortingOrder = 5;

        //create particles
        GameObject elementParticle = Instantiate(flyParticles, startPoint, Quaternion.identity);
        elementParticle.name = flyingGoal.name + "flypart_";
        elementParticle.transform.SetParent(flyingGoal.transform);


        float arcHeight = 1f; // Height of the arc        

        //start fly
        StartCoroutine(MoveAlongArc(flyingGoal, elementParticle, startPoint, endPoint, arcHeight, 1.0f));
    }


    private IEnumerator MoveAlongArc(GameObject rootObject, GameObject particles, Vector3 startPoint, Vector3 endPoint, float arcHeight, float duration)
    {
        float time = 0;

        // Set the target scale to 0 (start scale is 1)
        Vector3 startScale = Vector3.one;  // Start with scale of (1, 1, 1)
        Vector3 targetScale = Vector3.one * 0.5f;

        while (time < duration)
        {
            time += Time.deltaTime;

            // Interpolation value
            float t = time / duration;

            // Horizontal interpolation (linear)
            float x = Mathf.Lerp(startPoint.x, endPoint.x, t);

            // Vertical interpolation (parabolic arc)
            float y = Mathf.Lerp(startPoint.y, endPoint.y, t) + arcHeight * Mathf.Sin(Mathf.PI * t);

            // Depth interpolation (linear)
            float z = Mathf.Lerp(startPoint.z, endPoint.z, t);

            // Interpolate scale from (1, 1, 1) to (0, 0, 0)
            Vector3 currentScale = Vector3.Lerp(startScale, targetScale, t);

            // Update the position
            rootObject.transform.position = new Vector3(x, y, z);

           // objectMy.transform.rotation = Quaternion.Euler(0, 0, currentRotation);  // Assuming you want to rotate around the Y-axis
            rootObject.transform.localScale = currentScale;  // Apply scale

            yield return null;
        }

        //Debug.Break();

        Destroy(particles);
        Destroy(rootObject);        
    }

    //for goals
    private IEnumerator SmoothScaleText(Transform textTransform)
    {
        Vector3 startScale= new Vector3(1f, 1.5f, 1f);
        
        float elapsedTime = 0f;
        Vector3 targetScale = new Vector3(1f, 1f, 1f);
        float scaleDuration = 0.2f;  // Reduced duration for faster scaling

        // Wait for 1 second before scaling back
        yield return new WaitForSeconds(0.05f);

        while (elapsedTime < scaleDuration)
        {
            textTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / scaleDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        textTransform.localScale = new Vector3(1f, 1f, 1f);
    }

}
