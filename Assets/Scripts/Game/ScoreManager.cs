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
    public TMP_Text scoreText;

    public Slider scoreBar;
    public Image[] levelStars;
    public Sprite[] levelStarsSpite;

    private float[] starPosition;

    private bool update;

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


        for (int i = 0; i < levelStars.Length; i++)
        {
            levelStars[i].sprite = levelStarsSpite[1];

        }

        if (scoreText.text != null)
            scoreText.text = "" + gameDataClass.saveData.credits;


        // Cache the RectTransform of the score bar
        RectTransform rectTransformSlide = scoreBar.GetComponent<RectTransform>();
        float sliderWidth = rectTransformSlide.rect.width;

        // Get the max value from scoreGoals
        int maxValue = gameBoardClass.scoreGoals[gameBoardClass.scoreGoals.Length - 1];

        // Calculate one percent of the slider width (used for position calculation)
        float onePercent = sliderWidth / maxValue;

        // Cache the length of levelStars to avoid repeated calls in the loop
        int starCount = levelStars.Length;

        // Pre-allocate starPosition array with size based on the starCount
        float[] starPosition = new float[starCount];

        for (int i = 0; i < starCount; i++)
        {
            // Calculate the position for the star based on the score goal
            starPosition[i] = onePercent * gameBoardClass.scoreGoals[i];

            // Cache RectTransform for each star (avoid GetComponent inside the loop)
            RectTransform rectTransformStar = levelStars[i].GetComponent<RectTransform>();

            // Calculate new position considering the image width
            float imageWidth = rectTransformStar.rect.width/2;
            Vector3 currentPosition = rectTransformStar.localPosition;

            // Update the position of the star
            rectTransformStar.localPosition = new Vector3(starPosition[i] - imageWidth, currentPosition.y, currentPosition.z);
        }


        UpdateBar();

    }



    public void IncreaseScore(int amountToIncrease)
    {
        score += amountToIncrease; //score

        gameDataClass.saveData.credits += amountToIncrease;
       
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

        update = true;
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
        //update score bar
        if (gameBoardClass?.scoreGoals != null && scoreBar != null && gameBoardClass.scoreGoals.Length > 0)
        {
            // Update score bar using the last element of scoreGoals
            scoreBar.value = (float)score / gameBoardClass.scoreGoals[^1];
        }

        // Get the Image component of the fillRect
        if (scoreBar != null)
        {
            Image fillImage = scoreBar.fillRect.GetComponent<Image>();

            // Hide the fillRect image if credits are 0, otherwise show it
            if (fillImage != null)
            {
                fillImage.enabled = score > 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {        
        if (update)
        {
            scoreText.text = "" + gameDataClass.saveData.credits; //show score
            update = false;
        }
        
    }
}
