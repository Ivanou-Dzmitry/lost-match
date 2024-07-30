using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;
    private GameData gameDataClass;

    public int score;
    public TMP_Text scoreText;

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
    }

    public void IncreaseScore(int amountToIncrease)
    {
        score += amountToIncrease; //score
    }

    private void OnApplicationPause()
    {
        if (gameDataClass != null)
        {           
            gameDataClass.SaveToFile();
        }
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = "Score: " + score.ToString(); //show score
    }
}
