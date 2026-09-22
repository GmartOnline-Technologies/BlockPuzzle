using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class RegistrationController : MonoBehaviour
{
    public static RegistrationController Instance;

    [Header("API Config")]
    public AppConfig config;

    [Header("Panels")]
    [Tooltip("In Bootstrap: Assign Login_panel. In Tutorial: Assign user_de.")]
    public GameObject detailsPanel;       
    public GameObject otpPanel;
    public GameObject welcomePanel;
    public GameObject loadingOverlay;

    [Header("Inputs & Buttons")]
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
    [Serializable] private class OtpResponse { public string statusCode; public string referenceNo; public string statusDetail; }
    [Serializable] private class VerifyRequest { public string referenceNo; public string otp; }
    [Serializable] private class VerifyResponse { public string statusCode; public string subscriptionId; public string statusDetail; }
    [Serializable] public class UserRegisterRequest { public string username; public string phoneNumber; public string countryCode; public string country; public string platformName; public string email; public string appName; }
    [Serializable] public class UserRegisterResponse { public UserData user; }
    [Serializable] public class UserData { public int id; public string username; public string authToken; }
    // -----------------------------------------------------------

    void Awake() 
    { 
        Instance = this; 
        if(detailsPanel != null) detailsPanel.SetActive(false);
        if(otpPanel != null) otpPanel.SetActive(false);
        if(welcomePanel != null) welcomePanel.SetActive(false);
        if(loadingOverlay != null) loadingOverlay.SetActive(false);
        
        if(detailsWarningText != null) detailsWarningText.text = "";
    }

    void Start()
    {
        if (sendOtpButton != null) sendOtpButton.onClick.AddListener(OnSendOtpClicked);
        if (verifyOtpButton != null) verifyOtpButton.onClick.AddListener(OnVerifyClicked);
        if (resendOtpButton != null) resendOtpButton.onClick.AddListener(OnResendOtpClicked);

        if (singleOtpInput != null)
        {
            singleOtpInput.onValueChanged.AddListener(OnOtpValueChanged);
            singleOtpInput.characterLimit = 6;
            singleOtpInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        }

        SetupSubscriptionInfo();
    }

    void OnOtpValueChanged(string val)
    {
        if (verifyOtpButton != null)
        {
            if (val.Length >= 4) // Supporting shorter OTPs if server allows
            {
                verifyOtpButton.interactable = true;
                if (val.Length == 6) OnVerifyClicked();
            }
            else
            {
                verifyOtpButton.interactable = false;
            }
        }
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
            if (loadingOverlay != null && loadingOverlay.activeSelf) return;

            if (otpPanel != null && otpPanel.activeSelf)
            {
                OnBackToDetails();
                return;
            }

            if (detailsPanel != null && detailsPanel.activeSelf)
            {
                if (Time.time - lastBackPressTime < exitDoubleTapDelay)
                {
                    Debug.Log("Exiting Application...");
                    Application.Quit();
                }
                else
                {
                    lastBackPressTime = Time.time;
                    ShowToast("Tap again to exit");
                }
            }
        }
    }

    void OnBackToDetails()
    {
        Debug.Log("Back button pressed: Returning to Details.");
        if (otpPanel != null) otpPanel.SetActive(false);
        OpenRegistration();
    }

    public void OnBackClickedToDetails()
    {
        Debug.Log("Back button pressed: Returning to Details.");
        if (otpPanel != null) otpPanel.SetActive(false);
        OpenRegistration();
    }

    // Called by BootstrapManager for Login, and TutorialOnboardingFlow for New Users
    public void OpenRegistration()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
            detailsPanel.transform.localScale = Vector3.zero;
            // Add .SetUpdate(true) to bypass Time.timeScale = 0
            detailsPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true); 
        }
        if(detailsWarningText != null) detailsWarningText.text = "";
    }

    void OnSendOtpClicked()
    {
        if(detailsWarningText != null) detailsWarningText.text = "";

        tempUsername = usernameInput.text.Trim();
        string rawPhone = phoneInput.text.Trim();

        if (string.IsNullOrEmpty(tempUsername) || string.IsNullOrEmpty(rawPhone))
        {
            ShowWarning("Please fill in all fields");
            return;
        }

        string processedPhone = rawPhone;

        if (rawPhone.StartsWith("7") && rawPhone.Length == 9)
        {
            processedPhone = "0" + rawPhone;
        }
        else if (rawPhone.StartsWith("07") && rawPhone.Length == 10)
        {
            processedPhone = rawPhone;
        }
        else
        {
            ShowWarning("Enter a valid number (e.g. 07... or 7...)");
            return;
        }

        if (processedPhone == "0770000000" || processedPhone == "0770000001")
        {
            Debug.LogWarning("[AUTH BYPASS] Test number detected!");
            string fakeSubId = "test_" + processedPhone;
            if (processedPhone == "0770000001")
            {
                PlayerPrefs.SetInt("PlayerCoins", 10000);
                PlayerPrefs.Save();
            }
            CompleteRegistration(tempUsername, processedPhone, fakeSubId, fakeSubId);
            return;
        }
        currentMobile10Digit = processedPhone;
        StartCoroutine(SendOtpRequest());
    }

    void OnResendOtpClicked()
    {
        Debug.Log("Resending OTP...");
        if (singleOtpInput != null) singleOtpInput.text = "";
        StartCoroutine(SendOtpRequest());
        StartCoroutine(ResendCooldownRoutine());
    }

    private IEnumerator ResendCooldownRoutine()
    {
        int cooldownSeconds = 30;
        if (resendOtpButton != null) resendOtpButton.interactable = false;

        while (cooldownSeconds > 0)
        {
            if (otpMessageText != null)
            {
                otpMessageText.text = $"OTP sent to\n{currentMobile10Digit}\n" +
                                    $"<color=red>Resend available in {cooldownSeconds}s</color>";
            }

            // Force the timer to tick even when Time.timeScale is 0
            yield return new WaitForSecondsRealtime(1f); 
            cooldownSeconds--;
        }

        if (resendOtpButton != null) resendOtpButton.interactable = true;
        if (otpMessageText != null)
        {
            otpMessageText.text = $"OTP sent to\n{currentMobile10Digit}";
        }
    }

    private IEnumerator SendOtpRequest()
    {
        SetLoading(true);

        string url = config.baseApiUrl.Trim() + "subscriptions/request";
        OtpRequest reqData = new OtpRequest { tel = currentMobile10Digit };
        string jsonBody = JsonUtility.ToJson(reqData);

        using (UnityWebRequest www = CreateAuthenticatedPostRequest(url, jsonBody))
        {
            yield return www.SendWebRequest();
            SetLoading(false);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Auth] Error: {www.error} : {www.downloadHandler.text}");

                if (detailsPanel != null && detailsPanel.activeSelf) ShowWarning("Connection Failed.");
                else ShowToast("Connection Failed.");
            }
            else
            {
                try 
                {
                    Debug.Log($"[Auth] Response: {www.downloadHandler.text}");
                    OtpResponse response = JsonUtility.FromJson<OtpResponse>(www.downloadHandler.text);

                    if (response.statusCode == "S1000") 
                    {
                        referenceNo = response.referenceNo;
                        ShowToast("OTP Sent Successfully!");
                        SwitchToOtpPanel();
                    }
                    else if (response.statusCode == "S2000")
                    {
                        Debug.Log("[Auth] User already subscribed. Logging in directly.");
                        ShowToast("Welcome back!");
                        CompleteRegistration(tempUsername, currentMobile10Digit, response.referenceNo, response.referenceNo);
                    }
                    else
                    {
                        if (detailsPanel != null && detailsPanel.activeSelf) ShowWarning(response.statusDetail);
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
        if (singleOtpInput == null) return;
        string enteredOtp = singleOtpInput.text;

        if (enteredOtp.Length < 4)
        {
            ShowWarning("Invalid OTP Length");
            return;
        }

        StartCoroutine(VerifyOtpRequest(enteredOtp));
    }

    private IEnumerator VerifyOtpRequest(string otp)
    {
        SetLoading(true);

        string url = config.baseApiUrl.Trim() + "subscriptions/verify";
        VerifyRequest reqData = new VerifyRequest { referenceNo = referenceNo, otp = otp };
        string jsonBody = JsonUtility.ToJson(reqData);

        using (UnityWebRequest www = CreateAuthenticatedPostRequest(url, jsonBody))
        {
            yield return www.SendWebRequest();
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

                    if (response.statusCode == "S1000")
                    {
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
        PlayerPrefs.SetString("SubscriberId", subId);
        PlayerPrefs.SetString("ReferenceNo", refNo);
        
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
            appName = "busArena" // Confirm this matches your game's DB name
        };

        string jsonBody = JsonUtility.ToJson(req);

        using (UnityWebRequest www = new UnityWebRequest(config.gameUserApiUrl.Trim(), "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();
            SetLoading(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Game API Login Response: " + www.downloadHandler.text);
                try
                {
                    UserRegisterResponse res = JsonUtility.FromJson<UserRegisterResponse>(www.downloadHandler.text);
                    if (res != null && res.user != null)
                    {
                        Debug.Log("Token Received: " + res.user.authToken);
                        
                        PlayerPrefs.SetString("AccessToken", res.user.authToken);
                        PlayerPrefs.SetInt("UserId", res.user.id);
                        PlayerPrefs.SetString("Username", res.user.username);
                        PlayerPrefs.SetInt("IsRegisteredUser", 1);
                        PlayerPrefs.SetInt("IsLoggedOut", 0);
                        PlayerPrefs.Save();

                        SwitchToWelcomePanel();
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
        if (otpPanel != null && otpPanel.activeSelf) return;

        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (otpPanel != null)
        {
            otpPanel.SetActive(true);
            otpPanel.transform.localScale = Vector3.zero;
            // Add .SetUpdate(true) here as well
            otpPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (otpMessageText != null) otpMessageText.text = $"Enter OTP sent to\n{currentMobile10Digit}";
        
        if(singleOtpInput != null)
        {
            singleOtpInput.text = "";
            singleOtpInput.ActivateInputField();
        }
    }

   private void SwitchToWelcomePanel()
    {
        if (otpPanel != null) otpPanel.SetActive(false);
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);
            welcomePanel.transform.localScale = Vector3.zero;
            // Add .SetUpdate(true) here too
            welcomePanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void OnRegisterFromLoginClicked()
    {
        // Route brand new users from the Bootstrap scene directly to the gameplay tutorial
        SceneManager.LoadScene("Tutorial"); 
    }

    // Called by the "Start" button on the Welcome Panel
    public void OnStartClicked()
    {
        SceneManager.LoadScene("HomeScene"); 
    }

    private UnityWebRequest CreateAuthenticatedPostRequest(string url, string jsonBody)
    {
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Authorization", "Bearer " + config.apiAuthToken.Trim()); 
        
        return www;
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(isLoading);
        if (sendOtpButton != null) sendOtpButton.interactable = !isLoading;
        if (verifyOtpButton != null) verifyOtpButton.interactable = !isLoading;
        if (resendOtpButton != null) resendOtpButton.interactable = !isLoading;
        if (usernameInput != null) usernameInput.interactable = !isLoading;
        if (phoneInput != null) phoneInput.interactable = !isLoading;
    }

   private void ShowWarning(string message)
    {
        Debug.Log($"[WARNING] {message}");
        if (otpPanel != null && otpPanel.activeSelf && otpMessageText != null)
        {
            otpMessageText.text = message;
            // Add SetUpdate(true) to prevent the shake animation from freezing
            otpMessageText.transform.DOShakePosition(0.4f, 10).SetUpdate(true); 
        }
        else if (detailsWarningText != null && detailsPanel != null && detailsPanel.activeSelf)
        {
            detailsWarningText.text = message;
            // Add SetUpdate(true) to prevent the shake animation from freezing
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
        // Add SetUpdate(true) to prevent the shake animation from freezing
        if (detailsPanel != null && detailsPanel.activeSelf) detailsPanel.transform.DOShakePosition(0.3f, 5, 90).SetUpdate(true);
        if (otpPanel != null && otpPanel.activeSelf) otpPanel.transform.DOShakePosition(0.3f, 5, 90).SetUpdate(true);
    }
}