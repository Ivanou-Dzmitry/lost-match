using UnityEngine;

[CreateAssetMenu(fileName = "World", menuName = "World")]

public class World : ScriptableObject

{
    public int worldNumber;
    public Level[] levels;    
}
