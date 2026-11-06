using UnityEngine;

/// <summary>
/// Main menu start-up: signs the player in with Google Play Games. The "login" panel
/// is opened only when the sign-in fails. The game works offline in that case.
/// </summary>
public class MenuManager : MonoBehaviour
{
    private bool initialized = false;

    private static MenuManager singleton = null;

    public ParticleSystem particle;

    public static MenuManager Singleton
    {
        get
        {
            if (singleton == null)
            {
                singleton = FindFirstObjectByType<MenuManager>();
                singleton.Initialize();
            }
            return singleton;
        }
    }

    private void Initialize()
    {
        if (initialized) { return; }
        initialized = true;
    }

    private void OnDestroy()
    {
        if (singleton == this)
        {
            singleton = null;
        }
    }

    private void Awake()
    {
        Application.runInBackground = true;

        if(particle != null)
            particle.Pause();

        StartClientService();
    }

    public async void StartClientService()
    {
        PanelManager.CloseAll();

        // Silent automatic sign-in. If it fails the player can use the "Sign in" button on the panel.
        await PlayServices.SignInAsync();

        // The scene may have been left while the sign-in was running
        if (this == null)
            return;

        // Signed in: nothing to show, Google shows its own welcome message.
        // Not signed in: the panel explains it and offers the "Sign in" button.
        PanelManager.CloseAll();
        if (PlayServices.IsSignedIn)
        {
            SavePlayerName();
        }
        else
        {
            PanelManager.Open("login");
        }
    }

    // Keeps the Google name in the save, so it is shown even when the next start is offline
    private void SavePlayerName()
    {
        GameObject gameDataObject = GameObject.FindWithTag("GameData");
        GameData gameData = gameDataObject != null ? gameDataObject.GetComponent<GameData>() : null;

        if (gameData != null && gameData.saveData != null)
        {
            gameData.saveData.playerName = PlayServices.PlayerName;
            gameData.SaveToFile();
        }
    }
}
