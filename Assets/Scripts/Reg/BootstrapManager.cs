using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using System;

public class BootstrapManager : MonoBehaviour
{
    [Header("API Config")]
    public AppConfig config;
    
    [Header("Panels")]
    public GameObject languagePanel;
    public GameObject loadingPanel;

    [Serializable]
    public class AppSettingsResponse
    {
        public bool success;
        public AppSettingsData data;
    }

    [Serializable]
    public class AppSettingsData
    {
        public int app_id;
        public string app_name;
        public string sub_text;
        public bool is_sub_text;
        public bool is_unsub;
        public string android_version;
        public string ios_version;
        public bool update_required;
    }

    void Start()
    {
        Application.targetFrameRate = 30;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (languagePanel != null) languagePanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);

        StartCoroutine(StartupSequence());
    }

    IEnumerator StartupSequence()
    {
        // 1. Wait for Internet connection
        while (Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield return null; 
        }

        // 2. Start the API Request
        string url = config.baseApiUrl.TrimEnd('/') + "/apps/settings/by-token";
        UnityWebRequest www = new UnityWebRequest(url, "GET");
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", "Bearer " + config.apiAuthToken.Trim());
        
        UnityWebRequestAsyncOperation requestOp = www.SendWebRequest();

        // 3. Wait for the API Request to finish
        if (!requestOp.isDone) yield return requestOp;

        // 4. Process the API Data
        if (www.result == UnityWebRequest.Result.Success)
        {
            try
            {
                Debug.Log($"[Bootstrap] Settings: {www.downloadHandler.text}");
                AppSettingsResponse response = JsonUtility.FromJson<AppSettingsResponse>(www.downloadHandler.text);
                
                if (response != null && response.success && response.data != null)
                {
                    PlayerPrefs.SetString("Settings_SubText", response.data.sub_text);
                    PlayerPrefs.SetInt("Settings_IsSubText", response.data.is_sub_text ? 1 : 0);
                    PlayerPrefs.SetInt("Settings_IsUnsub", response.data.is_unsub ? 1 : 0);
                    PlayerPrefs.SetString("Settings_AndroidVersion", response.data.android_version);
                    PlayerPrefs.SetInt("Settings_UpdateRequired", response.data.update_required ? 1 : 0);
                    PlayerPrefs.Save();
                    
                    Debug.Log($"[Bootstrap] Settings Saved. Server Version: {response.data.android_version}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] Parse Error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[Bootstrap] API Error: {www.error}");
        }

        // 5. Hide Loading Bar
        if (loadingPanel != null) loadingPanel.SetActive(false);

        // 6. Routing Logic
        InitialCheck();
    }

    void InitialCheck()
    {
        bool hasLanguageSet = PlayerPrefs.HasKey("SelectedLanguage");
        bool hasFinishedTutorial = PlayerPrefs.GetInt("TutorialFinished", 0) == 1;
        bool isLoggedOut = PlayerPrefs.GetInt("IsLoggedOut", 0) == 1;
        bool isRegistered = PlayerPrefs.GetInt("IsRegisteredUser", 0) == 1;

        if (isLoggedOut || (hasFinishedTutorial && !isRegistered))
        {
            // Show the Login/Welcome Back Panel in the Bootstrap Scene
            if (RegistrationController.Instance != null)
            {
                RegistrationController.Instance.OpenRegistration();
            }
        }
        else if (isRegistered)
        {
            // Already logged in, jump straight to the game
            SceneManager.LoadScene("HomeScene");
        }
        else if (!hasLanguageSet)
        {
            // Brand new player -> Show Language Selection
            if (languagePanel != null) languagePanel.SetActive(true);
        }
        else
        {
            // Language is set but tutorial isn't finished -> Go to Tutorial
            SceneManager.LoadScene("Tutorial");
        }
    }

    // Called by the English/Sinhala buttons
    public void OnLanguageSelected(int languageIndex)
    {
        PlayerPrefs.SetInt("SelectedLanguage", languageIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Tutorial");
    }
}