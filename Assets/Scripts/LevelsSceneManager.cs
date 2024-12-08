using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelsSceneManager : MonoBehaviour
{
    private SoundManager soundManagerClass;
    public AudioClip thisSceneMusic;

    // Start is called before the first frame update
    void Start()
    {
        //class init
        soundManagerClass = GameObject.FindWithTag("SoundManager").GetComponent<SoundManager>();

        if (soundManagerClass != null)
        {
            soundManagerClass.PlayMusic(thisSceneMusic);
        }
    }


}
