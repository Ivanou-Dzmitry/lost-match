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

    private AudioClip lastPlayedClip;

    private GameData gameDataClass;
    private SettingsManager settingsManagerClass;

    public float fadeTime = 1.5f; // You can set this in the inspector

    private float originalVolume;

    public static SoundManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        MuteSound(gameDataClass.saveData.soundToggle);
        MuteMusic(gameDataClass.saveData.musicToggle);

        SetVolume("sound");
        SetVolume("music");

        //Debug.Log($"Volume: sound={effectsSource.volume}, music={musicSource.volume}");
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
        if (gameDataClass.saveData.soundToggle == true && clip != lastPlayedClip)
        {
            effectsSource.PlayOneShot(clip);
            lastPlayedClip = clip;
            StartCoroutine(ResetLastPlayedClip(clip.length));
        }        
    }

    private IEnumerator ResetLastPlayedClip(float duration)
    {
        yield return new WaitForSeconds(duration);
        lastPlayedClip = null;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (gameDataClass.saveData.musicToggle == true)
        {
            //Debug.Log($"{musicSource}, {musicSource.isPlaying}, {musicSource.volume}");

            if (musicSource != null && musicSource.isPlaying)
            {
                originalVolume = musicSource.volume;
                StartCoroutine(FadeOutAndPlayNewClip(clip)); //fade out and play music
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = originalVolume;
                musicSource.Play();
            }
        }
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            originalVolume = musicSource.volume;
            StartCoroutine(FadeOutAndStop());
        }
    }

    private IEnumerator FadeOutAndStop()
    {
        float startVolume = musicSource.volume;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = originalVolume;
    }

    private IEnumerator FadeOutAndPlayNewClip(AudioClip newClip)
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = originalVolume;
        musicSource.Play();
    }


    public void ButtonClick()
    {
        AudioClip aClip = Resources.Load<AudioClip>("Sound/Effects/btn_click01_aclip");
        
        if (aClip == null)
            Debug.LogError("Failed to load audio clip!");
        
        effectsSource.PlayOneShot(aClip);
    }
}
