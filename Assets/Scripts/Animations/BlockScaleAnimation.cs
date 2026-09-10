using UnityEngine;
using System.Collections;

public class BlockScaleAnimation : MonoBehaviour 
{
    public void SetAnimation(bool isDragged, float t) 
    {
        Vector3 target = isDragged ? Vector3.one : new Vector3(0.5f, 0.5f, 1f);
        StartCoroutine(ScaleRoutine(t, target));
    }

    private IEnumerator ScaleRoutine(float time, Vector3 target) 
    {
        Vector3 start = transform.localScale;
        float elapsed = 0;
        
        while(elapsed < time) 
        {
            transform.localScale = Vector3.Lerp(start, target, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = target;
        this.enabled = false;
    }
}