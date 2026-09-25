using DG.Tweening;
using TMPro;
using UnityEngine;

// Attach to a TMP UI label under the HomeScene's always-active Canvas.
// Keep outside panels that navigation hides. No DontDestroyOnLoad is needed.
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class HomeWelcomeText : MonoBehaviour
{
    [Header("Home-only visibility")]
    public GameObject homePanel;
    [Tooltip("Assign Settings, Shop, Leaderboard, loading and any other popup roots that hide this greeting.")]
    public GameObject[] hideWhenPanelsOpen;
    private bool showing;

    [Header("Player name")]
    public string usernamePreferenceKey = "Username";
    public string welcomePrefix = "WELCOME";
    public string fallbackName = "PLAYER";
    public bool nameOnSecondLine = true;

    [Header("Gentle wiggle")]
    public bool animate = true;
    [Range(0f, 8f)] public float wiggleDegrees = 2f;
    [Min(0.05f)] public float stepSeconds = 0.3f;
    [Min(0f)] public float restSeconds = 2f;

    private TextMeshProUGUI label;
    private RectTransform rect;
    private Quaternion restingRotation;
    private Sequence wiggle;
    private float nextRefresh;
    private string lastText;

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        rect = label.rectTransform;
        restingRotation = rect.localRotation;
        label.raycastTarget = false;
        // Player names must be displayed literally, not interpreted as TMP tags.
        label.richText = false;
    }

    private void OnEnable()
    {
        showing = false;
        label.enabled = false;
        RefreshName();
        RefreshVisibility();
    }

    private void LateUpdate()
    {
        // Run after navigation updates, including while Time.timeScale is zero.
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (label == null) return;
        bool visible = homePanel != null && homePanel.activeInHierarchy;
        if (visible && hideWhenPanelsOpen != null)
            foreach (GameObject panel in hideWhenPanelsOpen)
                if (panel != null && panel.activeInHierarchy) { visible = false; break; }
        if (showing == visible) return;
        showing = visible;
        // Hide the renderer only: this controller must remain active to detect returning Home.
        label.enabled = visible;
        if (visible) { RefreshName(); RestartWiggle(); }
        else StopWiggle();
    }

    private void Update()
    {
        // Also refresh if the saved profile name changes while HomeScene is open.
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.5f;
        RefreshName();
    }

    public void RefreshName()
    {
        if (label == null) return;
        string username = PlayerPrefs.GetString(usernamePreferenceKey, "").Trim();
        if (string.IsNullOrWhiteSpace(username)) username = fallbackName;
        username = username.Replace('\r', ' ').Replace('\n', ' ');
        string text = welcomePrefix + (nameOnSecondLine ? "\n" : " ") + username;
        if (lastText == text) return;
        lastText = text;
        label.text = text;
    }

    public void RestartWiggle()
    {
        StopWiggle();
        if (!animate || !showing || !isActiveAndEnabled || rect == null) return;
        Vector3 neutral = restingRotation.eulerAngles;
        float duration = Mathf.Max(0.05f, stepSeconds);
        wiggle = DOTween.Sequence();
        wiggle.Append(rect.DOLocalRotate(neutral + Vector3.forward * wiggleDegrees, duration).SetEase(Ease.InOutSine));
        wiggle.Append(rect.DOLocalRotate(neutral - Vector3.forward * wiggleDegrees, duration * 2f).SetEase(Ease.InOutSine));
        wiggle.Append(rect.DOLocalRotate(neutral, duration).SetEase(Ease.InOutSine));
        wiggle.AppendInterval(Mathf.Max(0f, restSeconds));
        wiggle.SetLoops(-1, LoopType.Restart).SetUpdate(true);
    }

    private void StopWiggle()
    {
        if (wiggle != null) { wiggle.Kill(); wiggle = null; }
        if (rect != null) rect.localRotation = restingRotation;
    }

    private void OnDisable() { StopWiggle(); showing = false; if (label != null) label.enabled = false; }
    private void OnDestroy() { StopWiggle(); }
}
