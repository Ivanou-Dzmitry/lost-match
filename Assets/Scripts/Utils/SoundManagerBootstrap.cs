using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManagerBootstrap : MonoBehaviour
{
    public GameObject soundManagerPrefab;

    private void Awake()
    {
        if (SoundManager.Instance == null)
        {
            Instantiate(soundManagerPrefab);
        }
    }
}
