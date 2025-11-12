using LM.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthentificationMenu : Panel
{
    [SerializeField] private TMP_InputField usernameInput = null;
    [SerializeField] private TMP_InputField passwordInput = null; 
    [SerializeField] private Button signinButton = null;
    [SerializeField] private Button signupButton = null; 
    [SerializeField] private Button anonymousButton = null; 


    public override void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        signinButton.onClick.AddListener(SignIn);
        signupButton.onClick.AddListener(SignUp);
        anonymousButton.onClick.AddListener(AnonymousSignIn);
        base.Initialize();
    }

    public override void Open()
    {
        usernameInput.text = "";
        passwordInput.text = "";
        base.Open();
    }

    private void AnonymousSignIn()
    {
        MenuManager.Singleton.SignInAnonymouslyAsync();
    }

    private void SignIn()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) == false && string.IsNullOrEmpty(password)== false)
        {
            MenuManager.Singleton.SignInWithUsernameAndPasswordAsync(username, password);
        }
       
    }

    private void SignUp()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        Debug.Log($"SignUp attempt with username: {username} and password: {password}");

        if (string.IsNullOrEmpty(username) == false && string.IsNullOrEmpty(password) == false)
        {
            if(IsPasswordValid(password))
            {
                MenuManager.Singleton.SignUpWithUsernameAndPasswordAsync(username, password);
            }
            else
            {
                ErrorMenu panel = (ErrorMenu)PanelManager.GetSingleton("error");
                panel.Open(ErrorMenu.Action.None, "Password must be 8-30 characters long and include at least one uppercase letter, one lowercase letter, one digit, and one symbol.", "OK");
            }
        }

    }

    private bool IsPasswordValid(string password)
    {
        if (password.Length < 8 || password.Length > 30)
        {
            return false;
        }

        bool hasUppercase = false;
        bool hasLowercase = false;
        bool hasDigit = false;
        bool hasSymbol = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c))
            {
                hasUppercase = true;
            }

            else if (char.IsLower(c))
            {
                hasLowercase = true;
            }

            else if (char.IsDigit(c))
            {
                hasDigit = true;
            }

            else if (!char.IsLetterOrDigit(c))
            {
                hasSymbol = true;
            }
        }

        return hasUppercase && hasLowercase && hasDigit && hasSymbol;
    }

}
