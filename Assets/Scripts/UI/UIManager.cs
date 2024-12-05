using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public RectTransform panelTop; // Assign your panel's RectTransform in the Inspector
    public RectTransform panelCenter;
    public RectTransform panelBottom;

    public int paddingPanels; // Assign your panel's RectTransform in the Inspector

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

        if (panelTop != null)
        {
            panelTopHeight = panelTop.rect.height;

            // Lower the panel by the unsafe zone's height at the bottom
            Vector2 anchoredPosition = panelTop.anchoredPosition;
            anchoredPosition.y = -unsafeZoneHeight; // Adjust based on your pivot and alignment
            panelTop.anchoredPosition = anchoredPosition;
        }

        float panelBottomHeight = 0;

        if (panelBottom != null)
        {
            panelBottomHeight = panelBottom.rect.height;
        }

        float panelCenterHeight = 0;

        if (panelCenter != null)
        {
            panelCenterHeight = panelCenter.rect.height;

            //change size
            Vector2 currentSize = panelCenter.sizeDelta;
            panelCenter.sizeDelta = new Vector2(currentSize.x, currentSize.y - unsafeZoneHeight);

            Vector2 anchoredPosition2 = panelCenter.anchoredPosition;
            anchoredPosition2.y = -unsafeZoneHeight - panelTopHeight - paddingPanels;
            panelCenter.anchoredPosition = anchoredPosition2;
        }
        
    }
}
