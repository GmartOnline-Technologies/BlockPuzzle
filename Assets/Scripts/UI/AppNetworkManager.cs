using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Instructor's connection polling, adapted for Block Puzzle.
// App update checks remain owned by BootstrapManager.
public class AppNetworkManager : MonoBehaviour
{
    public static AppNetworkManager Instance;
    public GameObject persistentRoot;
    public Canvas persistentCanvas;
    public GameObject connectionLostPanel;
    public Button tryAgainButton;
    public string pingUrl = "https://clients3.google.com/generate_204";
    [Min(1f)] public float checkIntervalSeconds = 3f;
    public bool IsConnectionLost { get; private set; }
    private bool checking;
    private bool ownsPause;
    private float savedScale;
    private GameManager pausedGame;
    private bool savedGamePaused;
    private int pauseScene;
    private UnityWebRequest activeRequest;

    private void Awake()
    {
        GameObject root = persistentRoot != null ? persistentRoot : gameObject;
        if (root.transform.parent != null || (root != gameObject && !transform.IsChildOf(root.transform)))
        { Debug.LogError("Network manager needs its own root containing the manager and Canvas.", this); enabled = false; return; }
        if (Instance != null && Instance != this) { Destroy(root); return; }
        Instance = this;
        DontDestroyOnLoad(root);
        if (persistentCanvas != null)
        {
            persistentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            persistentCanvas.overrideSorting = true;
            persistentCanvas.sortingOrder = 10000;
        }
        if (connectionLostPanel != null) connectionLostPanel.SetActive(false);
    }
    private void OnEnable()
    {
        if (Instance != this) return;
        if (tryAgainButton != null) tryAgainButton.onClick.AddListener(ManualConnectionRetry);
        StartCoroutine(Monitor());
    }
    private IEnumerator Monitor()
    {
        while (true)
        {
            yield return CheckConnection();
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, checkIntervalSeconds));
        }
    }
    public void ManualConnectionRetry()
    {
        if (!checking && isActiveAndEnabled) StartCoroutine(CheckConnection());
    }
    private IEnumerator CheckConnection()
    {
        if (checking) yield break;
        if (!System.Uri.TryCreate(pingUrl, System.UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
        { Debug.LogError("Assign a valid HTTP(S) Ping URL.", this); yield break; }
        checking = true;
        if (tryAgainButton != null) tryAgainButton.interactable = false;
        bool connected;
        using (var request = UnityWebRequest.Get(pingUrl))
        {
            activeRequest = request;
            request.timeout = 5;
            request.SetRequestHeader("Cache-Control", "no-cache");
            yield return request.SendWebRequest();
            connected = request.result == UnityWebRequest.Result.Success &&
                (request.responseCode == 200 || request.responseCode == 204);
            activeRequest = null;
        }
        checking = false;
        if (tryAgainButton != null) tryAgainButton.interactable = true;
        IsConnectionLost = !connected;
        if (connectionLostPanel != null) connectionLostPanel.SetActive(IsConnectionLost);
        if (IsConnectionLost) HoldPause(); else ReleasePause();
    }
    private void LateUpdate() { if (IsConnectionLost) HoldPause(); }
    private void HoldPause()
    {
        int scene = SceneManager.GetActiveScene().handle;
        if (!ownsPause || scene != pauseScene)
        {
            // A new scene has its own initial state; never restore an old scene's pause.
            savedScale = ownsPause && Time.timeScale == 0f ? 1f : Time.timeScale;
            pauseScene = scene;
            pausedGame = null;
            ownsPause = true;
        }
        if (GameManager.ins != null && pausedGame != GameManager.ins)
        {
            pausedGame = GameManager.ins;
            savedGamePaused = pausedGame.paused;
            if (InputManager.ins != null) InputManager.ins.ResetBlock();
        }
        if (pausedGame != null) pausedGame.paused = true;
        Time.timeScale = 0f;
    }
    private void ReleasePause()
    {
        if (!ownsPause) return;
        if (pausedGame != null) pausedGame.paused = savedGamePaused;
        Time.timeScale = savedScale;
        ownsPause = false;
        pausedGame = null;
    }
    private void OnDisable()
    {
        if (Instance != this) return;
        StopAllCoroutines();
        if (activeRequest != null) { activeRequest.Abort(); activeRequest.Dispose(); activeRequest = null; }
        checking = false;
        if (tryAgainButton != null)
        { tryAgainButton.onClick.RemoveListener(ManualConnectionRetry); tryAgainButton.interactable = true; }
        if (connectionLostPanel != null) connectionLostPanel.SetActive(false);
        IsConnectionLost = false;
        ReleasePause();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
