using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class BlockFadeAnimation : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite originalSprite;
    private Color originalColor;
    private bool isPreviewing;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void ShowPreview(float duration, Sprite previewSprite, float alpha = 0.75f)
    {
        if (previewSprite == null) return;
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // Capture at preview time: a spawned tile may receive its sprite after Awake.
        if (!isPreviewing)
        {
            originalSprite = sr.sprite;
            originalColor = sr.color;
            isPreviewing = true;
        }

        StopAllCoroutines();
        enabled = true;
        sr.sprite = previewSprite;

        // Colored artwork must use a white tint to avoid multiplying its colors.
        float startAlpha = sr.color.a;
        sr.color = new Color(1f, 1f, 1f, startAlpha);
        StartCoroutine(FadeRoutine(duration, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha))));
    }

    public void ClearPreview()
    {
        if (!isPreviewing) return;

        // Restore immediately so a pending fade cannot override placement opacity.
        StopAllCoroutines();
        sr.sprite = originalSprite;
        sr.color = originalColor;
        isPreviewing = false;
        enabled = false;
    }

    // Keep the existing color-animation API for other callers.
    // Line previews use ShowPreview instead of guessing from color equality.
    public void SetAnimation(float duration, Color targetColor)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        ClearPreview();
        StopAllCoroutines();
        enabled = true;
        StartCoroutine(FadeRoutine(duration, targetColor));
    }

    private IEnumerator FadeRoutine(float duration, Color targetColor)
    {
        Color startColor = sr.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            sr.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.color = targetColor;
        enabled = false;
    }
}