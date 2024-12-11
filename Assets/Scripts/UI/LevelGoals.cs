using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelGoals : MonoBehaviour
{
    [Header("Scriptable Objects")]
    public World worldClass;
    public BlankGoalClass[] levelGoals;
    public string goalDescription;
    
    public void GetGoals(int level)
    {
        if (worldClass != null)
        {
            if (level < worldClass.levels.Length)
            {
                levelGoals = worldClass.levels[level].levelGoals;
                goalDescription = worldClass.levels[level].goalsDescription;
            }
        }        
    }

}
