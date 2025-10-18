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
    public int numberStars;
    public TMP_Text scoreText;

    public Slider scoreBar;
    public Image[] levelStars;
    public Sprite[] levelStarsSpite;

    public GameLog log;

    public GameObject tutorPanel;

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

        //star position on progress bar
        for (int i = 0; i < starCount; i++)
        {
            // Calculate the position for the star based on the score goal
            if (i >= 0 && i < starPosition.Length && i < gameBoardClass.scoreGoals.Length)
            {
                starPosition[i] = onePercent * gameBoardClass.scoreGoals[i];
            }
            else
            {
                log.WriteSysLog($"Index {i} out of bounds. starPosition.Length={starPosition.Length}, scoreGoals.Length={gameBoardClass.scoreGoals.Length}");
            }

            // Cache RectTransform for each star (avoid GetComponent inside the loop)
            RectTransform rectTransformStar = levelStars[i].GetComponent<RectTransform>();

            // Calculate new position considering the image width
            float imageWidth = rectTransformStar.rect.width/2;
            
            Vector3 currentPosition = rectTransformStar.localPosition;

            // Update the position of the star on the progress bar
            float x = starPosition[i];

            Vector3 newPos = new Vector3(x, currentPosition.y, currentPosition.z);

            if (!float.IsNaN(newPos.x) && !float.IsNaN(newPos.y) && !float.IsNaN(newPos.z))
            {
                rectTransformStar.localPosition = newPos;
            }
            else
            {
                log.WriteSysLog($"Invalid position for Star01Image: {newPos} (i={i}, starPos={starPosition[i]}, imageWidth={imageWidth})");
            }

        }

        UpdateBar();
    }

    public void IncreaseScore(int amountToIncrease)
    {
        score += amountToIncrease; //score

        //reduce because 0=1 in list Important!
        int levelInList = Mathf.Max(0, gameBoardClass.loadedLevel - 1);

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

        //progress bar with stars
        UpdateBar();

        //close tutor panel
        tutorPanel.SetActive(false);
    }

    private void OnApplicationPause()
    {
        if (gameDataClass != null)
        {           
            gameDataClass.SaveToFile();
        }
    }

    //progress bar
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

}
