using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarManager : MonoBehaviour
{
    public GameObject gameBoard;
    private GameBoard gameBoardClass;

    public List<Sprite> avatarSprite = new List<Sprite>(); //for AVA 0-std, 1-surprize, 2-smile, 3-sleep
    public GameObject avatar;
    private Image avatarImage;
    public float delayAvatar = 2f;
    private bool isAvaSleeping = false;

    private int[] allowedIndexes = { 0, 2, 3 }; // Allowed sprite indexes
    private int currentIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        avatarImage = avatar.GetComponent<Image>();

        if (gameBoard != null)
        {
            gameBoardClass = gameBoard.GetComponent<GameBoard>();
        }
        else
        {
            gameBoardClass = null;
            InvokeRepeating(nameof(ChangeAvatar), 0f, 10f);
        }

    }

    public void SetTemporaryAvatar(int index, float waitTime)
    {
        if (index >= 0 && index < avatarSprite.Count && avatarImage != null)
        {
            avatarImage.sprite = avatarSprite[index];
            StopAllCoroutines(); // Stop any running reset coroutine
            StartCoroutine(ResetAvatarAfterTime(waitTime));
        }
    }

    // Coroutine to reset the avatar after X seconds
    private IEnumerator ResetAvatarAfterTime(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if (avatarSprite.Count > 0 && avatarImage != null)
        {
            avatarImage.sprite = avatarSprite[0]; // Reset to base avatar
        }
    }

    void Update()
    {

        if (gameBoardClass != null)
        {
            bool canSetTemporaryAvatar = gameBoardClass.currentState == GameState.move && gameBoardClass.matchState == MatchState.matching_stop;

            //avatar staff
            if (canSetTemporaryAvatar && !isAvaSleeping)
            {
                StartCoroutine(CheckConditionsFor10Seconds());
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))  // Left click or touch
            {
                if (avatarImage.sprite == avatarSprite[3])
                {
                    int randomIndex = Random.Range(0, 3);
                    avatarImage.sprite = avatarSprite[randomIndex];
                }
            }
        }

    }

    private IEnumerator CheckConditionsFor10Seconds()
    {
        isAvaSleeping = true;
        float timer = 0f;

        while (timer < 10f)
        {
            if (gameBoardClass != null)
            {
                bool cannotSetTemporaryAvatar = gameBoardClass.currentState != GameState.move || gameBoardClass.matchState != MatchState.matching_stop;

                if (cannotSetTemporaryAvatar)
                {
                    isAvaSleeping = false; // Stop checking if conditions are no longer met
                    yield break;
                }
            }
    
            timer += Time.deltaTime;
            yield return null;
        }

        // If conditions were true for 10 seconds, trigger avatar change
        
        SetTemporaryAvatar(3, 5);
        isAvaSleeping = false;
    }

    void ChangeAvatar()
    {
        if (avatarSprite.Count == 0 || avatarImage == null)
            return;

        // Get the next allowed index
        currentIndex = (currentIndex + 1) % allowedIndexes.Length;
        int spriteIndex = allowedIndexes[currentIndex];

        // Set the new sprite
        if (spriteIndex < avatarSprite.Count)
        {
            avatarImage.sprite = avatarSprite[spriteIndex];
        }
    }
}
