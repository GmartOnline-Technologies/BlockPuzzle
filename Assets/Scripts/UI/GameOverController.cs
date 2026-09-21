using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// GameScene only. Keep on an active object outside the popup.
public class GameOverController : MonoBehaviour
{
    public GameManager game;
    public GameObject modalRoot;
    public RectTransform gameOverBoard;
    public TMP_Text playedTimeText, collectedCoinsText;
    public Button homeButton, retryButton;
    public string homeSceneName = "HomeScene";
    private float playedSeconds;
    private bool shown, leaving, backgrounded;
    private Tween popup;
    private Vector3 restScale;

    private void Awake()
    {
        if (gameOverBoard != null) restScale = gameOverBoard.localScale;
        if (modalRoot != null) modalRoot.SetActive(false);
        if (homeButton != null) homeButton.onClick.AddListener(GoHome);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
    }
    private void Update()
    {
        if (game != null && !shown && !leaving && !backgrounded && !game.paused && !game.gameOver)
            playedSeconds += Time.deltaTime;
    }
    // Called after CheckSpace evaluates ALL remaining tray blocks.
    public void OnSpaceChecked(int activeBlocks, int blockedCount)
    {
        if (!isActiveAndEnabled || shown || leaving || game == null || game.paused) return;
        if (activeBlocks <= 0 || blockedCount != activeBlocks) return;
        if (DestroyManager.ins != null && DestroyManager.ins.IsClearing) return;
        ShowGameOver();
    }
    public void ShowGameOver()
    {
        if (shown || leaving) return;
        if (game == null || modalRoot == null || gameOverBoard == null)
        { Debug.LogError("Assign Game, Modal Root and Game Over Board.", this); return; }
        shown = true;
        game.SetGameOver();
        BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.GameOver);
        int seconds = Mathf.Max(0, Mathf.FloorToInt(playedSeconds));
        if (playedTimeText != null)
            playedTimeText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        // Rewards are already saved. Do not award the round total again.
        if (collectedCoinsText != null) collectedCoinsText.text = game.score.ToString();
        modalRoot.SetActive(true);
        gameOverBoard.gameObject.SetActive(true);
        gameOverBoard.SetAsLastSibling();
        gameOverBoard.localScale = restScale * 0.85f;
        popup = gameOverBoard.DOScale(restScale, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
    }
    public void GoHome() { if (shown) Load(homeSceneName); }
    public void Retry() { if (shown && game != null) Load(game.gameObject.scene.name); }
    private void Load(string sceneName)
    {
        if (leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError("Add scene to build list: " + sceneName, this); return; }
        leaving = true;
        try { SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
        catch (Exception e) { leaving = false; Debug.LogError("Scene load failed: " + e.Message, this); }
    }
    private void OnApplicationPause(bool paused) { backgrounded = paused; }
    private void OnDisable()
    {
        if (popup != null) popup.Kill();
        if (gameOverBoard != null) gameOverBoard.localScale = restScale;
    }
    private void OnDestroy()
    {
        if (homeButton != null) homeButton.onClick.RemoveListener(GoHome);
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
    }
}