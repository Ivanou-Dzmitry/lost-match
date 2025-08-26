using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
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
    

    public static class SystemInformation
    {
        [DllImport("user32.dll")]
        private static extern System.IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetWindowDC(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(System.IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        private const uint SPI_GETWORKAREA = 0x0030;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public static int WorkingAreaHeight()
        {
            RECT rect = new RECT();
            if (SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0))
            {
                return rect.Bottom - rect.Top;
            }
            else
            {
                // Fallback to screen height if failed
                return Screen.currentResolution.height;
            }
        }
    }


    private void Start()
    {

        //Set Framerate
        Application.targetFrameRate = 30;

        //for desktop builds
#if UNITY_STANDALONE_WIN
    int targetHeight = 1024;
    float aspectRatio = 9f / 16f;  // Portrait

    int calculatedWidth = Mathf.RoundToInt(targetHeight * aspectRatio);

    Screen.SetResolution(calculatedWidth, targetHeight, false);
#elif UNITY_STANDALONE_OSX
    int targetHeight = 1600;  // You can choose a different height for macOS
    float aspectRatio = 9f / 16f;

    int calculatedWidth = Mathf.RoundToInt(targetHeight * aspectRatio);

    Screen.SetResolution(calculatedWidth, targetHeight, false);
#endif

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

        //pause
        if (paused)
        {
            gameBoardClass.currentState = GameState.pause;
        }

        //avoid pause 
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

    //if WIN
    public void WinConfirm()
    {
        if (gameDataClass != null)
        {
            //open new level
            if(gameBoardClass.loadedLevel < gameBoardClass.totalLevels)
            {
                gameDataClass.saveData.isActive[gameBoardClass.loadedLevel] = true;
                gameDataClass.SaveToFile();
            }
            else
            {
                gameDataClass.SaveToFile();
            }
        }
    }

}
