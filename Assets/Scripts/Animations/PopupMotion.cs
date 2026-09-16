using DG.Tweening;
using UnityEngine;

// Attach to the individual popup/card, never to the full Tutorial Canvas.
[DisallowMultipleComponent]
public class PopupMotion : MonoBehaviour
{
    public float duration = 0.38f;
    [Range(0.5f, 1f)] public float startScale = 0.85f;
    private Vector3 restScale;
    private CanvasGroup group;
    private Tween motion;
    private bool initialized;
    private void Initialize()
    {
        if (initialized) return;
        initialized = true; restScale = transform.localScale;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }
    private void OnEnable() { Play(); }
    public void Play()
    {
        Initialize(); Stop();
        transform.localScale = restScale * startScale;
        group.alpha = 0f; group.interactable = false; group.blocksRaycasts = true;
        motion = DOTween.Sequence()
            .Join(transform.DOScale(restScale, Mathf.Max(0.1f, duration)).SetEase(Ease.OutBack))
            .Join(group.DOFade(1f, Mathf.Max(0.1f, duration) * 0.6f))
            .SetUpdate(true).OnComplete(() => { if (group != null) group.interactable = true; });
    }
    public void ErrorFeedback()
    {
        Initialize(); Stop(); group.alpha = 1f; group.interactable = true;
        transform.localScale = restScale;
        motion = transform.DOPunchScale(restScale * 0.025f, 0.25f, 3, 0.3f).SetUpdate(true);
    }
    public static void Show(GameObject target)
    {
        if (target == null) return;
        PopupMotion popup = target.GetComponent<PopupMotion>();
        bool wasActive = target.activeInHierarchy;
        if (popup == null) popup = target.AddComponent<PopupMotion>();
        else if (wasActive) popup.Play();
        target.SetActive(true);
    }
    private void Stop() { if (motion != null) { motion.Kill(); motion = null; } }
    private void OnDisable()
    {
        Stop();
        if (!initialized) return;
        transform.localScale = restScale; group.alpha = 1f; group.interactable = true;
    }
    private void OnDestroy() { Stop(); }
}
