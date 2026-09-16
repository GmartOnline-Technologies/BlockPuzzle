using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootstrapManager : MonoBehaviour
{
    public AppConfig config;
    public RegistrationController registration;
    [Header("Splash and loading")]
    public GameObject splashPanel, loadingBarRoot;
    public LoadingScreen loadingScreen;
    public TMP_Text loadingText;
    public float splashOnlySeconds = 1.5f;
    public float minimumLoadingSeconds = 2f;
    public Button retryButton;
    [Header("Panels")]
    public GameObject updatePopup, closeButton;
    public Button updateNowButton;
    [Header("Scenes")]
    public string onboardingName = "Tutorial";
    public string mainMenuName = "HomeScene";
    public bool WasRegisteredAtLaunch { get; private set; }
    private bool starting, changingScene, forcedUpdate;
    private UnityWebRequest activeRequest;
    [Serializable] private class SettingsResponse { public bool success; public SettingsData data; }
    [Serializable] private class SettingsData
    {
        public string sub_text, android_version, ios_version;
        public bool is_sub_text, is_unsub, update_required;
    }
    private void Start()
    {
        if (registration == null) registration = RegistrationController.Instance;
        if (retryButton != null) retryButton.onClick.AddListener(RetryStartup);
        if (updateNowButton != null) updateNowButton.onClick.AddListener(OnUpdateClicked);
        if (closeButton != null && closeButton.GetComponent<Button>() != null)
            closeButton.GetComponent<Button>().onClick.AddListener(OnCloseUpdatePopup);
        RetryStartup();
    }
    public void RetryStartup()
    {
        if (!starting && !changingScene) StartCoroutine(Startup());
    }
    private IEnumerator Startup()
    {
        starting = true; forcedUpdate = false;
        Active(updatePopup, false);
        if (registration != null) registration.HidePanels();
        Active(splashPanel, true); Active(loadingBarRoot, false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);

        if (loadingText != null) loadingText.text = "Loading...";
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, splashOnlySeconds));
        Active(loadingBarRoot, true);
        if (loadingScreen != null) loadingScreen.BeginLoading();
        WasRegisteredAtLaunch = PlayerPrefs.GetInt("IsRegisteredUser", 0) == 1;
        if (config == null || registration == null || string.IsNullOrWhiteSpace(config.baseApiUrl) || string.IsNullOrWhiteSpace(config.apiAuthToken))
        { StartupError("Assign AppConfig and RegistrationController, then retry."); yield break; }
        SettingsResponse settings = null;
        using (UnityWebRequest request = UnityWebRequest.Get(config.baseApiUrl.TrimEnd('/') + "/apps/settings/by-token"))
        {
            activeRequest = request; request.timeout = 20;
            request.SetRequestHeader("Authorization", "Bearer " + config.apiAuthToken.Trim());
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            float elapsed = 0f;
            float minimum = Mathf.Max(0.1f, minimumLoadingSeconds);
            while (!operation.isDone || elapsed < minimum)
            {
                elapsed += Time.unscaledDeltaTime;
                if (loadingScreen != null) loadingScreen.SetProgress(Mathf.Min(0.9f, elapsed / minimum * 0.9f));
                yield return null;
            }
            activeRequest = null;
            if (request.result == UnityWebRequest.Result.Success)
            { try { settings = JsonUtility.FromJson<SettingsResponse>(request.downloadHandler.text); } catch (Exception) { } }
        }
        if (settings == null || !settings.success || settings.data == null)
        { StartupError("Unable to load game settings. Check your connection and tap Retry."); yield break; }
        SettingsData data = settings.data;
        PlayerPrefs.SetString("Settings_SubText", data.sub_text ?? "");
        PlayerPrefs.SetInt("Settings_IsSubText", data.is_sub_text ? 1 : 0);
        PlayerPrefs.SetInt("Settings_IsUnsub", data.is_unsub ? 1 : 0);
        PlayerPrefs.SetString("Settings_AndroidVersion", data.android_version ?? "");
        PlayerPrefs.SetInt("Settings_UpdateRequired", data.update_required ? 1 : 0);
        PlayerPrefs.Save();
        if (loadingScreen != null) yield return loadingScreen.CompleteLoading();
        starting = false;
        string requiredVersion = Application.platform == RuntimePlatform.IPhonePlayer ? data.ios_version : data.android_version;
        if (!string.IsNullOrWhiteSpace(requiredVersion) && requiredVersion != Application.version)
        {
            forcedUpdate = data.update_required;
            if (updatePopup != null)
            { PopupMotion.Show(updatePopup); Active(closeButton, !forcedUpdate); yield break; }
            if (forcedUpdate) { StartupError("An app update is required. Configure the update panel."); yield break; }
        }
        OpenLogin();
    }
    private void StartupError(string message)
    {
        starting = false;
        if (loadingText != null) loadingText.text = message;
        if (retryButton != null) retryButton.gameObject.SetActive(true);
    }
    private void OpenLogin()
    {
        Active(loadingBarRoot, false); Active(splashPanel, false);
        // Both groups see Login/Register. The local preference is a hint, not authentication.
        registration.OpenLogin();
    }
    public void OnRegisterClicked()
    {
        if (starting || changingScene || registration == null || registration.IsBusy || forcedUpdate) return;
        LoadScene(onboardingName);
    }
    public void OnStartClicked()
    {
        if (registration == null || !registration.IsAuthenticated) return;
        LoadScene(mainMenuName);
    }
    private void LoadScene(string name)
    {
        if (changingScene) return;
        if (!Application.CanStreamedLevelBeLoaded(name))
        { Debug.LogError("Add scene to the build scene list: " + name, this); return; }
        changingScene = true; Time.timeScale = 1f; SceneManager.LoadSceneAsync(name);
    }
    public void OnCloseUpdatePopup()
    { if (forcedUpdate) return; Active(updatePopup, false); OpenLogin(); }
    public void OnUpdateClicked()
    {
        // Matches the instructor's Android update route; no Store URL field needed.
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#endif
    }
    private static void Active(GameObject target, bool value) { if (target != null) target.SetActive(value); }
    private void OnDestroy()
    {
        if (activeRequest != null) activeRequest.Abort();
        if (retryButton != null) retryButton.onClick.RemoveListener(RetryStartup);
        if (updateNowButton != null) updateNowButton.onClick.RemoveListener(OnUpdateClicked);
        if (closeButton != null && closeButton.GetComponent<Button>() != null)
            closeButton.GetComponent<Button>().onClick.RemoveListener(OnCloseUpdatePopup);
    }
}
