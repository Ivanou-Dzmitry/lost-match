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

        //get safe area values
        Rect safeArea = Screen.safeArea;

        float safeAreaHeight = safeArea.height;

        int screenHeight = Screen.height;

        float unsafeAreaHeight = screenHeight - safeAreaHeight;

        //Debug.Log($"Screen Width: {unsafeAreaHeight}, Screen Height: {safeAreaHeight}");


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

    private void Update()
    {

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

    public void SoundButton()
    {
        ToggleAudio(
            ref gameDataClass.saveData.soundToggle,
            soundButton,
            soundButtonSprites,
            gameDataClass.saveData.soundVolume,
            MuteSound,
            soundValueTxt
        );
    }

    public void MusicButton()
    {
        ToggleAudio(
            ref gameDataClass.saveData.musicToggle,
            musicButton,
            musicButtonSprites,
            gameDataClass.saveData.musicVolume,
            MuteMusic,
            musicValueTxt
        );
    }

    private void ToggleAudio(
        ref bool toggle,
        Button button,
        Sprite[] sprite,
        float volume,
        Action<float> muteFunction,
        TMP_Text valueText)
    {
        if (!toggle)
        {
            toggle = true;
            button.image.sprite = sprite[0];
            muteFunction(volume);
        }
        else
        {
            toggle = false;
            button.image.sprite = sprite[1];
            muteFunction(0);
            valueText.text = "Muted";
        }
    }


    public void SoundVolume()
    {                
        if (gameDataClass.saveData.soundToggle == true)
        {
            gameDataClass.saveData.soundVolume = soundSlider.value;
            soundManagerClass.SetVolume("sound");
            MuteSound(soundSlider.value);

            if (soundSlider.value <= 0.01f)
            {
                SoundButton();
                soundButton.image.sprite = soundButtonSprites[1];
            }
            else
            {
                gameDataClass.saveData.soundToggle = true;
                soundButton.image.sprite = soundButtonSprites[0];
            }

            UpdateTextValue();
        }
        else
        {
            if(soundSlider.value > 0.01f)
                gameDataClass.saveData.soundToggle = true;
        }
    }

    public void MusicVolume()
    {
        if (gameDataClass.saveData.musicToggle == true)
        {
            gameDataClass.saveData.musicVolume = musicSlider.value;
            soundManagerClass.SetVolume("music");
            MuteMusic(musicSlider.value);

            if (musicSlider.value <= 0.01f)
            {
                MusicButton();
                musicButton.image.sprite = musicButtonSprites[1];
            }
            else
            {
                gameDataClass.saveData.musicToggle = true;
                musicButton.image.sprite = musicButtonSprites[0];
            }

            UpdateTextValue();
        }
        else
        {
            if (musicSlider.value > 0.01f)
                gameDataClass.saveData.musicToggle = true;
        }

    }

    public void MuteSound(float sliderValue)
    {
        // Find the GameObject with the "EffectSource" tag
        GameObject gameObject = GameObject.FindWithTag("EffectSource");

        if (gameObject != null)
        {
            // Get the AudioSource component
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();

            if (audioSource != null)
            {
                // Mute the AudioSource if slider value is 0%, unmute otherwise
                audioSource.mute = sliderValue <= 0.01f;
                UpdateTextValue(); 
            }
        }
    }

    public void MuteMusic(float sliderValue)
    {
        // Find the GameObject with the "EffectSource" tag
        GameObject gameObject = GameObject.FindWithTag("MusicSource");

        if (gameObject != null)
        {
            // Get the AudioSource component
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();

            if (audioSource != null)
            {
                // Mute the AudioSource if slider value is 0%, unmute otherwise
                audioSource.mute = sliderValue <= 0.01f;
                UpdateTextValue(); 
            }
        }
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
