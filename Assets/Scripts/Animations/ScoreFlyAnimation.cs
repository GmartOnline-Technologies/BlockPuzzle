using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ScoreFlyAnimation : MonoBehaviour
{
    [Header("Flying reward")]
    public Sprite coinSprite;
    public Color rewardColor = new Color(1f, 0.86f, 0.25f, 1f);
    [Header("Popup size")]
    [Range(1f, 3f)] public float popupSizeMultiplier = 1.8f;
    [Min(0f)] public float holdDuration = 0.15f;
    [Min(0.05f)] public float flightDuration = 0.55f;
    [Range(0f, 0.5f)] public float arcHeight = 0.14f;

    private class Flight
    {
        public RectTransform rect;
        public CanvasGroup group;
        public TextMeshProUGUI target;
        public Vector2 start;
        public float age, hold, duration, arc;
        public Action landed;
    }

    private Canvas overlay;
    private readonly List<Flight> flights = new List<Flight>();

    // Returns false when the UI is unavailable; the caller then updates immediately.
    public bool Play(int amount, Vector3 worldOrigin, TextMeshProUGUI target, Action landed)
    {
        if (!isActiveAndEnabled || target == null || !target.isActiveAndEnabled) return false;
        EnsureCanvas();
        Camera camera = Camera.main;
        Vector3 source = camera != null ? camera.WorldToScreenPoint(worldOrigin)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);
        if (source.z <= 0f)
            source = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);

        float fontSize = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.065f, 24f, 56f)
            * Mathf.Clamp(popupSizeMultiplier, 1f, 3f);
        // Fit the larger amount and optional coin together on narrow phone screens.
        float totalWidthInEm = coinSprite != null ? 4.8f : 3.6f;
        fontSize = Mathf.Min(fontSize, Screen.safeArea.width * 0.9f / totalWidthInEm);
        float labelWidth = fontSize * 3.6f;
        float iconSpace = coinSprite != null ? fontSize * 1.2f : 0f;
        GameObject root = new GameObject("Earned coins +" + amount, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(overlay.transform, false);
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(labelWidth + iconSpace, fontSize * 1.5f);

        GameObject textObject = new GameObject("Reward amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = target.font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = rewardColor;
        text.text = "+" + amount;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(labelWidth, fontSize * 1.5f);
        text.rectTransform.anchoredPosition = new Vector2(iconSpace * 0.5f, 0f);

        if (coinSprite != null)
        {
            GameObject iconObject = new GameObject("Coin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(root.transform, false);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = coinSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.sizeDelta = new Vector2(fontSize, fontSize);
            icon.rectTransform.anchoredPosition = new Vector2(-labelWidth * 0.5f, 0f);
        }

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
        Rect safe = Screen.safeArea;
        float halfWidth = rect.sizeDelta.x * 0.54f;
        float halfHeight = rect.sizeDelta.y * 0.54f;
        Vector2 start = new Vector2(Mathf.Clamp(source.x, safe.xMin + halfWidth, safe.xMax - halfWidth),
            Mathf.Clamp(source.y, safe.yMin + halfHeight, safe.yMax - halfHeight));
        rect.anchoredPosition = start;
        flights.Add(new Flight { rect = rect, group = group, target = target, start = start,
            hold = Mathf.Max(0f, holdDuration), duration = Mathf.Max(0.05f, flightDuration),
            arc = Mathf.Min(Screen.width, Screen.height) * arcHeight, landed = landed });
        return true;
    }

    private void EnsureCanvas()
    {
        if (overlay != null) return;
        GameObject root = new GameObject("Flying score overlay", typeof(RectTransform), typeof(Canvas));
        overlay = root.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 400;
        // No GraphicRaycaster: reward visuals do not consume touches.
    }

    private Vector2 GetDestination(TextMeshProUGUI target)
    {
        Canvas canvas = target.canvas != null ? target.canvas.rootCanvas : null;
        Camera camera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        return RectTransformUtility.WorldToScreenPoint(camera,
            target.rectTransform.TransformPoint(target.rectTransform.rect.center));
    }

    private void Update()
    {
        // Oldest arrivals are processed first; callbacks can safely cancel remaining flights.
        for (int i = 0; i < flights.Count;)
        {
            Flight f = flights[i];
            f.age += Time.deltaTime;
            if (f.target == null || f.rect == null || f.age >= f.hold + f.duration)
            {
                flights.RemoveAt(i);
                if (f.rect != null) Destroy(f.rect.gameObject);
                if (f.landed != null) f.landed();
                continue;
            }

            if (f.age < f.hold)
            {
                float intro = f.age / Mathf.Max(0.001f, f.hold);
                f.group.alpha = Mathf.Clamp01(intro * 4f);
                f.rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.08f, Mathf.SmoothStep(0f, 1f, intro));
            }
            else
            {
                float t = Mathf.Clamp01((f.age - f.hold) / f.duration);
                float travel = t * t * (3f - 2f * t);
                Vector2 end = GetDestination(f.target);
                Vector2 control = (f.start + end) * 0.5f + Vector2.up * f.arc;
                float inverse = 1f - travel;
                f.rect.anchoredPosition = inverse * inverse * f.start
                    + 2f * inverse * travel * control + travel * travel * end;
                f.rect.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.45f, travel);
                f.group.alpha = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.85f) / 0.15f);
            }
            i++;
        }
    }

    public void CancelAll()
    {
        foreach (Flight f in flights)
            if (f.rect != null) Destroy(f.rect.gameObject);
        flights.Clear();
    }

    private void OnDisable() { CancelAll(); }
    private void OnDestroy()
    {
        CancelAll();
        if (overlay != null) Destroy(overlay.gameObject);
    }
}