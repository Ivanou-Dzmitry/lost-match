using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public Canvas mainCanvas;
    private Vector2 canvasDimension;
    

    [Header("Main Panels")]
    public RectTransform panelTop; // Assign your panel's RectTransform in the Inspector    
    public RectTransform panelCenter;
    public RectTransform panelBottom;

    [Header("Panels with Button Levels")]
    public RectTransform panelLevelButtons; //for buttons

    [Header("Padding between Panels")]
    public int paddingPanels; // Assign your panel's RectTransform in the Inspector

    private float panelCenterHeight;

    private int topPanelHeight = 235; //!Important
    private int panelBottomHeight = 0;
    private int controlButtonsHeight = 64;

    void Start()
    {
        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
        canvasDimension.y = canvasRect.rect.height;
        canvasDimension.x = canvasRect.rect.width;

        AdjustPanelPosition();
    }

    void AdjustPanelPosition()
    {
        // Get the safe area values
        Rect safeArea = Screen.safeArea;

        // Get the screen height
        float screenHeight = Screen.height;

        // Calculate the height of the unsafe zone (top and bottom combined)
        float unsafeZoneHeight = screenHeight - safeArea.height;

        float panelTopHeight = 0;

        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (panelTop != null)
        {
            panelTopHeight = panelTop.rect.height;

            //set size
            Vector2 size = panelTop.sizeDelta;
            

            //panel for game and levels
            size.y = unsafeZoneHeight + topPanelHeight;

            panelTop.sizeDelta = size;

            panelTopHeight = size.y;

            // Lower the panel by the unsafe zone's height at the bottom
            Vector2 anchoredPosition = panelTop.anchoredPosition;
            anchoredPosition.y = 0;

            //anchoredPosition.y = -unsafeZoneHeight; // Adjust based on your pivot and alignment
            panelTop.anchoredPosition = anchoredPosition;            
        }

        panelCenterHeight = 0;

        if (panelCenter != null)
        {
            //change size
            Vector2 currentSize = panelCenter.sizeDelta;
            currentSize.y = canvasDimension.y - panelTopHeight - (paddingPanels) - panelBottomHeight;

            panelCenterHeight = currentSize.y;            

            panelCenter.sizeDelta = new Vector2(currentSize.x, currentSize.y);

            Vector2 anchoredPosition2 = panelCenter.anchoredPosition;
            anchoredPosition2.y = -unsafeZoneHeight - topPanelHeight - paddingPanels;
            panelCenter.anchoredPosition = anchoredPosition2;
        }        

        if(panelLevelButtons != null)
        {
            Vector2 currentSize = panelLevelButtons.sizeDelta;
            currentSize.y = panelCenterHeight - (controlButtonsHeight*2);
            panelLevelButtons.sizeDelta = new Vector2(currentSize.x, currentSize.y);

            LevelButtonsPanelSize();
        }

    }

    public float LevelButtonsPanelSize()
    {
        return panelLevelButtons.sizeDelta.y;
    }

}
