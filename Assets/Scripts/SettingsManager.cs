using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    //classes
    private GameData gameDataClass;
    private SoundManager soundManagerClass;    
    private GameBoard gameBoardClass;

    [Header("Screens")]
    public GameObject settingsScreen;
    public GameObject quitScreen;

    public bool paused = false;

    public string sceneName;

    [Header("Sound")]
    public Button soundButton;
    public Slider soundSlider;
    public TMP_Text soundValueTxt;
    public Sprite[] soundButtonSprites;

    [Header("Music")]
    public Button musicButton;
    public Slider musicSlider;
    public TMP_Text musicValueTxt;
    public Sprite[] musicButtonSprites;


    private void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        sceneName = currentScene.name;

        //classes
        if (sceneName == "GameBoard")
        {
            gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        }

        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

        if (gameDataClass != null)
        {
            LoadData();
        }

        // Update the text value
        UpdateTextValue();
    }


    void UpdateTextValue()
    {
        if (soundValueTxt != null)
        {
            // Convert the slider value (0.0 to 1.0) to percentage (0% to 100%)
            int percentage = Mathf.RoundToInt(soundSlider.value * 100);

            if (percentage > 0 && gameDataClass.saveData.soundToggle)
            {
                soundValueTxt.text = percentage + "%";
            }
            else
            {
                soundValueTxt.text = "Muted";
            }
        }

        if (musicValueTxt != null)
        {
            // Convert the slider value (0.0 to 1.0) to percentage (0% to 100%)
            int percentage = Mathf.RoundToInt(musicSlider.value * 100);
            
            if (percentage > 0 && gameDataClass.saveData.musicToggle)
            {
                musicValueTxt.text = percentage + "%";
            }
            else
            {
                musicValueTxt.text = "Muted";
            }            
        }
    }

    public void SoundToggle()
    {
        bool toggle = gameDataClass.saveData.soundToggle;

        if (toggle)
        {
            gameDataClass.saveData.soundToggle = false;
            soundButton.image.sprite = soundButtonSprites[1];
            soundManagerClass.MuteSound(false);
        }
        else
        {
            gameDataClass.saveData.soundToggle = true;
            soundButton.image.sprite = soundButtonSprites[0];
            soundManagerClass.MuteSound(true);
        }

        UpdateTextValue();
    }

    public void MusicToggle()
    {
        bool toggle = gameDataClass.saveData.musicToggle;

        if (toggle)
        {
            gameDataClass.saveData.musicToggle = false;
            musicButton.image.sprite = musicButtonSprites[1];
            soundManagerClass.MuteMusic(false);
        }
        else
        {
            gameDataClass.saveData.musicToggle = true;
            musicButton.image.sprite = musicButtonSprites[0];
            soundManagerClass.MuteMusic(true);            
        }

        UpdateTextValue();
    }


    public void OnSoundSliderChanged()
    {
        gameDataClass.saveData.soundVolume = soundSlider.value;
        soundManagerClass.SetVolume("sound");
        UpdateTextValue();
    }

    public void OnMuscSliderChange()
    {
        gameDataClass.saveData.musicVolume = musicSlider.value;
        soundManagerClass.SetVolume("music");
        UpdateTextValue();
    }


    public void PauseGame()
    {
        paused = !paused;

        if (paused)
        {
            gameBoardClass.currentState = GameState.pause;
        }

        if (!paused && gameBoardClass.currentState != GameState.win)
        {
            if (gameBoardClass.currentState != GameState.lose)
                gameBoardClass.currentState = GameState.move;
        }
    }

    private void LoadData()
    {        
        //set sound
        if (!gameDataClass.saveData.soundToggle)                       
        {
            soundButton.image.sprite = soundButtonSprites[1];            
        }

        //set music
        if (!gameDataClass.saveData.musicToggle)
        {
            musicButton.image.sprite = musicButtonSprites[1];
        }

        //load volume
        soundSlider.value = gameDataClass.saveData.soundVolume;
        musicSlider.value = gameDataClass.saveData.musicVolume;
    }


    public void SwitchScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }


    public void WinConfirm()
    {
        if (gameDataClass != null)
        {
            gameDataClass.saveData.isActive[gameBoardClass.level + 1] = true;
            gameDataClass.SaveToFile();
        }
    }

}
