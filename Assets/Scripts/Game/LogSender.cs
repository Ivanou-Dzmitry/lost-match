using System;
using System.IO;
using TMPro;
using UnityEngine;

public class LogSender : MonoBehaviour
{
    public TMP_Text logText;

    public void SendLogFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "LM_GameSessionLog.csv");

        // Format the current date and time as "yyyyMMdd_HHmmss"
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string newFileName = $"{timestamp}_LM_GameSessionLog.csv";

        string destinationPath = Path.Combine("/storage/emulated/0/Download", newFileName);

        #if UNITY_ANDROID
                // On Android, save to the Downloads folder
                destinationPath = Path.Combine("/storage/emulated/0/Download", newFileName);
        #elif UNITY_STANDALONE_WIN
                // On PC (Windows), save to the user's Downloads folder
                string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                destinationPath = Path.Combine(downloadsFolder, newFileName);
        #else
                logText.text = "Unsupported platform.";
                return;
        #endif

        try
        {
            if (File.Exists(filePath))
            {
                File.Copy(filePath, destinationPath, true);
                logText.text = "Log file saved to Downloads folder: " + destinationPath;
            }
            else
            {
                logText.text = "Log file not found!";
            }
        }
        catch (Exception ex)
        {
            logText.text = "Error saving log file: " + ex.Message;
        }
    }

}

