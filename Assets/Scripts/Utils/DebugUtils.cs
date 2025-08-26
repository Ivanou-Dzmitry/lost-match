using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugUtils : MonoBehaviour
{
    private GameData gameDataClass;
    public TMP_Text logText;
    public string sceneName;

    // Start is called before the first frame update
    void Start()
    {
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
    }

    public void ScenLoader()
    {
        if(sceneName.Length > 0)
            SceneManager.LoadScene(sceneName);
    }

    public void ResetToDefaults()
    {
        gameDataClass.AddDefaultData();

        logText.text = "Reset complete!";
    }

    public void DebugAddCredits()
    {
        gameDataClass.saveData.credits = 10000;
        gameDataClass.SaveToFile();

        logText.text = "Added 10.000 credits";
    }

    public void DebugZeroCredit()
    {
        gameDataClass.saveData.credits = 13;
        gameDataClass.SaveToFile();

        logText.text = "";

        logText.text = "Credits redused to 13";
    }

    public void DebugZeroLife()
    {
        gameDataClass.saveData.bonuses[5] = 1; //set lives bonus 0
        gameDataClass.SaveToFile();
        logText.text = "Life redused to 1";
    }


    public void DebugOpenLevels()
    {
        for (int i = 0; i < gameDataClass.saveData.isActive.Length; i++)
        {
            gameDataClass.saveData.isActive[i] = true;
        }

        gameDataClass.SaveToFile();
        logText.text = "All levels are opened";
    }

}
