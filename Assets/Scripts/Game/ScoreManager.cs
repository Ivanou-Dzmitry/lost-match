using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;
    private GameData gameDataClass;

    public int score;
    private int numberStars;
    private int scoreBarLenght;
    public TMP_Text scoreText;

    public Slider scoreBar;
    public Image[] levelStars;
    public Sprite[] levelStarsSpite;

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        GameObject gameDataObject = GameObject.FindWithTag("GameData");
        gameDataClass = gameDataObject.GetComponent<GameData>();


        if (gameDataClass != null)
        {
            gameDataClass.LoadFromFile();
        }

        //get score
        if (gameBoardClass != null)
        {
            GetScoreData();
        }

        for (int i = 0; i < levelStars.Length; i++)
        {
            levelStars[i].sprite = levelStarsSpite[1];

        }
    }

    private void GetScoreData()
    {
        int scoreGoalsLength = gameBoardClass.scoreGoals.Length;

        scoreBarLenght = 0;

        for (int i = 0; i < scoreGoalsLength; i++)
        {
            scoreBarLenght = scoreBarLenght + gameBoardClass.scoreGoals[i];
        }
    }

    public void IncreaseScore(int amountToIncrease)
    {
        score += amountToIncrease; //score

        //for stars
        for (int i = 0; i < gameBoardClass.scoreGoals.Length; i++)
        {
            if (score >= gameBoardClass.scoreGoals[i] && numberStars < i + 1)
            {
                numberStars++;
            }
        }

        //turn on stars
        for (int i = 0; i < numberStars; i++)
        {
            levelStars[i].sprite = levelStarsSpite[0];
        }

        if (gameDataClass != null)
        {
            int hiScore = gameDataClass.saveData.highScore[gameBoardClass.level];


            if (score > hiScore)
            {
                gameDataClass.saveData.highScore[gameBoardClass.level] = score;
            }

            int currentStarsCount = gameDataClass.saveData.stars[gameBoardClass.level];

            if (numberStars > currentStarsCount)
            {
                gameDataClass.saveData.stars[gameBoardClass.level] = numberStars;
            }

            gameDataClass.SaveToFile();
        }

        UpdateBar();

    }

    private void OnApplicationPause()
    {
        if (gameDataClass != null)
        {           
            gameDataClass.SaveToFile();
        }
    }

    private void UpdateBar()
    {
        if (gameBoardClass != null && scoreBar != null)
        {
            int scoreGoalsLength = gameBoardClass.scoreGoals.Length;

            scoreBar.value = (float)score / (float)gameBoardClass.scoreGoals[scoreGoalsLength - 1];
        }
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = "Score: " + score.ToString(); //show score
    }
}
