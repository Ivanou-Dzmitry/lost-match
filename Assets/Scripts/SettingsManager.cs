using System.Collections;
using System.Collections.Generic;
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
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Slider soundSlider;

    [Header("Music")]
    public Button musicButton;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;
    public Slider musicSlider;

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

        Debug.Log($"Screen Width: {unsafeAreaHeight}, Screen Height: {safeAreaHeight}");



    }

    public void PauseGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        paused = !paused;
    }

    private void Update()
    {
        if (sceneName == "GameBoard")
        {
            if (paused)
            {
                //settingsScreen.SetActive(true);
                gameBoardClass.currentState = GameState.pause;                
            }

            if (!paused)
            {
                //settingsScreen.SetActive(false);
                gameBoardClass.currentState = GameState.move;
            }
        }
    }

    private void LoadData()
    {        
        //set sound
        if (gameDataClass.saveData.soundToggle)
        {
            soundButton.image.sprite = soundOnSprite;
            soundSlider.interactable = true;
        }
        else
        {
            soundButton.image.sprite = soundOffSprite;
            soundSlider.interactable = false;
        }

        //set music
        if (gameDataClass.saveData.musicToggle)
        {
            musicButton.image.sprite = musicOnSprite;
            musicSlider.interactable = true;
        }
        else
        {
            musicButton.image.sprite = musicOffSprite;
            musicSlider.interactable = false;
        }

        //load volume
        soundSlider.value = gameDataClass.saveData.soundVolume;
        musicSlider.value = gameDataClass.saveData.musicVolume;

    }

    public void SoundButton()
    {
        if (!gameDataClass.saveData.soundToggle)
        {
            gameDataClass.saveData.soundToggle = true;
            soundButton.image.sprite = soundOnSprite;
            soundSlider.interactable = true;
        }
        else
        {
            gameDataClass.saveData.soundToggle = false;
            soundButton.image.sprite = soundOffSprite;
            soundSlider.interactable = false;
        }

    }

    public void MusicButton()
    {
        if (!gameDataClass.saveData.musicToggle)
        {
            gameDataClass.saveData.musicToggle = true;
            musicButton.image.sprite = musicOnSprite;
            musicSlider.interactable = true;
        }
        else
        {
            gameDataClass.saveData.musicToggle = false;
            musicButton.image.sprite = musicOffSprite;
            musicSlider.interactable = false;
        }
    }


    public void SoundVolume()
    {
        gameDataClass.saveData.soundVolume = soundSlider.value;
        soundManagerClass.SetVolume("sound");
    }

    public void MusicVolume()
    {
        gameDataClass.saveData.musicVolume = musicSlider.value;
        soundManagerClass.SetVolume("music");
    }


    public void MuteSound(bool muted)
    {
        GameObject gameObject = GameObject.FindWithTag("EffectSource");
        AudioSource audioSource = gameObject.GetComponent<AudioSource>();

        audioSource.mute = !audioSource.mute;
    }

    public void MuteMusic(bool muted)
    {
        GameObject gameObject = GameObject.FindWithTag("MusicSource");
        AudioSource audioSource = gameObject.GetComponent<AudioSource>();

        audioSource.mute = !audioSource.mute;
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
