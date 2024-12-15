using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public RectTransform panelTop; // Assign your panel's RectTransform in the Inspector
    public RectTransform panelCenter;
    public RectTransform panelBottom;

    public int paddingPanels; // Assign your panel's RectTransform in the Inspector

    private int topPanelHeight = 180;

    [Header("Panel Shop")]
    public GameObject panelWithShop;
    public Vector2 constantSizePWS = new Vector2(768, 2050);

    [Header("Panel With Shop Controls")]
    public GameObject panelWithControls;
    public Vector2 constantSizePWC = new Vector2(768, 1990);

    [Header("Panel With Shop Items")]
    public GameObject panelWithShopItems;
    public Vector2 constantSizePSI = new Vector2(768, 1200);

    void Start()
    {
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

            // Lower the panel by the unsafe zone's height at the bottom
            Vector2 anchoredPosition = panelTop.anchoredPosition;
            anchoredPosition.y = 0;

            //anchoredPosition.y = -unsafeZoneHeight; // Adjust based on your pivot and alignment
            panelTop.anchoredPosition = anchoredPosition;            
        }

        if (panelCenter != null)
        {
            //change size
            Vector2 currentSize = panelCenter.sizeDelta;
            panelCenter.sizeDelta = new Vector2(currentSize.x, currentSize.y);            

            Vector2 anchoredPosition2 = panelCenter.anchoredPosition;
            anchoredPosition2.y = -unsafeZoneHeight - topPanelHeight - paddingPanels;
            panelCenter.anchoredPosition = anchoredPosition2;
        }        
    }

}
