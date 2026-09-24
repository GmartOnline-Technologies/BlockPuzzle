using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

// Replaces the supplied instructor script. Keep on an always-active HomeControllers root.
public class SettingsManager : MonoBehaviour
{
    [Header("References")]
    public HomeSceneController home;
    public GameAudioSettings audioSettings;
    [Header("Existing Home boards")]
    [Tooltip("Assign Home_Panel/Body/Setings_Board. No new settings panel is required.")]
    public GameObject settingsPanel;
    [Range(0f, 0.85f)] public float backgroundDarkness = 0.55f;
    [Header("Settings Controls")]
    public Button backButton;
    public Button soundButton, musicButton;
    public Image soundIcon, musicIcon;
    public Sprite soundOnSprite, soundOffSprite, musicOnSprite, musicOffSprite;
    public TMP_Dropdown languageDropdown;
    [Header("Terms and Conditions")]
    public GameObject termsAndConditionsPanel;
    public Button openTermsButton, closeTermsButton;
    [Header("Account Actions")]
    public Button mainLogoutButton, mainDeactivateButton;
    [Header("Logout Popup")]
    public GameObject logoutPopupPanel;
    public Button confirmLogoutButton, cancelLogoutButton, closeLogoutButton;
    [Header("Deactivate Popup")]
    public GameObject deactivatePopupPanel;
    public Button confirmDeactivateButton, cancelDeactivateButton, closeDeactivateButton;
    [Header("Transitions")]
    public string bootstrapScene = "Bootstrap";
    public string tutorialScene = "Tutorial";
    private int currentLanguageIndex;
    private enum View { Closed, Settings, Logout, Deactivate, Terms }
    private View view;
    private readonly List<Button> boundButtons = new List<Button>();
    private readonly List<UnityAction> boundActions = new List<UnityAction>();
    private bool Busy { get { return home != null && home.IsTransitioning; } }

    private void Awake()
    {
        if (home == null) home = GetComponent<HomeSceneController>();
        if (audioSettings == null) audioSettings = GetComponent<GameAudioSettings>();
        HideCards(); HideDimmer();
        Bind(backButton, OnBackClicked);
        Bind(soundButton, ToggleSound); Bind(musicButton, ToggleMusic);
        Bind(mainLogoutButton, OpenLogout); Bind(mainDeactivateButton, OpenDeactivate);
        Bind(confirmLogoutButton, OnConfirmLogout); Bind(cancelLogoutButton, OpenSettings); Bind(closeLogoutButton, OpenSettings);
        Bind(confirmDeactivateButton, OnConfirmDeactivate); Bind(cancelDeactivateButton, OpenSettings); Bind(closeDeactivateButton, OpenSettings);
        Bind(openTermsButton, OpenTerms); Bind(closeTermsButton, OpenSettings);
        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
    
    private void Start() { RefreshFields(); }
    
    // --- UI POLISH: Intercept the Bind method to add animations to EVERY button ---
    private void Bind(Button button, UnityAction action)
    {
        if (button == null) return;
        
        // Wrap the original action to include the bounce animation
        UnityAction animatedAction = () => {
            ButtonClickEffect(button);
            action.Invoke();
        };
        
        button.onClick.AddListener(animatedAction); 
        boundButtons.Add(button); 
        boundActions.Add(animatedAction); // Store the animated version so we can remove it on Destroy
    }

    // --- UI POLISH: Universal Button Click Bounce ---
    private void ButtonClickEffect(Button btn)
    {
        if (btn == null) return;
        btn.transform.DOKill(true); // Stop any existing animations on this button
        btn.transform.localScale = Vector3.one;
        btn.transform.DOScale(0.85f, 0.1f).SetLoops(2, LoopType.Yoyo).SetUpdate(true);
    }

    private void RefreshFields()
    {
        currentLanguageIndex = Mathf.Clamp(PlayerPrefs.GetInt("SelectedLanguage", 0), 0, 1);
        if (languageDropdown != null) languageDropdown.SetValueWithoutNotify(currentLanguageIndex);
        RefreshAudioIcons(); UpdateCoinDisplay();
    }
    
    public void UpdateCoinDisplay()
    {
        if (home != null) home.RefreshBalances();
    }
    
    private void RefreshAudioIcons()
    {
        if (soundIcon != null) soundIcon.sprite = PlayerPrefs.GetInt("GameSound", 1) == 1 ? soundOnSprite : soundOffSprite;
        if (musicIcon != null) musicIcon.sprite = PlayerPrefs.GetInt("GameMusic", 1) == 1 ? musicOnSprite : musicOffSprite;
    }
    
    public void ToggleSound() { OnSoundToggled(PlayerPrefs.GetInt("GameSound", 1) == 0); }
    public void ToggleMusic() { OnMusicToggled(PlayerPrefs.GetInt("GameMusic", 1) == 0); }
    
    public void OnSoundToggled(bool enabled)
    {
        if (Busy) return;
        if (audioSettings != null) audioSettings.SetSoundEnabled(enabled);
        else { PlayerPrefs.SetInt("GameSound", enabled ? 1 : 0); PlayerPrefs.Save(); Debug.LogWarning("Assign GameAudioSettings to apply sound changes.", this); }
        RefreshAudioIcons();
    }
    
    public void OnMusicToggled(bool enabled)
    {
        if (Busy) return;
        if (audioSettings != null) audioSettings.SetMusicEnabled(enabled);
        else { PlayerPrefs.SetInt("GameMusic", enabled ? 1 : 0); PlayerPrefs.Save(); Debug.LogWarning("Assign GameAudioSettings to apply music changes.", this); }
        RefreshAudioIcons();
    }
    
    public void OpenSettings()
    {
        if (Busy) return;
        RefreshFields(); Show(settingsPanel, View.Settings);
    }
    
    public void OnBackClicked()
    {
        if (Busy) return;
        HideCards(); HideDimmer(); view = View.Closed;
        if (home != null) home.SetModalOpen(false);
    }
    
    private void Show(GameObject card, View next)
    {
        if (Busy || card == null) return;
        HideCards();
        if (home != null) home.SetModalOpen(true);
        view = next;
        
        // Activate first: configure the nested Canvas only after OnEnable has run.
        PopupMotion.Show(card);
        ShowDimmer(card);

        // --- UI POLISH: Snappy Popup Appearance ---
        card.transform.DOKill(true);
        card.transform.localScale = Vector3.one * 0.7f; // Start slightly shrunk
        card.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true); // Pop to full size
    }
    
    private void HideCards()
    {
        Active(settingsPanel, false); Active(logoutPopupPanel, false); Active(deactivatePopupPanel, false);
        Active(termsAndConditionsPanel, false);
    }
    
    public void OpenLogout() { Show(logoutPopupPanel, View.Logout); }
    public void OpenDeactivate() { Show(deactivatePopupPanel, View.Deactivate); }
    public void OpenTerms() { Show(termsAndConditionsPanel, View.Terms); }
    
    public void OnDropdownValueChanged(int index)
    {
        if (Busy || index < 0 || index > 1 || index == currentLanguageIndex) return;
        currentLanguageIndex = index;
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.SetLanguage(currentLanguageIndex);
        else
        {
            PlayerPrefs.SetInt("SelectedLanguage", currentLanguageIndex);
            PlayerPrefs.SetInt("HasSelectedLanguage", 1); PlayerPrefs.Save();
            Debug.LogWarning("Add LocalizationManager so live labels update when language changes.", this);
        }
        if (languageDropdown != null) languageDropdown.SetValueWithoutNotify(currentLanguageIndex);
    }
    
    public void OnConfirmLogout()
    {
        if (Busy || view != View.Logout || home == null) return;
        if (home.BeginTransition(bootstrapScene, () => ResetAccount(false))) { HideCards(); HideDimmer(); }
    }
    
    public void OnConfirmDeactivate()
    {
        if (Busy || view != View.Deactivate || home == null) return;
        if (home.BeginTransition(bootstrapScene, () => ResetAccount(true))) { HideCards(); HideDimmer(); }
    }
    
    // Runtime UI layers preserve the boards' original Body parent and positions.
    private Image dimmer;
    private Tween dimTween;
    
    private sealed class CardLayer
    {
        public Canvas canvas;
        public bool addedCanvas, previousOverride, previousEnabled;
        public GraphicRaycaster raycaster;
        public bool previousRaycasterEnabled;
        public int previousOrder, previousLayer;
        public GraphicRaycaster addedRaycaster;
    }
    
    private readonly Dictionary<GameObject, CardLayer> cardLayers = new Dictionary<GameObject, CardLayer>();
    
    private void ShowDimmer(GameObject card)
    {
        Canvas parentCanvas = card.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        { Debug.LogError("Home popup boards must be children of a Canvas.", card); return; }
        Canvas root = parentCanvas.rootCanvas;
        if (dimmer == null)
        {
            GameObject obj = new GameObject("HomePopupDimmer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            obj.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas layer = obj.GetComponent<Canvas>();
            layer.overrideSorting = true; layer.sortingLayerID = root.sortingLayerID;
            layer.sortingOrder = root.sortingOrder + 10;
            dimmer = obj.GetComponent<Image>();
            dimmer.color = Color.clear; dimmer.raycastTarget = true;
        }
        CardLayer state;
        if (!cardLayers.TryGetValue(card, out state))
        {
            Canvas layer = card.GetComponent<Canvas>();
            state = new CardLayer { addedCanvas = layer == null };
            if (layer == null) layer = card.AddComponent<Canvas>();
            state.canvas = layer; state.previousOverride = layer.overrideSorting;
            state.previousEnabled = layer.enabled;
            state.previousOrder = layer.sortingOrder; state.previousLayer = layer.sortingLayerID;
            state.raycaster = card.GetComponent<GraphicRaycaster>();
            if (state.raycaster == null)
            {
                state.addedRaycaster = card.AddComponent<GraphicRaycaster>();
                state.raycaster = state.addedRaycaster;
            }
            state.previousRaycasterEnabled = state.raycaster.enabled;
            cardLayers.Add(card, state);
        }
        state.canvas.enabled = true;
        state.canvas.sortingLayerID = root.sortingLayerID;
        state.canvas.overrideSorting = true;
        state.canvas.sortingOrder = root.sortingOrder + 11;
        state.raycaster.enabled = true;
        bool alreadyVisible = dimmer.gameObject.activeSelf && dimmer.color.a > 0f;
        dimmer.gameObject.SetActive(true);
        if (dimTween != null) dimTween.Kill();
        if (!alreadyVisible) dimmer.color = Color.clear;
        dimTween = dimmer.DOColor(new Color(0f, 0f, 0f, backgroundDarkness), 0.2f).SetUpdate(true);
    }
    
    private void HideDimmer()
    {
        if (dimTween != null) { dimTween.Kill(); dimTween = null; }
        if (dimmer != null) dimmer.gameObject.SetActive(false);
    }
    
    private static void ResetAccount(bool deactivate)
    {
        // Both confirmation buttons now sign out only. Per-user progress stays saved.
        PlayerPrefs.SetInt("Auth_LoggedOut", 1);
        foreach (string key in new[] { "AccessToken", "UserId", "Username", "Mobile", "SubscriberId", "ReferenceNo" })
            PlayerPrefs.DeleteKey(key);
        PlayerPrefs.SetInt("IsRegisteredUser", 0);
        PlayerPrefs.SetInt("IsLoggedOut", 1);
        PlayerPrefs.SetString("AuthMessage", "Successfully Logged Out. Progress saved.");
        PlayerPrefs.Save();
        Debug.Log(deactivate
            ? "[Auth] Deactivate button: signed out; saved progress preserved."
            : "[Auth] Logged out; saved progress preserved.");
    }

    private static void Active(GameObject target, bool active) { if (target != null) target.SetActive(active); }
    
    private void OnDisable()
    {
        HideCards(); HideDimmer(); view = View.Closed;
        if (home != null) home.SetModalOpen(false);
    }
    
    private void OnDestroy()
    {
        HideDimmer();
        if (dimmer != null) Destroy(dimmer.gameObject);
        foreach (CardLayer layer in cardLayers.Values)
        {
            if (layer.addedRaycaster != null) Destroy(layer.addedRaycaster);
            else if (layer.raycaster != null) layer.raycaster.enabled = layer.previousRaycasterEnabled;
            if (layer.canvas == null) continue;
            if (layer.addedCanvas) Destroy(layer.canvas);
            else
            {
                layer.canvas.enabled = layer.previousEnabled;
                layer.canvas.overrideSorting = layer.previousOverride;
                layer.canvas.sortingOrder = layer.previousOrder;
                layer.canvas.sortingLayerID = layer.previousLayer;
            }
        }
        for (int i = 0; i < boundButtons.Count; i++)
            if (boundButtons[i] != null) boundButtons[i].onClick.RemoveListener(boundActions[i]);
        if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }
}