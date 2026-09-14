using UnityEngine;
using System.Collections;

public class BlockScaleAnimation : MonoBehaviour
{
    public bool IsAnimating { get; private set; }

    public void SetAnimation(bool isDragged, float time)
    {
        Vector3 target = isDragged ? Vector3.one : new Vector3(0.5f, 0.5f, 1f);
        Cancel();
        if (time <= 0f)
        {
            transform.localScale = target;
            return;
        }
        enabled = true;
        IsAnimating = true;
        StartCoroutine(Animate(time, target));
    }

    public void Cancel()
    {
        StopAllCoroutines();
        IsAnimating = false;
        enabled = false;
    }

    private IEnumerator Animate(float time, Vector3 target)
    {
        Vector3 start = transform.localScale;
        float elapsed = 0f;
        while (elapsed < time)
        {
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / time));
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = target;
        IsAnimating = false;
        enabled = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsAnimating = false;
    }
}
