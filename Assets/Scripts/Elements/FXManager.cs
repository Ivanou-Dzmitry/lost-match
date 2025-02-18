using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FXManager : MonoBehaviour
{
    private GameBoard gameBoardClass;

    [Header("ColorBomb Staff")]
    //for colorbomb
    public List<GameObject> createdLines = new List<GameObject>();

    //add rainbow
    private List<Color> rayColors = new List<Color>()
    {
        Color.red,              // Red
        new Color(1f, 0.647f, 0f), // Orange
        Color.yellow,           // Yellow
        Color.green,            // Green
        Color.blue,             // Blue
        new Color(0.294f, 0f, 0.51f), // Indigo
        new Color(0.56f, 0f, 1f) // Violet
    };

    public Material colorBombRayMat;

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        //color bomb
        createdLines.Clear();
    }

    // Coroutine to fade out and delete color bomb lines after a delay
    public IEnumerator DeleteColorBombLines(float delay)
    {
        // Wait for the specified delay before starting the fade-out process
        yield return new WaitForSeconds(delay);

        // Fade out each line and then destroy it
        foreach (GameObject line in createdLines)
        {
            if (line != null)
            {
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // Start fading out the line before destroying it
                    StartCoroutine(FadeAndDestroyLine(lineRenderer, line));
                }
            }
        }

        // Clear the list after deletion (all lines will be destroyed by now)
        createdLines.Clear();
    }

    public void CreateColorBombLines(Vector2 startPoint, Vector2 endPoint, Color color, float width)
    {
        // Create a new GameObject for the LineRenderer
        GameObject lineObject = new GameObject("DynamicLine");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.name = "colorbomb_line_" + endPoint.x + "_" + endPoint.y;

        // Set the material
        lineRenderer.material = colorBombRayMat;

        //width = 0.5f;

        // Divide the line into 8 segments
        int segments = 12;
        lineRenderer.positionCount = segments + 1; // 9 points for 8 segments      

        // Convert Vector2 to Vector3 (z = 0 for 2D lines)
        //Vector3 startPoint3D = new Vector3(startPoint.x, startPoint.y, 0f);
        //Vector3 endPoint3D = new Vector3(endPoint.x, endPoint.y, 0f);

        // Calculate the total distance between start and end points
        float lineLength = Vector2.Distance(startPoint, endPoint);

        //Debug.Log(lineRenderer.name + "=" + lineLength);

        // Set the positions for the start and end points
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(segments, endPoint);

        // Determine the maxShift based on the line length
        ///float maxShift = (lineLength < 2f) ? 0.01f : 0.2f; // Use 0.1f for short lines, 0.5f for long lines

        if (lineLength < 1.5f)
        {
            // For short lines, set only start and end points
            lineRenderer.positionCount = 2; // Only 2 points for start and end
            lineRenderer.SetPosition(0, new Vector3(startPoint.x, startPoint.y, 0f));
            lineRenderer.SetPosition(1, new Vector3(endPoint.x, endPoint.y, 0f));
        }
        else
        {
            // For longer lines, divide into segments and apply sawtooth pattern
            //int segments = 16;
            lineRenderer.positionCount = segments + 1; // 17 points for 16 segments

            // Convert Vector2 to Vector3 (z = 0 for 2D lines)
            Vector3 startPoint3D = new Vector3(startPoint.x, startPoint.y, 0f);
            Vector3 endPoint3D = new Vector3(endPoint.x, endPoint.y, 0f);

            // Calculate the normalized direction vector
            Vector3 direction = (endPoint3D - startPoint3D).normalized;

            // Calculate and apply the sawtooth pattern
            float segmentLength = lineLength / segments; // Uniform length for each segment
            float maxShift = (Mathf.Abs(endPoint.x - startPoint.x) > 2f && Mathf.Abs(endPoint.y - startPoint.y) > 2f) ? 0.4f : 0.2f; // Larger shift for longer diagonals

            for (int i = 0; i <= segments; i++) // Include both endpoints
            {
                Vector3 point = startPoint3D + direction * (i * segmentLength);

                // Calculate shift factor that decreases as we get closer to the end point
                float t = (float)i / segments; // Interpolation value between 0 and 1
                float shiftFactor = maxShift * (1 - t); // Gradually decrease shift as we approach the end

                // Create the sawtooth effect by shifting odd points along x and y
                if (i % 2 != 0) // Check if the index is odd
                {
                    // Apply alternating shifts for the sawtooth effect
                    float shift = shiftFactor * (i % 2 == 1 ? 1 : -1); // Alternating direction for each odd point

                    // Apply the shift to both X and Y axes
                    point.x += shift;
                    point.y += shift;
                }

                lineRenderer.SetPosition(i, point);
            }
        }

        int randStart = UnityEngine.Random.Range(0, rayColors.Count);
        //int randEnd = UnityEngine.Random.Range(0, rayColors.Count);

        Color startRayColor = rayColors[randStart];
        Color endRayColor = rayColors[randStart];

        // Set the color
        lineRenderer.startColor = startRayColor;
        lineRenderer.endColor = endRayColor;

        // Set the width
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        // Set additional properties (optional)
        lineRenderer.useWorldSpace = true; // Use world coordinates
        lineRenderer.sortingOrder = 1;    // Ensure visibility

        //sorting
        lineRenderer.sortingLayerName = "Elements";
        lineRenderer.sortingOrder = 3;


        //set parent
        lineObject.transform.parent = gameBoardClass.gameArea.transform;

        createdLines.Add(lineObject);
    }


    // Coroutine to fade out the line's opacity and destroy it
    private IEnumerator FadeAndDestroyLine(LineRenderer lineRenderer, GameObject lineObject)
    {
        float fadeDuration = 0.5f; // Time in seconds to fade out
        float startTime = Time.time;
        Color startColor = lineRenderer.startColor;

        //get width
        float startWidth = lineRenderer.startWidth;

        // Gradually decrease opacity over time
        while (Time.time < startTime + fadeDuration)
        {
            float t = (Time.time - startTime) / fadeDuration; // Calculate the time factor
            float alpha = Mathf.Lerp(1f, 0f, t); // Lerp from 1 to 0 for opacity

            float lineWidth = Mathf.Lerp(startWidth, 0f, t); // Lerp from 1 to 0 for opacity

            // Set the color with the new alpha
            Color fadedColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lineRenderer.startColor = fadedColor;
            lineRenderer.endColor = fadedColor;

            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            yield return null; // Wait until the next frame
        }

        // Ensure the final opacity is 0
        lineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        lineRenderer.endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        // Destroy the line object after fading
        Destroy(lineObject);
    }


    // Helper to instantiate and configure a particle
    public void InstantiateAndConfigureParticle(ElementController element, Vector3 position, Quaternion rotation)
    {
        GameObject particle = Instantiate(element.lineBombParticle, position, rotation);
        SpriteMask spriteMask = particle.GetComponentInChildren<SpriteMask>();
        if (spriteMask != null)
            SetSpriteMaskToScreenCenter(spriteMask, rotation == Quaternion.identity ? 0 : 90);
        Destroy(particle, 1.9f);
    }

    //for row col bombs
    void SetSpriteMaskToScreenCenter(SpriteMask spriteMask, int angle = -1)
    {
        float yOffset = 1; //see public float yOffset = 1; in CameraManager

        // Get the screen center in world space
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 worldCenter = Camera.main.ScreenToWorldPoint(screenCenter);

        // Set the Sprite Mask position (ensure the correct z-axis value)
        worldCenter.z = 0f; // Adjust this depending on your scene setup
        worldCenter.y -= yOffset;
        spriteMask.transform.position = worldCenter;

        spriteMask.transform.localScale = new Vector3(gameBoardClass.column, gameBoardClass.row, 0);

        //rotate for horizontal
        if (angle > 0)
            spriteMask.transform.eulerAngles += new Vector3(0, 0, angle);
    }

}
