using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "World", menuName = "Level")]

public class Level : ScriptableObject

{
    [Header("Size")]
    public int columns;
    public int rows;

    [Header("Layout")]
    public TileType[] boardLayout;

    [Header("Preload Layout")]
    public TileType[] preloadBoardLayout;

    [Header("Object Types")]
    public GameObject[] element;

    [Header("Level Goals")]
    public int[] scoreGoals;

    [Header("End Game Rules")]
    public EndGameRequriments endGameRequrimentsForLevel; //end game manager
    public BlankGoalClass[] levelGoals; // goal manager
}
