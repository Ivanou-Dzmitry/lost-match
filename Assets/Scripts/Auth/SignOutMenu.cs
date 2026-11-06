using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LM.UI;

/// <summary>
/// The "login" panel. Opened at start only when the Google Play Games sign-in failed:
/// shows the player name and the "Sign in" button (the old "log out" button).
/// </summary>
public class SignOutMenu : Panel
{
    [SerializeField] private Button logoutButton = null;
    [SerializeField] private TMP_Text nameText = null;

    private GameData gameDataClass;

    public override void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        logoutButton.onClick.AddListener(SignIn);
        base.Initialize();
    }

    public override void Open()
    {
        UpdatePlayerNameUI();
        base.Open();
    }

    private async void SignIn()
    {
        logoutButton.interactable = false;

        await PlayServices.SignInAsync(interactive: true);

        // The panel can be destroyed while the dialog is open
        if (this == null)
        {
            return;
        }

        logoutButton.interactable = true;
        UpdatePlayerNameUI();
    }

    private void UpdatePlayerNameUI()
    {
        bool signedIn = PlayServices.IsSignedIn;
        string playerName = PlayServices.PlayerName;

        nameText.text = playerName;

        // Description above the name (child "Descript txt" of the panel)
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == "Descript txt")
            {
                text.text = signedIn
                    ? "You are signed in to Google Play Games as:"
                    : "Not signed in to Google Play Games. You play as:";
                break;
            }
        }

        // Button is only a way to sign in again
        logoutButton.gameObject.SetActive(!signedIn);
        TMP_Text label = logoutButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "Sign in";
        }

        if (!signedIn)
        {
            return;
        }

        GameObject gameDataObject = GameObject.FindWithTag("GameData");
        gameDataClass = gameDataObject != null ? gameDataObject.GetComponent<GameData>() : null;

        if (gameDataClass != null && gameDataClass.saveData != null)
        {
            gameDataClass.saveData.playerName = playerName;
            gameDataClass.SaveToFile();
        }
    }

}
