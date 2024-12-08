using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class GoalPanel : MonoBehaviour
{
    public Image thisImage;
    public Sprite thisSprite;
    public Image thisCheck;
    public TMP_Text thisText;
    public string thisString;

    // Start is called before the first frame update
    void Start()
    {
        SetupGoals();
    }

    void SetupGoals()
    {
        if (thisImage != null)
            thisImage.sprite = thisSprite;

        if (thisText != null)
            thisText.text = thisString;
    }

}
