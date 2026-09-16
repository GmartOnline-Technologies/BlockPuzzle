using System.Collections;
using DG.Tweening;
using UnityEngine;

// Visual-only loading indicator. BootstrapManager owns progress and navigation.
public class LoadingScreen : MonoBehaviour
{
    public RectTransform[] loadingBlocks;
    [Min(0.01f)] public float timeBetweenBlocks = 0.12f;
    [Min(0.05f)] public float popDuration = 0.35f;
    private Vector3[] scales;
    private Tween[] tweens;
    private int shown;
    private float progress, nextPop;
    private bool running;

    private void Initialize()
    {
        if (scales != null) return;
        int count = loadingBlocks == null ? 0 : loadingBlocks.Length;
        scales = new Vector3[count]; tweens = new Tween[count];
        for (int i = 0; i < count; i++)
            scales[i] = loadingBlocks[i] != null && loadingBlocks[i].localScale != Vector3.zero
                ? loadingBlocks[i].localScale : Vector3.one;
    }
    public void BeginLoading()
    {
        Initialize(); StopTweens();
        shown = 0; progress = 0f; nextPop = 0f; running = true;
        foreach (RectTransform block in loadingBlocks ?? new RectTransform[0])
            if (block != null) block.localScale = Vector3.zero;
    }
    public void SetProgress(float value) { progress = Mathf.Clamp01(value); }
    private void Update()
    {
        if (!running || scales == null) return;
        int target = Mathf.FloorToInt(progress * scales.Length + 0.0001f);
        if (shown >= target || Time.unscaledTime < nextPop) return;
        int index = shown++;
        if (loadingBlocks[index] != null)
            tweens[index] = loadingBlocks[index].DOScale(scales[index], Mathf.Max(0.05f, popDuration))
                .SetEase(Ease.OutBack).SetUpdate(true);
        nextPop = Time.unscaledTime + Mathf.Max(0.01f, timeBetweenBlocks);
    }
    public IEnumerator CompleteLoading()
    {
        Initialize(); progress = 1f;
        while (isActiveAndEnabled && running && shown < scales.Length) yield return null;
        // Let the final pop settle before hiding the bar.
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, popDuration) + 0.15f);
        running = false;
    }
    private void StopTweens()
    {
        if (tweens != null) foreach (Tween tween in tweens) if (tween != null) tween.Kill();
    }
    private void OnDisable() { running = false; StopTweens(); }
    private void OnDestroy() { StopTweens(); }
}
