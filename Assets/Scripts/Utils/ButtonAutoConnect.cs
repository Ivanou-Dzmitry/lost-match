using UnityEngine;
using UnityEngine.UI;

public class ButtonAutoConnect : MonoBehaviour
{
    private void Awake()
    {
        Button btn = GetComponent<Button>();

        if (btn != null && SoundManager.Instance != null)
        {
            btn.onClick.AddListener(SoundManager.Instance.ButtonClick);
        }        
    }
}
