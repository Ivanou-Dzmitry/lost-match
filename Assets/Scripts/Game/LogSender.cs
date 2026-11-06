using System;
using System.IO;
using TMPro;
using UnityEngine;

public class LogSender : MonoBehaviour
{
    public TMP_Text logText;

    // Intent extras have a size limit (binder transaction ~1 MB), so only the tail of the log is sent.
    private const int MaxSharedChars = 100000;

    public void SendLogFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "LM_GameSessionLog.csv");

        if (!File.Exists(filePath))
        {
            logText.text = "Log file not found!";
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string newFileName = $"{timestamp}_LM_GameSessionLog.csv";

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 10+ (scoped storage) does not allow writing to /storage/emulated/0/Download directly,
            // so hand the log text to the system share sheet instead. No storage permission needed.
            string text = File.ReadAllText(filePath);
            if (text.Length > MaxSharedChars)
                text = text.Substring(text.Length - MaxSharedChars);

            ShareTextAndroid(newFileName, text);
            logText.text = "Choose where to send the log.";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string destinationPath = Path.Combine(downloadsFolder, newFileName);

            File.Copy(filePath, destinationPath, true);
            logText.text = "Log file saved to Downloads folder: " + destinationPath;
#else
            logText.text = "Unsupported platform.";
#endif
        }
        catch (Exception ex)
        {
            logText.text = "Error sending log file: " + ex.Message;
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void ShareTextAndroid(string subject, string text)
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = new AndroidJavaObject("android.content.Intent"))
        using (var intentClass = new AndroidJavaClass("android.content.Intent"))
        {
            intent.Call<AndroidJavaObject>("setAction", "android.intent.action.SEND").Dispose();
            intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
            intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", subject).Dispose();
            intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text).Dispose();

            using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Send log"))
            {
                activity.Call("startActivity", chooser);
            }
        }
    }
#endif
}
