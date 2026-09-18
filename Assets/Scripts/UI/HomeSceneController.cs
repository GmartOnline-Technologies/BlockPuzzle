using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keep on an always-active root outside Home, settings and loading panels.
public class HomeSceneController : MonoBehaviour
{
    [Header("Home buttons")]
    public Button playButton, settingsButton, leaderboardButton, keyStoreButton, addKeysButton;
    public SettingsManager settings;
    [Header("Balances (read-only display)")]
    public TMP_Text coinText, keyText;
    public string keyPreferenceKey = "PlayerKeys";
    [Header("First Play guide")]
    public RectTransform guideHand;
    public float handTravel = 9f;
    [Header("Loading")]
    public GameObject loadingPanel;
    public LoadingScreen loadingScreen;
    public TMP_Text statusText;
    [Min(0.1f)] public float minimumLoadingSeconds = 1.2f;
    public string gameSceneName = "GameScene";
    [Header("Panel navigation")]
    public GameObject homePanel;
    public LeaderboardController leaderboard;
    public GameObject shopPanel;
    public Button shopBackButton;
    private bool shopOpen;
    public bool IsTransitioning { get; private set; }
    private string guidePreferenceKey;
    private Tween guideTween;
    private Vector2 guidePosition;
    private Vector3 guideScale;
    private bool modalOpen;
    private AsyncOperation pendingScene;
    private Action beforeActivation;
    private bool transitionCommitted;

    private void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (shopBackButton != null) shopBackButton.onClick.AddListener(CloseShop);
        guidePreferenceKey = "HomePlayGuideSeen_" + PlayerPrefs.GetInt("UserId", 0);
        if (guideHand != null)
        {
            guidePosition = guideHand.anchoredPosition; guideScale = guideHand.localScale;
            foreach (Graphic graphic in guideHand.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            CanvasGroup group = guideHand.GetComponent<CanvasGroup>();
            if (group == null) group = guideHand.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false; group.blocksRaycasts = false;
        }
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (leaderboardButton != null) leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        if (keyStoreButton != null) keyStoreButton.onClick.AddListener(OnKeyStoreClicked);
        if (addKeysButton != null) addKeysButton.onClick.AddListener(OnKeyStoreClicked);
    }
    private void OnEnable() { CoinWallet.Changed += OnCoinsChanged; RefreshBalances(); UpdateGuide(); }
    private void OnCoinsChanged(int total) { RefreshBalances(); }
    public void RefreshBalances()
    {
        SetBalances(CoinWallet.Total, PlayerPrefs.GetInt(keyPreferenceKey, 0));
    }
    // Display only: earning is handled by CoinWallet, not this method.
    public void SetBalances(int coins, int keys)
    {
        if (coinText != null) coinText.text = Mathf.Max(0, coins).ToString();
        if (keyText != null) keyText.text = Mathf.Max(0, keys).ToString();
    }
    public void SetModalOpen(bool open) { modalOpen = open; UpdateGuide(); }
    private void UpdateGuide()
    {
        if (guideHand == null) return;
        if (guideTween != null) { guideTween.Kill(); guideTween = null; }
        guideHand.anchoredPosition = guidePosition; guideHand.localScale = guideScale;
        bool show = !modalOpen && !IsTransitioning && PlayerPrefs.GetInt(guidePreferenceKey, 0) == 0;
        guideHand.gameObject.SetActive(show);
        if (!show) return;
        guideTween = DOTween.Sequence()
            .Append(guideHand.DOAnchorPos(guidePosition + Vector2.up * handTravel, 0.5f).SetEase(Ease.InOutSine))
            .Join(guideHand.DOScale(guideScale * 0.94f, 0.5f).SetEase(Ease.InOutSine))
            .AppendInterval(0.15f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }
    public void OnPlayClicked()
    {
        if (modalOpen) return;
        BeginTransition(gameSceneName, () => {
            PlayerPrefs.SetInt(guidePreferenceKey, 1); PlayerPrefs.Save();
        });
    }
    public void OnSettingsClicked()
    { if (!IsTransitioning && !modalOpen && settings != null) settings.OpenSettings(); }
    public void OnLeaderboardClicked()
    {
        if (IsTransitioning || modalOpen) return;
        if (leaderboard == null || !leaderboard.isActiveAndEnabled)
        { ShowError("Assign an active LeaderboardController to HomeSceneController."); return; }
        leaderboard.Open();
    }
    public void OnKeyStoreClicked()
    {
        if (IsTransitioning || modalOpen) return;
        if (homePanel == null || shopPanel == null || homePanel == shopPanel ||
            shopPanel.transform.IsChildOf(homePanel.transform) ||
            transform.IsChildOf(homePanel.transform))
        { ShowError("Assign separate Home and Shop panels; keep HomeControllers outside Home Panel."); return; }
        shopOpen = true;
        SetModalOpen(true);
        homePanel.SetActive(false);
        shopPanel.SetActive(true);
        shopPanel.transform.SetAsLastSibling();
    }
    public void CloseShop()
    {
        if (!shopOpen || IsTransitioning) return;
        shopOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
        SetModalOpen(false);
        RefreshBalances();
    }

    // Settings commits local logout/reset only after the destination has loaded successfully.
    public bool BeginTransition(string sceneName, Action commit = null)
    {
        if (IsTransitioning) return false;
        if (loadingPanel == null || loadingScreen == null || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            ShowError("Assign the loading panel/bar and add the destination scene to the build list: " + sceneName);
            return false;
        }
        loadingPanel.SetActive(true);
        if (!loadingPanel.activeInHierarchy || !loadingScreen.isActiveAndEnabled)
        {
            loadingPanel.SetActive(false);
            ShowError("Loading panel parents and LoadingScreen must be active."); return false;
        }
        loadingPanel.transform.SetAsLastSibling();
        IsTransitioning = true; beforeActivation = commit; transitionCommitted = false;
        UpdateGuide(); Time.timeScale = 1f;
        StartCoroutine(LoadRoutine(sceneName));
        return true;
    }
    private IEnumerator LoadRoutine(string sceneName)
    {
        loadingScreen.BeginLoading();
        try { pendingScene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
        catch (Exception) { pendingScene = null; }
        if (pendingScene == null)
        {
            IsTransitioning = false; beforeActivation = null; loadingPanel.SetActive(false);
            UpdateGuide(); ShowError("Could not load " + sceneName + ". Check the build scene list."); yield break;
        }
        pendingScene.allowSceneActivation = false;
        float elapsed = 0f;
        while (pendingScene.progress < 0.9f || elapsed < Mathf.Max(0.1f, minimumLoadingSeconds))
        {
            elapsed += Time.unscaledDeltaTime;
            float timeProgress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, minimumLoadingSeconds));
            loadingScreen.SetProgress(Mathf.Min(timeProgress, pendingScene.progress / 0.9f) * 0.9f);
            yield return null;
        }
        yield return loadingScreen.CompleteLoading();
        CommitTransition();
        pendingScene.allowSceneActivation = true;
        yield return pendingScene;
    }
    private void CommitTransition()
    {
        if (transitionCommitted) return;
        transitionCommitted = true;
        Action callback = beforeActivation; beforeActivation = null;
        if (callback != null) callback();
    }
    private void ShowError(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.LogWarning(message, this);
    }
    private void OnDisable()
    {
        CoinWallet.Changed -= OnCoinsChanged;
        if (guideTween != null) guideTween.Kill();
        // Unity scene loads cannot be cancelled. Never strand an operation at 90%.
        if (pendingScene != null && !pendingScene.isDone)
        { CommitTransition(); pendingScene.allowSceneActivation = true; }
    }
    private void OnDestroy()
    {
        if (shopBackButton != null) shopBackButton.onClick.RemoveListener(CloseShop);
        if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (leaderboardButton != null) leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
        if (keyStoreButton != null) keyStoreButton.onClick.RemoveListener(OnKeyStoreClicked);
        if (addKeysButton != null) addKeysButton.onClick.RemoveListener(OnKeyStoreClicked);
    }
}
