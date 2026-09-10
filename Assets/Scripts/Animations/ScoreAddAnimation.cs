using UnityEngine;
using TMPro;
using System.Globalization;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class ScoreAddAnimation : MonoBehaviour
{
    private TextMeshProUGUI label;
    private int startValue, targetValue, displayedValue;
    private float elapsed, duration;
    private bool animating;
    private Vector3 restScale;

    private void EnsureLabel()
    {
        if (label == null) label = GetComponent<TextMeshProUGUI>();
    }

    // Compatibility with existing callers: s is the OLD total, p is the reward.
    public void SetAnimation(int p, int s, float d)
    {
        AnimateTo(s + p, d);
    }

    public void AnimateTo(int total, float seconds)
    {
        EnsureLabel();
        if (!animating)
        {
            if (!int.TryParse(label.text, NumberStyles.Integer | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out displayedValue))
                displayedValue = 0;
            restScale = transform.localScale;
        }

        startValue = displayedValue;
        targetValue = total;
        elapsed = 0f;
        duration = Mathf.Max(0f, seconds);
        if (duration <= 0f || startValue == targetValue)
        {
            SetImmediate(total);
            return;
        }
        animating = true;
        enabled = true;
    }

    public void SetImmediate(int total)
    {
        EnsureLabel();
        if (animating) transform.localScale = restScale;
        animating = false;
        displayedValue = startValue = targetValue = total;
        label.text = total.ToString();
        enabled = false;
    }

    private void Update()
    {
        if (!animating) return;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        // Double precision keeps integer totals exact even at large scores.
        displayedValue = (int)System.Math.Round(startValue + ((double)targetValue - startValue) * eased);
        label.text = displayedValue.ToString();
        transform.localScale = restScale * (1f + 0.1f * Mathf.Sin(t * Mathf.PI));
        if (t >= 1f) SetImmediate(targetValue);
    }

    private void OnDisable()
    {
        if (!animating) return;
        // Disabling an active animation leaves a correct, final counter value.
        transform.localScale = restScale;
        animating = false;
        displayedValue = targetValue;
        if (label != null) label.text = targetValue.ToString();
    }
}
