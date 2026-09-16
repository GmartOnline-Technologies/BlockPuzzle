using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Transform visual;
    private Button button;
    private Vector3 rest;
    private Tween tween;
    private void Awake()
    {
        button = GetComponent<Button>();
        if (visual == null) visual = transform;
        rest = visual.localScale;
    }
    public void OnPointerDown(PointerEventData data)
    {
        if (!button.IsInteractable()) return;
        Animate(rest * 0.94f, 0.08f, Ease.OutQuad);
    }
    public void OnPointerUp(PointerEventData data) { Animate(rest, 0.2f, Ease.OutBack); }
    public void OnPointerExit(PointerEventData data) { Animate(rest, 0.15f, Ease.OutQuad); }
    private void Animate(Vector3 target, float duration, Ease ease)
    {
        if (tween != null) tween.Kill();
        tween = visual.DOScale(target, duration).SetEase(ease).SetUpdate(true);
    }
    private void OnDisable()
    {
        if (tween != null) tween.Kill();
        if (visual != null) visual.localScale = rest;
    }
}
