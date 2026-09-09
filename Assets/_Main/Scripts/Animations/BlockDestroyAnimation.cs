using UnityEngine;
using System.Collections;

public class BlockDestroyAnimation : MonoBehaviour
{
    private bool isDestroying;

    // Existing callers, including the three-row helper, remain compatible.
    public void SetAnimation(float time)
    {
        PlayClear(time, 0f, 0.07f, 1.12f, null, 0);
    }

    public void PlayClear(float shrinkTime, float rippleDelay, float chargeTime,
        float pulseScale, LineClearEffects effects, int sparks)
    {
        if (isDestroying) return;
        isDestroying = true;
        enabled = true;
        StartCoroutine(ClearRoutine(Mathf.Max(0f, shrinkTime),
            Mathf.Max(0f, rippleDelay), Mathf.Max(0f, chargeTime),
            Mathf.Max(1f, pulseScale), effects, sparks));
    }

    private IEnumerator ClearRoutine(float shrinkTime, float rippleDelay,
        float chargeTime, float pulseScale, LineClearEffects effects, int sparks)
    {
        Vector3 startScale = transform.localScale;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color startColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;

        if (effects != null && sr != null) effects.Charge(sr, chargeTime);
        while (elapsed < chargeTime)
        {
            float t = elapsed / chargeTime;
            transform.localScale = startScale * Mathf.Lerp(1f, pulseScale,
                Mathf.Sin(t * Mathf.PI * 0.5f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = startScale * pulseScale;

        if (rippleDelay > 0f) yield return new WaitForSeconds(rippleDelay);
        if (effects != null && sr != null) effects.Burst(sr, sparks);

        elapsed = 0f;
        while (elapsed < shrinkTime)
        {
            float t = elapsed / shrinkTime;
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = startScale * pulseScale * (1f - ease);
            if (sr != null)
                sr.color = new Color(startColor.r, startColor.g, startColor.b,
                    startColor.a * (1f - t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
