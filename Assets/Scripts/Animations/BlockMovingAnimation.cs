using UnityEngine;
using System.Collections;

public class BlockMovingAnimation : MonoBehaviour
{
    public bool IsAnimating { get; private set; }

    public void SetAnimation(float time, Vector3 target)
    {
        Cancel();
        if (time <= 0f)
        {
            transform.position = target;
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
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < time)
        {
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / time));
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = target;
        IsAnimating = false;
        enabled = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsAnimating = false;
    }
}
