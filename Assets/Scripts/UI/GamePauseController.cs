using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keep on an always-active root, outside the modal Canvas and its boards.
public class GamePauseController : MonoBehaviour
{
    public GameManager game;
    public GameAudioSettings audioSettings;
    [Header("Modal UI")]
    public GameObject modalRoot;
    public GameObject menuBoard, settingsBoard, restartBoard, quitBoard;
    [Header("Menu")]
    public Button pauseButton, continueButton, menuCloseButton;
    public Button settingsButton, restartButton, quitButton;
    [Header("Settings")]
    public Button settingsCloseButton, soundButton, musicButton;
    public Image soundIcon, musicIcon;
    public Sprite soundOn, soundOff, musicOn, musicOff;
    [Header("Confirmation")]
    public Button restartYesButton, restartNoButton, restartCloseButton;
    public Button quitYesButton, quitNoButton, quitCloseButton;
    public string homeSceneName = "HomeScene";

    private enum Page { Playing, Menu, Settings, Restart, Quit }
    private Page page;
    private bool leaving;
    private bool ownsPause;
    private float previousTimeScale;
    private Tween motion;
    private Vector3[] scales;

    private GameObject[] Boards => new[] { menuBoard, settingsBoard, restartBoard, quitBoard };

    private void Awake()
    {
        scales = new Vector3[4];
        GameObject[] boards = Boards;
        for (int i = 0; i < boards.Length; i++)
        {
            scales[i] = boards[i] != null ? boards[i].transform.localScale : Vector3.one;
            if (boards[i] != null) boards[i].SetActive(false);
        }
        if (modalRoot != null) modalRoot.SetActive(false);
        Wire(true);
    }
    private void Start() { RefreshAudioIcons(); }
    private void Wire(bool add)
    {
        Bind(pauseButton, OpenPause, add);
        Bind(continueButton, Resume, add); Bind(menuCloseButton, Resume, add);
        Bind(settingsButton, OpenSettings, add);
        Bind(restartButton, AskRestart, add); Bind(quitButton, AskQuit, add);
        Bind(settingsCloseButton, BackToMenu, add);
        Bind(soundButton, ToggleSound, add); Bind(musicButton, ToggleMusic, add);
        Bind(restartYesButton, ConfirmRestart, add);
        Bind(restartNoButton, BackToMenu, add); Bind(restartCloseButton, BackToMenu, add);
        Bind(quitYesButton, ConfirmQuit, add);
        Bind(quitNoButton, BackToMenu, add); Bind(quitCloseButton, BackToMenu, add);
    }
    private static void Bind(Button button, UnityEngine.Events.UnityAction action, bool add)
    {
        if (button == null) return;
        if (add) button.onClick.AddListener(action);
        else button.onClick.RemoveListener(action);
    }
    public void OpenPause()
    {
        if (page != Page.Playing || leaving || game == null || game.gameOver || game.paused) return;
        if (modalRoot == null || menuBoard == null)
        { Debug.LogError("Assign the pause modal root and menu board.", this); return; }
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        game.PauseGame();
        modalRoot.SetActive(true);
        Show(Page.Menu, menuBoard);
    }
    public void Resume()
    {
        if (!ownsPause || leaving) return;
        HideBoards();
        if (modalRoot != null) modalRoot.SetActive(false);
        page = Page.Playing;
        ReleasePause();
    }
    private void ReleasePause()
    {
        if (!ownsPause) return;
        if (game != null) game.paused = false;
        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }
    public void OpenSettings()
    {
        if (page != Page.Menu || leaving) return;
        RefreshAudioIcons(); Show(Page.Settings, settingsBoard);
    }
    public void AskRestart() { if (page == Page.Menu && !leaving) Show(Page.Restart, restartBoard); }
    public void AskQuit() { if (page == Page.Menu && !leaving) Show(Page.Quit, quitBoard); }
    public void BackToMenu() { if (ownsPause && !leaving) Show(Page.Menu, menuBoard); }
    private void Show(Page next, GameObject board)
    {
        if (board == null) { Debug.LogError("Assign the " + next + " board.", this); return; }
        HideBoards();
        page = next;
        board.SetActive(true);
        board.transform.SetAsLastSibling();
        Vector3 target = board.transform.localScale;
        board.transform.localScale = target * 0.88f;
        motion = board.transform.DOScale(target, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }
    private void HideBoards()
    {
        if (motion != null) { motion.Kill(); motion = null; }
        GameObject[] boards = Boards;
        for (int i = 0; i < boards.Length; i++)
            if (boards[i] != null)
            {
                boards[i].SetActive(false);
                if (scales != null) boards[i].transform.localScale = scales[i];
            }
    }
    public void ToggleSound()
    {
        if (page != Page.Settings || leaving || audioSettings == null) return;
        audioSettings.SetSoundEnabled(!audioSettings.SoundEnabled); RefreshAudioIcons();
    }
    public void ToggleMusic()
    {
        if (page != Page.Settings || leaving || audioSettings == null) return;
        audioSettings.SetMusicEnabled(!audioSettings.MusicEnabled); RefreshAudioIcons();
    }
    private void RefreshAudioIcons()
    {
        if (audioSettings == null) return;
        if (soundIcon != null) soundIcon.sprite = audioSettings.SoundEnabled ? soundOn : soundOff;
        if (musicIcon != null) musicIcon.sprite = audioSettings.MusicEnabled ? musicOn : musicOff;
    }
    public void ConfirmRestart()
    {
        if (page == Page.Restart && !leaving) LoadScene(game.gameObject.scene.name);
    }
    public void ConfirmQuit()
    {
        if (page == Page.Quit && !leaving) LoadScene(homeSceneName);
    }
    private void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError("Add scene to the build list: " + sceneName, this); return; }
        leaving = true;
        // Keep gameplay frozen until the current scene is replaced. No restart-in-place
        // while a line-clear coroutine may still own references to the old board.
        try { SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
        catch (Exception exception)
        { leaving = false; Debug.LogError("Could not load scene: " + exception.Message, this); }
    }
    private void OnDisable()
    {
        HideBoards();
        if (modalRoot != null) modalRoot.SetActive(false);
        page = Page.Playing;
        ReleasePause(); // Includes scene unload: the destination must not stay paused.
    }
    private void OnDestroy() { Wire(false); }
}
