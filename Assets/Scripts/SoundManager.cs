using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();       
        settingsManagerClass = GameObject.FindWithTag("SettingsManager").GetComponent<SettingsManager>();
       

        if (gameDataClass != null)
        {
            LoadSoundData();
        }
    }


    private void LoadSoundData()
    {
        SetVolume("sound");
        SetVolume("music");

        MuteSound(gameDataClass.saveData.soundToggle);
        MuteMusic(gameDataClass.saveData.musicToggle);        
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

    public void MuteSound(bool value)
    {
        if (value)
        {
            effectsSource.mute = false;
        }
        else
        {
            effectsSource.mute = true;
        }
        
    }

    public void MuteMusic(bool value)
    {
        if(value)
        {
            musicSource.mute = false;
        }
        else
        {
            musicSource.mute = true;
        }

        AudioClip clip = musicSource.clip;
        if (clip != null)
            PlayMusic(clip);
    }


    public void PlaySound(AudioClip clip)
    {
        if (gameDataClass.saveData.soundToggle == true)
        {
            effectsSource.PlayOneShot(clip);
        }
        
    }

    public void PlayMusic(AudioClip clip)
    {
        if (gameDataClass.saveData.musicToggle == true)
        {
            musicSource.clip = clip;

            //stop previous
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }


    public void ButtonClick()
    {
        aClip = (AudioClip)Resources.Load("button_click_01");
        effectsSource.PlayOneShot(aClip);
    }
}
