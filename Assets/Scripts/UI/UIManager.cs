using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public RectTransform panelTop; // Assign your panel's RectTransform in the Inspector
    public RectTransform panelCenter;
    int paddingTop = 8; // Assign your panel's RectTransform in the Inspector

    void Start()
    {
        AdjustPanelPosition();
    }

    void AdjustPanelPosition()
    {
        if (panelTop == null) return;

        // Get the safe area values
        Rect safeArea = Screen.safeArea;

        // Get the screen height
        float screenHeight = Screen.height;

        // Calculate the height of the unsafe zone (top and bottom combined)
        float unsafeZoneHeight = screenHeight - safeArea.height;

        // Lower the panel by the unsafe zone's height at the bottom
        Vector2 anchoredPosition = panelTop.anchoredPosition;
        Vector2 anchoredPosition2 = panelCenter.anchoredPosition;

        anchoredPosition.y = -unsafeZoneHeight; // Adjust based on your pivot and alignment
        anchoredPosition2.y = -unsafeZoneHeight - paddingTop;

        panelTop.anchoredPosition = anchoredPosition;
        panelCenter.anchoredPosition = anchoredPosition2;

        Debug.Log($"Safe Area: {safeArea}, Unsafe Zone Height: {unsafeZoneHeight}");
    }
}
