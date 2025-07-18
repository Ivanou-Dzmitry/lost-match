using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelGoals : MonoBehaviour
{
    [Header("Scriptable Objects")]
    public World worldClass;
    public WorldManager worldManager;
    public Level level;
    public BlankGoalClass[] levelGoals;
    public string goalDescription;
    private int totalLevels;
    private const string className = "LevelGoals";


    public void GetGoals(int levelN)
    {

        if (worldManager != null)
        {
            level = worldManager.GetLevel(levelN, out World foundWorld);
            totalLevels = worldManager.GetTotalLevelsCount();

            worldClass = foundWorld;
        }
        else
        {
            Debug.LogError($"{className}: worldManager in NULL here");
        }

        if (level == null)
        {
            Debug.LogError($"{className}: Failed to load level {levelN}");
            return;
        }


        if (worldClass != null)
        {
            if (levelN < totalLevels)
            {
                levelGoals = level.levelGoals;
                goalDescription = level.goalsDescription;
            }
            else
            {
                Debug.LogError($"{className}: worldClass is NULL here");
            }
        }        
    }

}
