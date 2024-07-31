using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //classes
    private GameBoard gameBoardClass;

    [Header("Camera Tuning Stuff")]
    public float cameraOffset;
    public float aspectRatio = 1.78f;
    public float padding = 1;
    public float yOffset = 1;

    public GameObject backImage;
    public GameObject elementBack;

    // Start is called before the first frame update
    void Start()
    {
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();

        if (gameBoardClass != null)
        {
            CameraPos(gameBoardClass.column - 1, gameBoardClass.row - 1);
        }
    }

    void CameraPos(float x, float y)
    {
        Vector3 temPos = new Vector3(x / 2, y / 2 + yOffset, cameraOffset);

        transform.position = temPos;

        Camera.main.orthographicSize = 8.57f;

        //background
        backImage.transform.position = new Vector3(temPos.x, temPos.y, 0);
        elementBack.transform.position = new Vector3(temPos.x, temPos.y - yOffset, 0);
    }
}
