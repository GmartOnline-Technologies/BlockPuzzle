using UnityEngine;
using System.Collections;

public class BlockDestroyAnimation : MonoBehaviour 
{
    public void SetAnimation(float time) 
    {
        StartCoroutine(ShrinkRoutine(time));
    }

    private IEnumerator ShrinkRoutine(float time) 
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0;
        
        while (elapsed < time) 
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }
}