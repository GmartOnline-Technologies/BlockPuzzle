using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keep this component on active StartupControllers, outside the panels it hides.
public class BootstrapManager : MonoBehaviour
{
    [Header("References")]
    public RegistrationController registration;
    public LoadingScreen loadingScreen;
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingStatusText;
    public AppConfig config;
    [Header("UI Panels")]
    public GameObject languagePanel;
    public GameObject registrationPanel;
    [Header("Scene Config")]
    public string mainMenuName = "HomeScene";
    public string onboardingName = "Tutorial";
    [Header("Update Popup")]
    public GameObject updatePopup;
    public Button updateNowButton;
    public GameObject closeButton;
    [Header("Existing project integrations")]
    public LanguageEvent languageSelected = new LanguageEvent();
    public ConfigEvent configReady = new ConfigEvent();
    [Serializable] public class LanguageEvent : UnityEvent<int> { }
    [Serializable] public class ConfigEvent : UnityEvent<AppConfig> { }
    [Min(1)] public int requestTimeoutSeconds = 30;
    private bool navigating, ready, languageChosen;
    private Button closePopupButton;

    [Serializable] public class AppSettingsResponse { public bool success; public AppSettingsData data; }
    [Serializable] public class AppSettingsData
    {
        public int app_id;
        public string app_name, sub_text;
        public bool is_sub_text, is_unsub;
        public string android_version, ios_version;
        public bool update_required;
    }

    private void Start()
    {
        Application.targetFrameRate = 30;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Time.timeScale = 1f;
        if (languagePanel != null) languagePanel.SetActive(false);
        if (registrationPanel != null) registrationPanel.SetActive(false);
        if (updatePopup != null) updatePopup.SetActive(false);
        if (closeButton != null) closePopupButton = closeButton.GetComponent<Button>();
        if (closePopupButton != null) closePopupButton.onClick.AddListener(OnCloseUpdatePopup);
        if (updateNowButton != null) updateNowButton.onClick.AddListener(OnUpdateClicked);
        if (loadingPanel != null) { loadingPanel.SetActive(true); loadingPanel.transform.SetAsLastSibling(); }
        if (loadingScreen != null) { loadingScreen.BeginLoading(); loadingScreen.SetProgress(0.15f); }
        if (config == null)
        {
            Debug.LogError("AppConfig is missing on BootstrapManager!");
            SetStatus("Assign AppConfig on BootstrapManager.");
            return;
        }
        // Optional compile-time bridge if these instructor systems exist in this game.
#if BLOCK_PUZZLE_INSTRUCTOR_SERVICES
        EconomyAPI.Config = config;
        LeaderboardAPI.Config = config;
#endif
        configReady.Invoke(config);
        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        while (Application.internetReachability == NetworkReachability.NotReachable)
        { SetStatus("Waiting for internet..."); yield return null; }
        SetStatus("Loading...");
        if (loadingScreen != null) loadingScreen.SetProgress(0.65f);
        string url = config.baseApiUrl.Trim().TrimEnd('/') + "/apps/settings/by-token";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.timeout = Mathf.Max(1, requestTimeoutSeconds);
            www.SetRequestHeader("Authorization", "Bearer " + config.apiAuthToken.Trim());
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Debug.Log($"[Bootstrap] Settings: {www.downloadHandler.text}");
                    AppSettingsResponse response = JsonUtility.FromJson<AppSettingsResponse>(www.downloadHandler.text);
                    if (response != null && response.success && response.data != null)
                    {
                        PlayerPrefs.SetString("Settings_SubText", response.data.sub_text ?? "");
                        PlayerPrefs.SetInt("Settings_IsSubText", response.data.is_sub_text ? 1 : 0);
                        PlayerPrefs.SetInt("Settings_IsUnsub", response.data.is_unsub ? 1 : 0);
                        PlayerPrefs.SetString("Settings_AndroidVersion", response.data.android_version ?? "");
                        PlayerPrefs.SetString("Settings_IOSVersion", response.data.ios_version ?? "");
                        PlayerPrefs.SetInt("Settings_UpdateRequired", response.data.update_required ? 1 : 0);
                        PlayerPrefs.Save();
                        Debug.Log($"[Bootstrap] Settings Saved. Server Version: {response.data.android_version}");
                    }
                }
                catch (Exception e) { Debug.LogError($"[Bootstrap] Parse Error: {e.Message}"); }
            }
            else { Debug.LogError($"[Bootstrap] API Error: {www.error}"); }
        }
        if (loadingScreen != null && loadingScreen.isActiveAndEnabled)
            yield return loadingScreen.CompleteLoading();
        if (loadingPanel != null) loadingPanel.SetActive(false);
        ready = true;
        if (!DetermineUpdateStatus()) RouteAfterStartup();
    }

    private void RouteAfterStartup()
    {
        if (navigating) return;
        // Explicit logout takes precedence over onboarding flags.
        if (PlayerPrefs.GetInt("Auth_LoggedOut", 0) == 1) { CheckRegistrationStatus(); return; }
        if (!PlayerPrefs.HasKey("SelectedLanguage")) { ShowLanguagePanel(); return; }
        if (PlayerPrefs.GetInt("TutorialFinished", 0) != 1) { LoadScene(onboardingName); return; }
        CheckRegistrationStatus();
    }

    private void ShowLanguagePanel()
    {
        if (languagePanel == null) { Debug.LogError("Assign Language Panel.", this); return; }
        languagePanel.SetActive(true);
        languagePanel.transform.localScale = Vector3.zero;
        languagePanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void OnLanguageSelected(int languageIndex)
    {
        if (!ready || navigating || languageChosen) return;
        languageChosen = true;
        PlayerPrefs.SetInt("SelectedLanguage", languageIndex);
        PlayerPrefs.Save();
        // Hook the game's Localization component SetLanguage(int), dynamic int, in Inspector.
        languageSelected.Invoke(languageIndex);
        LoadScene(onboardingName);
    }

    private void CheckRegistrationStatus()
    {
        bool loggedIn = PlayerPrefs.GetInt("IsRegisteredUser", 0) == 1 &&
            !string.IsNullOrEmpty(PlayerPrefs.GetString("AccessToken", "")) &&
            PlayerPrefs.GetInt("Auth_LoggedOut", 0) == 0;
        if (loggedIn) { LoadScene(mainMenuName); return; }
        if (registration != null) registration.OpenRegistration();
        else Debug.LogError("Assign the Bootstrap RegistrationController.", this);
    }

    private bool DetermineUpdateStatus()
    {
        string serverVersion = PlayerPrefs.GetString("Settings_AndroidVersion", "");
        bool isForceUpdate = PlayerPrefs.GetInt("Settings_UpdateRequired", 0) == 1;
        if (string.IsNullOrEmpty(serverVersion) || Application.version == serverVersion) return false;
        if (updatePopup == null)
        {
            if (isForceUpdate) Debug.LogError("A mandatory update is required. Assign Update Popup and Update Now Button.", this);
            return isForceUpdate;
        }
        updatePopup.SetActive(true);
        if (closeButton != null) closeButton.SetActive(!isForceUpdate);
        // Wait for dismissal of optional updates, so another scene cannot hide the popup.
        return true;
    }

    private void OnCloseUpdatePopup()
    {
        if (!ready || PlayerPrefs.GetInt("Settings_UpdateRequired", 0) == 1) return;
        if (updatePopup != null) updatePopup.SetActive(false);
        RouteAfterStartup();
    }

    private void OnUpdateClicked()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IPHONE
#endif
        Debug.Log("Opening Play Store...");
    }

    private void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError("Add scene to Build Settings: " + sceneName, this); languageChosen = false; return; }
        navigating = true;
        SceneManager.LoadScene(sceneName);
    }
    private void SetStatus(string message) { if (loadingStatusText != null) loadingStatusText.text = message; }
    private void OnDestroy()
    {
        if (closePopupButton != null) closePopupButton.onClick.RemoveListener(OnCloseUpdatePopup);
        if (updateNowButton != null) updateNowButton.onClick.RemoveListener(OnUpdateClicked);
    }
}
