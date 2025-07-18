using UnityEngine;

[CreateAssetMenu(fileName = "WorldManager", menuName = "World Manager")]
public class WorldManager : ScriptableObject
{
    public World[] worlds;
    public int levelPerWorld = 10;

    public World GetWorldForLevel(int level)
    {
        int worldIndex = (level - 1) / 10; // Example: level 23 => index 2 (World03)
        if (worldIndex >= 0 && worldIndex < worlds.Length)
            return worlds[worldIndex];

        Debug.LogWarning("World index out of range!");
        return null;
    }

    public Level GetLevel(int levelNumber, out World selectedWorld)
    {
        int worldIndex = (levelNumber - 1) / 10;
        int levelIndex = (levelNumber - 1) % 10;

        selectedWorld = null;

        if (worldIndex >= 0 && worldIndex < worlds.Length)
        {
            World world = worlds[worldIndex];
            if (world != null && levelIndex < world.levels.Length)
            {
                selectedWorld = world;
                return world.levels[levelIndex];
            }
        }

        return null;
    }

    public int GetTotalLevelsCount()
    {
        int totalLevels = 0;
        foreach (var world in worlds)
        {
            if (world != null && world.levels != null)
                totalLevels += world.levels.Length;
        }
        return totalLevels;
    }
}
