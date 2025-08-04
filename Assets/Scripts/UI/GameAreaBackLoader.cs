using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Level;

public class GameAreaBackLoader : MonoBehaviour
{
    private GameBoard gameBoardClass;

    public SpriteRenderer spriteRendererGameArea;

    public Sprite[] spriteEasyLevelsBack;
    public Sprite[] spriteMediumLevelsBack;
    public Sprite[] spriteHardLevelsBack;

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        if( gameBoardClass != null)
        {            
            if (gameBoardClass.levelDifficulty != null)
            {
                setGameBackAreaSprite(gameBoardClass.levelDifficulty); //set from file
            }
            else
            {
                setGameBackAreaSprite(0); //set simple
            }
                
        }
        else
        {
            Debug.LogWarning("GameAreaBackLoader: gameBoardClass=null");
        }
            
    }

    private void setGameBackAreaSprite(int difficulty)
    {
        int randomSpriteNumber = UnityEngine.Random.Range(0, 3);

        switch (difficulty)
        {
            case 0:
                spriteRendererGameArea.sprite = spriteEasyLevelsBack[randomSpriteNumber];
                break;
            case 1:
                spriteRendererGameArea.sprite = spriteMediumLevelsBack[randomSpriteNumber];
                break;
            case 2:
                spriteRendererGameArea.sprite = spriteHardLevelsBack[randomSpriteNumber];
                break;
        }

    }

}
