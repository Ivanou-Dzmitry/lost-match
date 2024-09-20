using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGoals : MonoBehaviour
{
    [Header("Scriptable Objects")]
    public World worldClass;
    public BlankGoalClass[] levelGoals;

    public void GetGoals(int level)
    {
        if (worldClass != null)
        {
            if (level < worldClass.levels.Length)
            {
                levelGoals = worldClass.levels[level].levelGoals;
            }
        }
    }

}
