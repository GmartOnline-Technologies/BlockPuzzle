using UnityEngine;
using System.Collections;

public class BlockMovingAnimation : MonoBehaviour 
{
    public void SetAnimation(float t, Vector3 d) 
    {
        StartCoroutine(MoveRoutine(t, d));
    }

    private IEnumerator MoveRoutine(float time, Vector3 target) 
    {
        Vector3 start = transform.position;
        float elapsed = 0;
        
        while(elapsed < time) 
        {
            transform.position = Vector3.Lerp(start, target, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = target;
        this.enabled = false;
    }
}