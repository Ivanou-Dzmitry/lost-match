using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "World", menuName = "Level")]

public class Level : ScriptableObject

{
    [Header("Size")]
    public int columns;
    public int rows;

    [Header("Back Tile File")]
    public TextAsset xmlLayoutFile;

    [Header("Layout")]
    public TileType[] boardLayout;

    [Header("Art")]
    //public GameBoardBack elementsBack;

    [Header("Preload Layout")]
    public TileType[] preloadBoardLayout;

    [Header("Object Types")]
    public GameObject[] element;

    [Header("Level Goals")]
    public int[] scoreGoals;

    [Header("Goals Descripton")]
    public string goalsDescription;

    [Header("End Game Rules")]
    public EndGameRequriments endGameRequrimentsForLevel; //end game manager
    public BlankGoalClass[] levelGoals; // goal manager


}
