using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;

    [Header("Camera Tuning Stuff")]
    public float cameraOffset;
    public float yOffset = 1; //!Important

    public GameObject backImage;
    public GameObject elementBack;
    public GameObject tutorPanel;

    public GameLog log;

    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        if (gameBoardClass != null)
        {
            CameraPos(gameBoardClass.column - 1, gameBoardClass.row - 1);
        }
        else
        {
            log.WriteSysLog("ERROR: gameBoardClass is null");
        }
    }

    void CameraPos(float x, float y)
    {
        float aspect = (float)Screen.width / Screen.height;

        float newOrthoSize;
       
        // Match common vertical aspect ratios
        if (aspect >= 0.5f)         // ~16:9 and similar
            newOrthoSize = 9f;
        else if (aspect >= 0.45f)   // ~19.5:9
            newOrthoSize = 9.0f;
        else if (aspect >= 0.42f)   // ~20:9
            newOrthoSize = 10f;
        else if (aspect >= 0.39f)   // ~21:9
            newOrthoSize = 10.5f;
        else if (aspect >= 0.35f)   // ~Galaxy Fold (Folded)
            newOrthoSize = 11.5f;
        else                        // Anything taller
            newOrthoSize = 12f;

        Camera.main.orthographicSize = newOrthoSize;

        //for back image
        float scaleFactor = 1f;

        if (newOrthoSize > 9f)
        {
            // Linear scale from 9 to 12 scale from 1.0 to 1.2
            scaleFactor = Mathf.Lerp(1f, 1.25f, (newOrthoSize - 9f) / 3f);
        }

        backImage.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        Vector3 temPos = new Vector3(x / 2, y / 2 + yOffset, cameraOffset);

        transform.position = temPos;

        //background
        backImage.transform.position = new Vector3(temPos.x, temPos.y, 0);
        elementBack.transform.position = new Vector3(temPos.x, temPos.y - yOffset, 0);
        
        //tutorial panel
        tutorPanel.transform.position = new Vector3(temPos.x, temPos.y - yOffset, 0);

        if (log!=null)
            log.WriteSysLog($"{newOrthoSize}, {scaleFactor}");
    }
}
