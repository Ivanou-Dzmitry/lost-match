using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager soundManager;

    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;

    private AudioClip aClip;

    private GameData gameDataClass;
    private SettingsManager settingsManagerClass;


    private void Awake()
    {
        if (soundManager == null)
        {
            soundManager = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        GameObject gameDataObject = GameObject.FindWithTag("GameData");
        gameDataClass = gameDataObject.GetComponent<GameData>();

        GameObject setManageObject = GameObject.FindWithTag("SettingsManager");
        settingsManagerClass = setManageObject.GetComponent<SettingsManager>();

        if (gameDataClass != null)
        {
            LoadData();
        }

    }


    private void LoadData()
    {
        //load data
        if (!gameDataClass.saveData.soundToggle)
        {
            settingsManagerClass.MuteSound(true);
        }


        if (!gameDataClass.saveData.musicToggle)
        {
            settingsManagerClass.MuteMusic(true);
        }

        //load vol
        effectsSource.volume = gameDataClass.saveData.soundVolume;
        musicSource.volume = gameDataClass.saveData.musicVolume;
    }

    public void SetVolume(string type)
    {
        if (gameDataClass != null)
        {
            if (type == "sound")
            {
                effectsSource.volume = gameDataClass.saveData.soundVolume;
            }

            if (type == "music")
            {
                musicSource.volume = gameDataClass.saveData.musicVolume;
            }
        }
    }


    public void PlaySound(AudioClip clip)
    {
        effectsSource.PlayOneShot(clip);
    }

}
