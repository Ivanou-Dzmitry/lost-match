using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager soundManager;

    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;

    private AudioClip lastPlayedClip;

    private GameData gameDataClass;

    public float fadeTime = 1.5f; // You can set this in the inspector

    private float originalVolume;
    private float savedMusicTime = 0f;

    [Header("Music")]
    public AudioClip[] musicClips;
   
    public GameLog log;
    private const string className = "SoundManager:";

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
       
        if (gameDataClass != null)
        {
            LoadSoundData();
        }
        else
        {
            log.WriteSysLog($"{className}ERR: gameDataClass null");
        }
    }


    private void LoadSoundData()
    {
        try
        {
            //lastPlayedClip = musicClips[gameDataClass.saveData.currentPlayingClipIndex];
            musicSource.clip = lastPlayedClip;
        }
        catch (System.Exception ex)
        {
            log.WriteSysLog($"{className}Failed to set music clip: {ex.Message}\n{ex.StackTrace}");
        }

        MuteSound(gameDataClass.saveData.soundToggle);
        MuteMusic(gameDataClass.saveData.musicToggle);

        SetVolume("sound");
        SetVolume("music");        

        if (lastPlayedClip != null && musicSource.mute == false)
        {
            PlayMusic(lastPlayedClip);
        }        
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
                originalVolume = musicSource.volume;
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
            musicSource.time = savedMusicTime;
            PlayMusic(lastPlayedClip);
        }
        else
        {
            savedMusicTime = musicSource.time;            
            musicSource.mute = true;
            lastPlayedClip = musicSource.clip;
        }            
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
            if (musicSource != null && musicSource.isPlaying)
            {
                originalVolume = musicSource.volume;
                MusicFader("fadeIn", "play", clip); //fade out and play music
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
            MusicFader("fadeOut", "stop");
        }
    }

    public void MusicFader(string fadeType, string action, AudioClip clip = null)
    {
        StartCoroutine(FadeMusicCoroutine(fadeType, action, clip));
    }

    private IEnumerator FadeMusicCoroutine(string fadeType, string action, AudioClip newClip = null)
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        if (fadeType == "fadeOut")
        {
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
                yield return null;
            }

            // Don't use Stop() if you want to resume later
            if (action == "stop")
            {
                savedMusicTime = musicSource.time;
                musicSource.Pause();  // Keeps position
                yield break;
            }

            musicSource.Pause();  // Pause before switching clip
        }

        if (action == "play")
        {
            if (newClip != null && musicSource.clip != newClip)
            {
                musicSource.clip = newClip;
                savedMusicTime = 0f;
            }

            musicSource.volume = (fadeType == "fadeIn") ? 0f : originalVolume;
            musicSource.time = savedMusicTime;
            musicSource.Play();

            if (fadeType == "fadeIn")
            {
                t = 0f;
                while (t < fadeTime)
                {
                    t += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0f, originalVolume, t / fadeTime);
                    yield return null;
                }
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
            lastPlayedClip = musicClips[scene.buildIndex];
            PlayMusic(lastPlayedClip);
    }


    public void ButtonClick()
    {
        //set click sound
        AudioClip aClip = Resources.Load<AudioClip>("Sound/Effects/btn_click01_aclip");
        
        if (aClip == null)
            Debug.LogError("Failed to load audio clip!");
        
        effectsSource.PlayOneShot(aClip);
    }
}
