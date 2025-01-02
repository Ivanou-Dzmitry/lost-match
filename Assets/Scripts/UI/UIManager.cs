using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColorPalette
{
    public static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
    {
        { "DarkBlue", new Color(0.196f, 0.231f, 0.4f) }, // 323B66
        { "DarkTeal", new Color(0.372f, 0.466f, 0.569f) }, // 5F7791
        { "VioletMed", new Color(0.831f, 0.729f, 0.902f) }, // D4BAE6
        { "LightTeal", new Color(0.729f, 0.812f, 0.902f) }, // BACFE6
        { "LightGreen", new Color(0.760f, 0.902f, 0.729f) },  // C2E6BA
        { "DarkViolet", new Color(0.3176f, 0.1961f, 0.4f) },    // 513266
        { "GreenSaturate", new Color(0.533f, 0.902f, 0.451f) }  // 88E673
    };
}

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

    private float waitingTime = 1f;

    [Header("Final Text")]
    public GameObject finalTextPanel;
    public GameObject infoPanel;
    public TMP_Text finalText;
    private Image infoPanelImage;


    void Start()
    {
        RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
        canvasDimension.y = canvasRect.rect.height;
        canvasDimension.x = canvasRect.rect.width;

        AdjustPanelPosition();

        //get color from panel
        if(infoPanel != null )
            infoPanelImage = infoPanel.GetComponent<Image>();
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

    public void ShowInGameInfo(string infoText, bool showPanel, Color pnlColor = default)
    {
        if (pnlColor == default)
        {
            infoPanelImage.color = ColorPalette.Colors["DarkTeal"];
        }
        else
        {
            infoPanelImage.color = pnlColor;
        }
            

        if (showPanel)
        {
            finalTextPanel.SetActive(true);
            finalText.text = infoText;
        }
        else
        {
            finalTextPanel.SetActive(false);
            finalText.text = "";
        }

        //hide panel
        if (finalTextPanel != null && finalTextPanel.activeSelf)
        {
            StartCoroutine(HidePanelCoroutine(waitingTime));
        }
    }

    private IEnumerator HidePanelCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay        

        finalTextPanel.SetActive(false); // Hide the panel
    }

    private void PanelActivator()
    {
        finalTextPanel.SetActive(false); // Completely disable the panel
    }

}
