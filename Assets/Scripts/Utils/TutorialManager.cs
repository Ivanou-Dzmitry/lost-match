using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private GameBoard gameBoardClass;
    private GameData gameDataClass;

    public Sprite[] tutorialSprites;
    public SpriteRenderer tutorialSpriteRenderer;


    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        int activeLevels = 0;

        //for run tutor only once
        for (int i = 0; i < gameDataClass.saveData.isActive.Length; i++)
        {
            if (gameDataClass.saveData.isActive[i])
            {
                activeLevels++;
            }
        }
       
        // Early exit if gameBoardClass is null
        if (gameBoardClass == null) return;

        // Single condition check - only when activeLevels matches loadedLevel
        if (activeLevels == gameBoardClass.loadedLevel)
        {
            switch (activeLevels)
            {
                case 1:
                    tutorialSpriteRenderer.sprite = tutorialSprites[0];
                    break;
                case 3:
                    tutorialSpriteRenderer.sprite = tutorialSprites[1];
                    break;
                case 4:
                    tutorialSpriteRenderer.sprite = tutorialSprites[2];
                    break;
                case 6:
                    tutorialSpriteRenderer.sprite = tutorialSprites[3];
                    break;
            }
        }
    }


}
