using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;
using Firebase.Analytics;
// using Facebook.Unity;
using UnityEngine.SceneManagement;

public class RegistrationController : MonoBehaviour
{
    public static RegistrationController Instance;

    [Header("References")]
    public string homeSceneName = "HomeScene";
    public string tutorialSceneName = "Tutorial";
    public GameObject welcomePanel;
    public Button startButton;
    public LoadingScreen loadingScreen;
    public OtpDigitInputs otpDigits;
    [Min(1)] public int requestTimeoutSeconds = 30;
    private bool busy, navigating;
    private float resendAvailableAt;
    private Coroutine resendRoutine;

    [Header("API Config")]
    public AppConfig config;

    [Header("Panels")]
    public GameObject registrationPanel;
    public GameObject detailsPanel;       
    public GameObject otpPanel;
    public GameObject loadingOverlay;

    [Header("Details Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField phoneInput;
    public Button sendOtpButton;
    public TextMeshProUGUI detailsWarningText;
    public TextMeshProUGUI subInfoText;

    [Header("OTP Inputs")]
    public TMP_InputField singleOtpInput;
    public Button verifyOtpButton;
    public Button resendOtpButton;
    public TextMeshProUGUI otpMessageText; 

    // Internal State
    private string currentMobile10Digit;
    private string tempUsername;
    private string referenceNo; 
    private string subscriptionId; 

    private float lastBackPressTime = 0f;
    private const float exitDoubleTapDelay = 2.0f;

    // --- JSON CLASSES ---
    [Serializable] private class OtpRequest { public string tel; }
    
    [Serializable] 
    private class OtpResponse 
    { 
        public string statusCode; 
        public string referenceNo; 
        public string statusDetail; 
    }

    [Serializable] private class VerifyRequest { public string referenceNo; public string otp; }
    
    [Serializable] 
    private class VerifyResponse 
    { 
        public string statusCode; 
        public string subscriptionId; 
        public string statusDetail; 
    }

    [Serializable]
    public class UserRegisterRequest
    {
        public string username;
        public string phoneNumber;
        public string countryCode;
        public string country;
        public string platformName;
        public string email;
        public string appName;
    }

    [Serializable]
    public class UserRegisterResponse { public UserData user; }

    [Serializable]
    public class UserData { public int id; public string username; public string authToken; }
    // -----------------------------------------------------------

    void Awake() 
    { 
        Instance = this; 
        if(detailsPanel != null) detailsPanel.SetActive(false);
        if(otpPanel != null) otpPanel.SetActive(false);
        if(welcomePanel != null) welcomePanel.SetActive(false);
        
        if(detailsWarningText != null) detailsWarningText.text = "";
    }

    void Start()
    {
        if (sendOtpButton != null) sendOtpButton.onClick.AddListener(OnSendOtpClicked);
        if (verifyOtpButton != null) verifyOtpButton.onClick.AddListener(OnVerifyClicked);
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if(resendOtpButton != null) resendOtpButton.onClick.AddListener(OnResendOtpClicked);

        if (singleOtpInput != null)
        {
            singleOtpInput.onValueChanged.AddListener(OnOtpValueChanged);
            singleOtpInput.characterLimit = 6;
            singleOtpInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        RefreshButtons();
        SetupSubscriptionInfo();
    }

    private void LogGameEvent(string eventName, string parameterName = null, string parameterValue = null)
    {
        // 1. Log to Firebase
        if (string.IsNullOrEmpty(parameterName))
            FirebaseAnalytics.LogEvent(eventName);
        else
            FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
    }

    void OnOtpValueChanged(string val)
    {
        RefreshButtons();
        // Verification starts only when the player clicks the Continue button.
    }

    bool IsValidOtp()
    {
        string value = singleOtpInput != null ? singleOtpInput.text : "";
        if (value.Length != 6) return false;
        foreach (char digit in value) if (digit < '0' || digit > '9') return false;
        return true;
    }

    void SetupSubscriptionInfo()
    {
        if (subInfoText != null)
        {
            bool isVisible = PlayerPrefs.GetInt("Settings_IsSubText", 0) == 1;
            string textContent = PlayerPrefs.GetString("Settings_SubText", "");


            if (isVisible && !string.IsNullOrEmpty(textContent))
            {
                subInfoText.text = textContent;
                subInfoText.gameObject.SetActive(true);
            }
            else
            {
                subInfoText.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (busy || (loadingOverlay != null && loadingOverlay.activeSelf)) return;

            if (otpPanel != null && otpPanel.activeSelf)
            {
                OnBackToDetails();
                return;
            }

            if (detailsPanel != null && detailsPanel.activeSelf)
            {
                if (Time.unscaledTime - lastBackPressTime < exitDoubleTapDelay)
                {
                    
                    Debug.Log("Exiting Application...");
                    Application.Quit();
                }
                else
                {
                    lastBackPressTime = Time.unscaledTime;
                    ShowToast("Tap again to exit");
                }
            }
        }
    }

    void OnBackToDetails()
    {
        if (busy) return;
        Debug.Log("Back button pressed: Returning to Details.");
        otpPanel.SetActive(false);
        OpenRegistration();
    }

    public void OnBackClickedToDetails()
    {
        if (busy) return;
        Debug.Log("Back button pressed: Returning to Details.");
        otpPanel.SetActive(false);
        OpenRegistration();
    }


    public void OpenRegistration()
    {
        if (busy) return;
        if (detailsPanel == null) { Debug.LogError("Assign Details Panel on RegistrationController.", this); return; }
        if (registrationPanel != null) registrationPanel.SetActive(true);
        if (otpPanel != null) otpPanel.SetActive(false);
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
        referenceNo = null;
        if (otpDigits != null) otpDigits.Clear();
        else if (singleOtpInput != null) singleOtpInput.text = "";
        SetupSubscriptionInfo();
        RefreshButtons();
        detailsPanel.SetActive(true);
        detailsPanel.transform.localScale = Vector3.zero;
        detailsPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        if(detailsWarningText != null) detailsWarningText.text = "";
    }

    public void OnSendOtpClicked()
    {
        Debug.Log("[UI] Registration Continue clicked.", this);
        if (busy)
        {
            Debug.LogWarning("[UI] Continue ignored: a request is already in progress.", this);
            return;
        }
        if (config == null || usernameInput == null || phoneInput == null || otpPanel == null || singleOtpInput == null || welcomePanel == null)
        {
            const string message = "Assign AppConfig, details inputs, OTP Panel, Single Otp Input and Welcome Panel.";
            Debug.LogError(message, this);
            ShowWarning(message);
            return;
        }

        if(detailsWarningText != null) detailsWarningText.text = "";

        tempUsername = usernameInput.text.Trim();
        string rawPhone = phoneInput.text.Trim();

        if (string.IsNullOrEmpty(tempUsername) || string.IsNullOrEmpty(rawPhone))
        {
            ShowWarning("Please fill in all fields");
            return;
        }

        string processedPhone;
        if (!TryNormalizePhone(rawPhone, out processedPhone))
        {
            ShowWarning("Enter a valid number: 07XXXXXXXX or +947XXXXXXXX.");
            return;
        }
        Debug.Log("[Auth] Details accepted; mobile normalized to local 10-digit format.", this);

        if (processedPhone == "0770000000" || processedPhone == "0770000001")
        {
            Debug.LogWarning("[AUTH BYPASS] Test number detected!");
            string fakeSubId = "test_" + processedPhone;
            if (processedPhone == "0770000001")
            {
                PlayerPrefs.SetInt("PlayerGems", 10000);
                PlayerPrefs.Save();
            }
            CompleteRegistration(tempUsername, processedPhone, fakeSubId, fakeSubId);
            return;
        }
        currentMobile10Digit = processedPhone;
        StartCoroutine(SendOtpRequest());
    }

    // The backend still receives the same local 07XXXXXXXX value as the instructor's script.
    public static bool TryNormalizePhone(string value, out string normalized)
    {
        string phone = System.Text.RegularExpressions.Regex.Replace(value ?? "", @"[\s()\-]", "");
        if (phone.StartsWith("+94", StringComparison.Ordinal)) phone = phone.Substring(3);
        else if (phone.StartsWith("94", StringComparison.Ordinal)) phone = phone.Substring(2);
        if (phone.Length == 9 && phone.StartsWith("7", StringComparison.Ordinal)) phone = "0" + phone;
        normalized = phone;
        if (phone.Length != 10 || !phone.StartsWith("07", StringComparison.Ordinal)) return false;
        foreach (char digit in phone) if (digit < '0' || digit > '9') return false;
        return true;
    }

    void OnResendOtpClicked()
    {
        if (busy || Time.unscaledTime < resendAvailableAt || string.IsNullOrEmpty(currentMobile10Digit)) return;
        Debug.Log("Resending OTP...");
        if (otpDigits != null) otpDigits.Clear();
        else if (singleOtpInput != null) singleOtpInput.text = "";
        resendAvailableAt = Time.unscaledTime + 30f;
        if (resendRoutine != null) StopCoroutine(resendRoutine);
        resendRoutine = StartCoroutine(ResendCooldownRoutine());
        StartCoroutine(SendOtpRequest());
    }

    private IEnumerator ResendCooldownRoutine()
    {
        // Use button availability for the timer; don't overwrite server error messages.
        while (Time.unscaledTime < resendAvailableAt)
        {
            RefreshButtons();
            yield return new WaitForSecondsRealtime(0.25f);
        }
        resendRoutine = null;
        RefreshButtons();
    }

    private IEnumerator SendOtpRequest()
    {
        SetLoading(true);

        string url = config.baseApiUrl.Trim().TrimEnd('/') + "/subscriptions/request";
        
        OtpRequest reqData = new OtpRequest { tel = currentMobile10Digit };
        string jsonBody = JsonUtility.ToJson(reqData);

        using (UnityWebRequest www = CreateAuthenticatedPostRequest(url, jsonBody))
        {
            yield return www.SendWebRequest();
            yield return FinishLoading();
            SetLoading(false);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Auth] Error: {www.error} : {www.downloadHandler.text}");

                if (detailsPanel.activeSelf) ShowWarning("Connection Failed.");
                else ShowToast("Connection Failed.");
            }
            else
            {
                try 
                {
                    Debug.Log($"[Auth] Response: {www.downloadHandler.text}");
                    OtpResponse response = JsonUtility.FromJson<OtpResponse>(www.downloadHandler.text);

                    if (response == null) throw new Exception("Empty server response");
                    if (response.statusCode == "S1000") 
                    {
                        LogGameEvent("Pin_sent", "status", "success");
                        if (string.IsNullOrEmpty(response.referenceNo)) throw new Exception("Missing OTP reference");
                        referenceNo = response.referenceNo;
                        ShowToast("OTP Sent Successfully!");
                        SwitchToOtpPanel();
                    }
                    else if (response.statusCode == "S2000")
                    {
                        LogGameEvent("auth_direct_login", "mode", "S2000");
                        Debug.Log("[Auth] User already subscribed. Logging in directly.");
                        ShowToast("Welcome back!");
                        CompleteRegistration(tempUsername, currentMobile10Digit, response.referenceNo, response.referenceNo);
                    }
                    else
                    {
                        if (detailsPanel.activeSelf) ShowWarning(response.statusDetail);
                        else ShowToast(response.statusDetail);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Auth] JSON Parse Error: {e.Message}");
                    ShowWarning("Server response error.");
                }
            }
        }
    }


    void OnVerifyClicked()
    {
        if (busy) return;
        if (!IsValidOtp()) { ShowWarning("Invalid OTP Length"); return; }
        if (string.IsNullOrEmpty(referenceNo)) { ShowWarning("Request an OTP first."); return; }
        StartCoroutine(VerifyOtpRequest(singleOtpInput.text));
    }

    private IEnumerator VerifyOtpRequest(string otp)
    {
        SetLoading(true);

        string url = config.baseApiUrl.Trim().TrimEnd('/') + "/subscriptions/verify";
        
        VerifyRequest reqData = new VerifyRequest { referenceNo = referenceNo, otp = otp };
        string jsonBody = JsonUtility.ToJson(reqData);

        using (UnityWebRequest www = CreateAuthenticatedPostRequest(url, jsonBody))
        {
            yield return www.SendWebRequest();
            yield return FinishLoading();
            SetLoading(false);

            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowToast("Verification Failed.");
                Debug.LogError(www.downloadHandler.text);
            }
            else
            {
                try
                {
                    Debug.Log($"[Verify] Response: {www.downloadHandler.text}");
                    VerifyResponse response = JsonUtility.FromJson<VerifyResponse>(www.downloadHandler.text);

                    if (response == null) throw new Exception("Empty server response");
                    if (response.statusCode == "S1000")
                    {
                        LogGameEvent("auth_pin_result", "status", "success");
                        subscriptionId = response.subscriptionId;
                        ShowToast("Verified! Logging in...");
                        CompleteRegistration(tempUsername, currentMobile10Digit, subscriptionId, referenceNo);
                    }
                    else
                    {
                        ShowWarning($"Invalid OTP: {response.statusDetail}");
                    }
                }
                catch (Exception e)
                { 
                    Debug.LogError($"[Verify] JSON/Parsing Error: {e.Message}");
                    
                    ShowWarning("Error reading verification.");
                }
            }
        }
    }


    void CompleteRegistration(string username, string mobile, string subId, string refNo)
    {
        Debug.Log($"OTP Verified. Now fetching Game Token for {username}...");
        
        PlayerPrefs.SetString("Mobile", mobile);
        PlayerPrefs.SetString("SubscriberId", subId ?? "");
        PlayerPrefs.SetString("ReferenceNo", refNo ?? "");
        
        StartCoroutine(RegisterUserWithGameApi(username, mobile));
    }

    private IEnumerator RegisterUserWithGameApi(string username, string mobile)
    {
        SetLoading(true);

        UserRegisterRequest req = new UserRegisterRequest
        {
            username = username,
            phoneNumber = mobile,
            countryCode = "+94",
            country = "LKR",
            platformName = "mobile",
            email = "filler",
            appName = config.gameAppName
        };

        string jsonBody = JsonUtility.ToJson(req);

        using (UnityWebRequest www = new UnityWebRequest(config.gameUserApiUrl.Trim(), "POST"))
        {
            www.timeout = Mathf.Max(1, requestTimeoutSeconds);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();
            yield return FinishLoading();
            SetLoading(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Game API Login Response: " + www.downloadHandler.text);
                try
                {
                    UserRegisterResponse res = JsonUtility.FromJson<UserRegisterResponse>(www.downloadHandler.text);
                    if (res != null && res.user != null && !string.IsNullOrEmpty(res.user.authToken))
                    {
                        Debug.Log("Token Received: " + res.user.authToken);
                        
                        PlayerPrefs.SetString("AccessToken", res.user.authToken);
                        PlayerPrefs.SetInt("UserId", res.user.id);
                        PlayerPrefs.SetString("Username", res.user.username);
                        PlayerPrefs.SetInt("IsRegisteredUser", 1);
                        PlayerPrefs.DeleteKey("Auth_LoggedOut");
                        PlayerPrefs.Save();

                        ShowWelcome();
                    }
                    else
                    {
                        ShowToast("Login Failed: Invalid Response");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    ShowToast("Login Error");
                }
            }
            else
            {
                Debug.LogError("Game API Error: " + www.error + " | " + www.downloadHandler.text);
                ShowToast("Failed to login to Game Server. Try Again.");
            }
        }
    }

    private void SwitchToOtpPanel()
    {
        if (otpPanel.activeSelf) return;

        detailsPanel.SetActive(false);
        otpPanel.SetActive(true);
        if (otpMessageText != null) otpMessageText.text = $"Enter OTP sent to\n{currentMobile10Digit}";
        
        if(singleOtpInput != null)
        {
            if (otpDigits != null) { otpDigits.Clear(); otpDigits.Focus(); }
            else { singleOtpInput.text = ""; singleOtpInput.ActivateInputField(); }
        }
        
        otpPanel.transform.localScale = Vector3.zero;
        otpPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private UnityWebRequest CreateAuthenticatedPostRequest(string url, string jsonBody)
    {
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        www.timeout = Mathf.Max(1, requestTimeoutSeconds);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + config.apiAuthToken.Trim()); 
        
        return www;
    }

    private void SetLoading(bool isLoading)
    {
        busy = isLoading;
        if (loadingOverlay != null)
        { loadingOverlay.SetActive(isLoading); if (isLoading) loadingOverlay.transform.SetAsLastSibling(); }
        if (isLoading && loadingScreen != null)
        { loadingScreen.BeginLoading(); loadingScreen.SetProgress(0.85f); }
        RefreshButtons();
        if (usernameInput != null) usernameInput.interactable = !isLoading;
        if (phoneInput != null) phoneInput.interactable = !isLoading;
        if (singleOtpInput != null) singleOtpInput.interactable = !isLoading;
    }

    private IEnumerator FinishLoading()
    {
        if (loadingScreen != null && loadingScreen.isActiveAndEnabled)
            yield return loadingScreen.CompleteLoading();
    }

    private void RefreshButtons()
    {
        if (sendOtpButton != null) sendOtpButton.interactable = !busy;
        if (verifyOtpButton != null) verifyOtpButton.interactable = !busy && IsValidOtp();
        if (resendOtpButton != null) resendOtpButton.interactable = !busy && Time.unscaledTime >= resendAvailableAt;
    }

    public void ShowWelcome()
    {
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (otpPanel != null) otpPanel.SetActive(false);
        if (welcomePanel == null)
        { Debug.LogError("Assign Welcome Panel on RegistrationController.", this); return; }
        welcomePanel.SetActive(true);
        welcomePanel.transform.localScale = Vector3.zero;
        welcomePanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void OnStartClicked()
    {
        if (busy || navigating || PlayerPrefs.GetInt("IsRegisteredUser", 0) != 1) return;
        if (!Application.CanStreamedLevelBeLoaded(homeSceneName))
        { Debug.LogError("Add the HomeScene to Build Settings: " + homeSceneName, this); return; }
        navigating = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    // Compatibility with the existing serialized On Click entry in your screenshot.
    public void OnRegisterFromLoginClicked()
    {
        OnRegisterClicked();
    }

    public void OnRegisterClicked()
    {
        Debug.Log("[UI] Welcome Back Register clicked.", this);
        if (busy || navigating)
        {
            Debug.LogWarning("[UI] Register ignored: a request or scene transition is in progress.", this);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(tutorialSceneName))
        { Debug.LogError("Add the Tutorial scene to Build Settings: " + tutorialSceneName, this); return; }
        navigating = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(tutorialSceneName);
    }

    private void OnDestroy()
    {
        if (sendOtpButton != null) sendOtpButton.onClick.RemoveListener(OnSendOtpClicked);
        if (verifyOtpButton != null) verifyOtpButton.onClick.RemoveListener(OnVerifyClicked);
        if (resendOtpButton != null) resendOtpButton.onClick.RemoveListener(OnResendOtpClicked);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (singleOtpInput != null) singleOtpInput.onValueChanged.RemoveListener(OnOtpValueChanged);
        if (Instance == this) Instance = null;
    }

    private void ShowWarning(string message)
    {
        Debug.Log($"[WARNING] {message}");
        if (otpPanel != null && otpPanel.activeSelf && otpMessageText != null)
        {
            otpMessageText.text = message;
            otpMessageText.transform.DOShakePosition(0.4f, 10).SetUpdate(true);
        }
        else if (detailsWarningText != null)
        {
            detailsWarningText.text = message;
            detailsWarningText.transform.DOShakePosition(0.4f, 10).SetUpdate(true);
        }
        else
        {
            ShowToast(message);
        }
    }

    private void ShowToast(string message)
    {
        Debug.Log($"[TOAST] {message}");
        if (otpPanel != null && otpPanel.activeSelf && otpMessageText != null) otpMessageText.text = message;
        else if (detailsWarningText != null) detailsWarningText.text = message;
        // Shake active panel
        if (detailsPanel != null && detailsPanel.activeSelf) detailsPanel.transform.DOShakePosition(0.3f, 5, 90).SetUpdate(true);
        if (otpPanel != null && otpPanel.activeSelf) otpPanel.transform.DOShakePosition(0.3f, 5, 90).SetUpdate(true);
    }
}