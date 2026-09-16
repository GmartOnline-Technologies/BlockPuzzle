using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

// Scene-local: place on an always-active object, outside all managed panels.
public class RegistrationController : MonoBehaviour
{
    public static RegistrationController Instance;
    public AppConfig config;
    [Tooltip("Exact backend appName. The instructor used busArena; confirm the value for your game.")]
    public string gameAppName = "busArena";
    [Header("Panels")]
    public GameObject registrationPanel;
    public GameObject loginPanel, detailsPanel, otpPanel, welcomePanel, loadingOverlay;
    [Header("Login")]
    public TMP_InputField loginNameInput, loginPhoneInput;
    public Button loginButton;
    public TextMeshProUGUI loginWarningText;
    [Header("Details")]
    public TMP_InputField usernameInput, phoneInput;
    public Button sendOtpButton;
    public TextMeshProUGUI detailsWarningText, subInfoText;
    [Header("OTP: assign one input OR six boxes")]
    public TMP_InputField singleOtpInput;
    public OtpDigitInputs otpDigits;
    public Button verifyOtpButton, resendOtpButton, changeNumberButton;
    public TextMeshProUGUI otpMessageText;
    [Header("Events")]
    public UnityEvent onAuthenticated;
    public bool IsBusy { get; private set; }
    public bool IsAuthenticated { get; private set; }
    private bool loginMode, subscriptionVerified;
    private string mobile, playerName, referenceNo, subscriptionId;
    private float resendAt;
    private UnityWebRequest activeRequest;

    [Serializable] private class OtpRequest { public string tel; }
    [Serializable] private class OtpResponse { public string statusCode, referenceNo, statusDetail; }
    [Serializable] private class VerifyRequest { public string referenceNo, otp; }
    [Serializable] private class VerifyResponse { public string statusCode, subscriptionId, statusDetail; }
    [Serializable] private class UserRequest
    {
        public string username, phoneNumber, countryCode, country, platformName, email, appName;
    }
    [Serializable] private class UserResponse { public UserData user; }
    [Serializable] private class UserData { public int id; public string username, authToken; }

    private void Awake()
    {
        Instance = this;
        HidePanels();
        SetActive(loadingOverlay, false);
        if (singleOtpInput != null)
        {
            singleOtpInput.characterLimit = 6;
            singleOtpInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (sendOtpButton != null) sendOtpButton.onClick.AddListener(OnSendOtpClicked);
        if (verifyOtpButton != null) verifyOtpButton.onClick.AddListener(OnVerifyClicked);
        if (resendOtpButton != null) resendOtpButton.onClick.AddListener(OnResendOtpClicked);
        if (changeNumberButton != null) changeNumberButton.onClick.AddListener(OnBackClickedToDetails);
    }

    private void Update()
    {
        if (verifyOtpButton != null)
            verifyOtpButton.interactable = !IsBusy && (subscriptionVerified || Regex.IsMatch(ReadOtp(), "^[0-9]{6}$"));
        if (resendOtpButton != null)
            resendOtpButton.interactable = !IsBusy && !subscriptionVerified && Time.unscaledTime >= resendAt;
    }

    public void HidePanels()
    {
        SetActive(loginPanel, false); SetActive(detailsPanel, false);
        SetActive(otpPanel, false); SetActive(welcomePanel, false);
        // registrationPanel is an optional wrapper, not one of the child panels.
        SetActive(registrationPanel, false);
    }
    public void OpenLogin()
    {
        if (IsBusy) return;
        loginMode = true; ResetAttempt(); HidePanels();
        SetActive(registrationPanel, true); PopupMotion.Show(loginPanel);
        Message(loginWarningText, "");
    }
    public void OpenRegistration()
    {
        if (IsBusy) return;
        loginMode = false; ResetAttempt(); HidePanels();
        SetActive(registrationPanel, true); PopupMotion.Show(detailsPanel);
        Message(detailsWarningText, "");
        if (subInfoText != null)
        {
            string text = PlayerPrefs.GetString("Settings_SubText", "");
            // Keep your Inspector disclosure when settings do not provide one.
            if (!string.IsNullOrWhiteSpace(text)) subInfoText.text = text;
        }
    }
    private void ResetAttempt()
    {
        subscriptionVerified = false; referenceNo = null; subscriptionId = null;
        mobile = null; playerName = null; ClearOtp();
    }
    public void OnLoginClicked() { if (!IsBusy) SubmitDetails(true); }
    public void OnSendOtpClicked() { if (!IsBusy) SubmitDetails(false); }
    private void SubmitDetails(bool returning)
    {
        loginMode = returning;
        TMP_InputField nameField = returning ? loginNameInput : usernameInput;
        TMP_InputField phoneField = returning ? loginPhoneInput : phoneInput;
        if (nameField == null || phoneField == null) { Warn("Assign the name and phone inputs in the Inspector."); return; }
        string name = nameField.text.Trim();
        string normalized;
        if (name.Length < 2 || name.Length > 40) { Warn("Enter a name with 2 to 40 characters."); return; }
        if (!NormalizePhone(phoneField.text, out normalized)) { Warn("Enter a valid mobile number: 07XXXXXXXX or +947XXXXXXXX."); return; }
        if (!ValidConfig()) return;
        // Retrying user creation after a network failure must not request another OTP.
        bool sameVerified = subscriptionVerified && normalized == mobile && name == playerName;
        playerName = name; mobile = normalized;
        if (sameVerified) StartCoroutine(RegisterUser());
        else { subscriptionVerified = false; StartCoroutine(RequestOtp()); }
    }
    public static bool NormalizePhone(string value, out string normalized)
    {
        string phone = Regex.Replace(value ?? "", @"[\s()\-]", "");
        if (Regex.IsMatch(phone, @"^\+947[0-9]{8}$")) phone = "0" + phone.Substring(3);
        else if (Regex.IsMatch(phone, "^947[0-9]{8}$")) phone = "0" + phone.Substring(2);
        else if (Regex.IsMatch(phone, "^7[0-9]{8}$")) phone = "0" + phone;
        normalized = phone;
        return Regex.IsMatch(phone, "^07[0-9]{8}$");
    }
    private bool ValidConfig()
    {
        if (config == null || string.IsNullOrWhiteSpace(config.baseApiUrl) ||
            string.IsNullOrWhiteSpace(config.gameUserApiUrl) || string.IsNullOrWhiteSpace(config.apiAuthToken) ||
            string.IsNullOrWhiteSpace(gameAppName))
        { Warn("API configuration is incomplete. Assign AppConfig and its app identifier."); return false; }
        return true;
    }
    private IEnumerator RequestOtp()
    {
        SetBusy(true); referenceNo = null;
        string body = JsonUtility.ToJson(new OtpRequest { tel = mobile });
        using (UnityWebRequest request = Post(config.baseApiUrl.TrimEnd('/') + "/subscriptions/request", body, true))
        {
            activeRequest = request;
            yield return request.SendWebRequest();
            activeRequest = null;
            if (request.result != UnityWebRequest.Result.Success)
            { SetBusy(false); Warn("Could not request OTP. Check your connection and try again."); yield break; }
            OtpResponse response = Parse<OtpResponse>(request.downloadHandler.text);
            if (response != null && response.statusCode == "S1000" && !string.IsNullOrWhiteSpace(response.referenceNo))
            {
                referenceNo = response.referenceNo; resendAt = Time.unscaledTime + 30f;
                HidePanels(); SetActive(registrationPanel, true); PopupMotion.Show(otpPanel);
                ClearOtp(); Message(otpMessageText, "Enter the six-digit OTP sent to your number.");
                SetBusy(false);
            }
            else if (response != null && response.statusCode == "S2000" && !string.IsNullOrWhiteSpace(response.referenceNo))
            {
                // Instructor's existing server contract: already-subscribed response.
                referenceNo = response.referenceNo; subscriptionId = response.referenceNo;
                subscriptionVerified = true;
                yield return RegisterUser();
            }
            else { SetBusy(false); Warn("The server could not start verification. Please try again."); }
        }
    }
    public void OnVerifyClicked()
    {
        if (IsBusy) return;
        if (subscriptionVerified) { StartCoroutine(RegisterUser()); return; }
        string otp = ReadOtp();
        if (!Regex.IsMatch(otp, "^[0-9]{6}$") || string.IsNullOrWhiteSpace(referenceNo))
        { Warn("Enter all six OTP digits. Request a new code if needed."); return; }
        StartCoroutine(VerifyOtp(otp));
    }
    private IEnumerator VerifyOtp(string otp)
    {
        SetBusy(true);
        string body = JsonUtility.ToJson(new VerifyRequest { referenceNo = referenceNo, otp = otp });
        using (UnityWebRequest request = Post(config.baseApiUrl.TrimEnd('/') + "/subscriptions/verify", body, true))
        {
            activeRequest = request;
            yield return request.SendWebRequest();
            activeRequest = null;
            if (request.result != UnityWebRequest.Result.Success)
            { SetBusy(false); Warn("Verification failed. Check your connection and try again."); yield break; }
            VerifyResponse response = Parse<VerifyResponse>(request.downloadHandler.text);
            if (response == null || response.statusCode != "S1000" || string.IsNullOrWhiteSpace(response.subscriptionId))
            { SetBusy(false); Warn("Incorrect or expired OTP. Try again or resend the code."); yield break; }
            subscriptionId = response.subscriptionId; subscriptionVerified = true;
        }
        yield return RegisterUser();
    }
    private IEnumerator RegisterUser()
    {
        SetBusy(true);
        UserRequest data = new UserRequest {
            username = playerName, phoneNumber = mobile, countryCode = "+94", country = "LKR",
            platformName = "mobile", email = "filler", appName = gameAppName
        };
        using (UnityWebRequest request = Post(config.gameUserApiUrl.Trim(), JsonUtility.ToJson(data), false))
        {
            activeRequest = request;
            yield return request.SendWebRequest();
            activeRequest = null;
            if (request.result != UnityWebRequest.Result.Success)
            { SetBusy(false); Warn("Verification succeeded, but account loading failed. Press Continue again to retry."); yield break; }
            UserResponse response = Parse<UserResponse>(request.downloadHandler.text);
            if (response == null || response.user == null || response.user.id <= 0 || string.IsNullOrWhiteSpace(response.user.authToken))
            { SetBusy(false); Warn("The server did not return a valid account. Press Continue to retry."); yield break; }
            PlayerPrefs.SetString("Mobile", mobile);
            PlayerPrefs.SetString("Username", string.IsNullOrWhiteSpace(response.user.username) ? playerName : response.user.username);
            PlayerPrefs.SetString("SubscriberId", subscriptionId ?? "");
            PlayerPrefs.SetString("ReferenceNo", referenceNo ?? "");
            // Retains instructor's token storage contract; see setup notes for production storage.
            PlayerPrefs.SetString("AccessToken", response.user.authToken);
            PlayerPrefs.SetInt("UserId", response.user.id);
            PlayerPrefs.SetInt("IsRegisteredUser", 1);
            PlayerPrefs.Save();
            IsAuthenticated = true;
            SetBusy(false); HidePanels(); PopupMotion.Show(welcomePanel);
            if (onAuthenticated != null) onAuthenticated.Invoke();
        }
    }
    public void OnResendOtpClicked()
    {
        if (IsBusy || subscriptionVerified || Time.unscaledTime < resendAt || string.IsNullOrWhiteSpace(mobile)) return;
        resendAt = Time.unscaledTime + 30f;
        ClearOtp(); StartCoroutine(RequestOtp());
    }
    public void OnBackClickedToDetails()
    {
        if (IsBusy) return;
        if (loginMode) OpenLogin(); else OpenRegistration();
    }
    private string ReadOtp() { return otpDigits != null ? otpDigits.Code : (singleOtpInput != null ? singleOtpInput.text.Trim() : ""); }
    private void ClearOtp()
    {
        if (otpDigits != null) otpDigits.Clear();
        if (singleOtpInput != null) singleOtpInput.text = "";
    }
    private UnityWebRequest Post(string url, string json, bool authenticated)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 20;
        request.SetRequestHeader("Content-Type", "application/json");
        if (authenticated) request.SetRequestHeader("Authorization", "Bearer " + config.apiAuthToken.Trim());
        return request;
    }
    private static T Parse<T>(string json) where T : class
    { try { return JsonUtility.FromJson<T>(json); } catch (Exception) { return null; } }
    private void SetBusy(bool busy)
    {
        IsBusy = busy; SetActive(loadingOverlay, busy);
        if (loginButton != null) loginButton.interactable = !busy;
        if (sendOtpButton != null) sendOtpButton.interactable = !busy;
        if (changeNumberButton != null) changeNumberButton.interactable = !busy;
        foreach (TMP_InputField field in new[] { loginNameInput, loginPhoneInput, usernameInput, phoneInput, singleOtpInput })
            if (field != null) field.interactable = !busy;
        if (otpDigits != null) otpDigits.SetInteractable(!busy);
    }
    private void Warn(string text)
    {
        GameObject card = otpPanel != null && otpPanel.activeSelf ? otpPanel : (loginMode ? loginPanel : detailsPanel);
        if (card != null)
        {
            PopupMotion popup = card.GetComponent<PopupMotion>();
            if (popup != null) popup.ErrorFeedback();
        }
        if (otpPanel != null && otpPanel.activeSelf) Message(otpMessageText, text);
        else Message(loginMode ? loginWarningText : detailsWarningText, text);
    }
    private static void Message(TMP_Text target, string text) { if (target != null) target.text = text; }
    private static void SetActive(GameObject target, bool value) { if (target != null) target.SetActive(value); }
    private void OnDestroy()
    {
        if (activeRequest != null) activeRequest.Abort();
        if (Instance == this) Instance = null;
        if (loginButton != null) loginButton.onClick.RemoveListener(OnLoginClicked);
        if (sendOtpButton != null) sendOtpButton.onClick.RemoveListener(OnSendOtpClicked);
        if (verifyOtpButton != null) verifyOtpButton.onClick.RemoveListener(OnVerifyClicked);
        if (resendOtpButton != null) resendOtpButton.onClick.RemoveListener(OnResendOtpClicked);
        if (changeNumberButton != null) changeNumberButton.onClick.RemoveListener(OnBackClickedToDetails);
    }
}
