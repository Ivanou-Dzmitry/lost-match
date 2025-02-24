
using System.Xml.Linq;
using UnityEngine;


public class BackBuilder : MonoBehaviour
{
    public Sprite backSprites;

    private Sprite[] tiles; // Assign your sprites in the inspector
    public GameObject tilePrefab; // Assign a UI Image prefab (with an Image component)
    public Transform parentGrid; // Parent GameObject to hold all tiles
    private float tileSize = 1f; // Size of each tile (adjust as needed)

    // Start is called before the first frame update
    void Start()
    {
        //LoadDataFromXML();

        tiles = GetSpritesFromAtlas(backSprites);        
    }

    Sprite[] GetSpritesFromAtlas(Sprite atlas)
    {
        if (atlas == null)
        {
            Debug.LogError("Atlas is NULL! Make sure you assigned it in the Inspector.");
            return new Sprite[0];
        }

        // Use atlas.texture.name instead of atlas.name
        Sprite[] sprites = Resources.LoadAll<Sprite>("BackTiles/back_tile01");

        if (sprites.Length == 0)
        {
            Debug.LogError($"No sprites found for {atlas.texture.name}. Ensure the sprite is in a 'Resources' folder and properly sliced.");
        }
        else
        {
            Debug.Log($"Loaded {sprites.Length} sprites from {atlas.texture.name}");
        }

        return sprites;
    }

    public void LoadDataFromXML(TextAsset textAsset, int column, int row)
    {

        //string xmlPath = Path.Combine(Application.dataPath, "ScriptableObjects", folder, file);
        XElement dataElement = null;

        if (textAsset != null)
        {
            XDocument xmlDoc = XDocument.Parse(textAsset.text);
            // Load the XML file       
            dataElement = xmlDoc.Element("data");
        }       

        // Get the CSV data and split into individual values
        string csvData = dataElement.Value;
        string[] values = csvData.Split(',');

        // Loop through the CSV values and place tiles

        for (int y = row - 1; y >= 0; y--)  // Start from the last row (top) and go down
        {
            for (int x = column - 1; x >= 0; x--)  // Start from the last column (right) and go left
            {
                int index = y * column + x;  // Calculate the index for the 1D array

                if (index < values.Length)
                {
                    int tileValue = int.Parse(values[index]);

                    // Only generate tiles for non-zero values (assuming tileValue 0 means no tile)

                        Vector2 worldPosition = MapToPosition(x, y, row);
                        GenerateTiles(worldPosition.x, worldPosition.y, tileValue);  // Call GenerateTiles to create the tile

                }
                else
                {
                    Debug.LogError("SKIPPED!");
                }
            }
        }

    }


    void GenerateTiles(float column, float row, int tileNumber)
    {
        if (tiles == null || tiles.Length == 0)
        {
            Debug.LogError("No sprites assigned to backSprites!");
            return;
        }

        GameObject newTile = null;

        // Instantiate a new tile GameObject
        if (tilePrefab != null)
        {
            newTile = Instantiate(tilePrefab, parentGrid);
            newTile.name = "Tile_" + tileNumber +"-" + column + "_" + row;
        }

        // Find the index in the tiles array based on tileNumber. 0-15 Elements, 16-... back
        int tileIndex = tileNumber - 26;

       // Debug.Log("tileIndex: " + tileIndex);
        

        if (tileIndex >= 0 && tileIndex < tiles.Length)
        {
            // Set sprite in SpriteRenderer instead of UI Image
            SpriteRenderer spriteRenderer = newTile.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = tiles[tileIndex];
                // Position in the grid (for world objects)
            }
            else
            {
                Debug.LogWarning("SpriteRenderer not found on tilePrefab!");
            }                
        }
        else
        {
            Debug.LogError("Tile number " + tileNumber + " is out of bounds for the tiles array!");
        }

        Vector2 gridPosition = new Vector2(column, row);
        newTile.transform.position = gridPosition;
    }

    Vector2 MapToPosition(float x, float y, int columns)
    {
        // We assume (0,0) is at bottom-left corner for 16 and (6,8) is top-right for 19.
        float posX = x * tileSize;  // X-coordinate based on column index
        float posY = ((columns-1) - y) * tileSize; // Invert y to align the position from bottom to top  See camManager YOffset      

        return new Vector2(posX, posY);
    }

}
