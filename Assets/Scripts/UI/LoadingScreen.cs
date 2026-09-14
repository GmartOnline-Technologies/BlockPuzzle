using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening; 

public class LoadingScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform[] loadingBlocks;
    
    [Header("Settings")]
    public float timeBetweenBlocks = 0.5f;
    public string nextSceneName = "GameScene"; // Change to your actual main scene name

    void Start()
    {
        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        // 1. Hide all blocks instantly at the start by setting scale to 0
        foreach (RectTransform block in loadingBlocks)
        {
            block.localScale = Vector3.zero;
        }

        // 2. Animate each block popping in one by one
        foreach (RectTransform block in loadingBlocks)
        {
            // The OutBack ease gives it that punchy, snappy pop effect
            block.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(timeBetweenBlocks);
        }

        // 3. Brief pause for the player to register the bar is full
        yield return new WaitForSeconds(0.4f);
        
        // 4. Load the actual game
        SceneManager.LoadScene(nextSceneName);
    }
}