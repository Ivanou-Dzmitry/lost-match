using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LM.UI;
using Unity.Services.Authentication;

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

        logoutButton.onClick.AddListener(SignOut);
        base.Initialize();

    }

    public override void Open()
    {
        UpdatePlayerNameUI();
        base.Open();
    }

    private void SignOut()
    {
        MenuManager.Singleton.SignOut();
    }

    private void UpdatePlayerNameUI()
    {        
        string playerName = AuthenticationService.Instance.PlayerName;

        nameText.text = playerName;

        gameDataClass = GameObject.FindWithTag("GameData").GetComponent<GameData>();

        if (gameDataClass != null)
        {
            gameDataClass.saveData.playerName = playerName;
            gameDataClass.SaveToFile();
        }
    }

}