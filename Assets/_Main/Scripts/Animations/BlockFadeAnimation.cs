using UnityEngine;
using System.Collections;

public class BlockFadeAnimation : MonoBehaviour 
{
    private Sprite originalSprite;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalSprite = sr.sprite; // Save the true sprite of this specific block (e.g., green or red)
    }

    public void SetAnimation(float duration, Color targetColor) 
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (originalSprite == null) originalSprite = sr.sprite;
        
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(duration, targetColor));
    }

    private IEnumerator FadeRoutine(float duration, Color targetColor) 
    {
        BlockTile tile = GetComponent<BlockTile>();
        Sprite targetSprite = originalSprite;
        Color targetColorAlpha = Color.white;

        // If the targetColor doesn't match this tile's default color, it means we are hovering a different colored piece over it!
        if (targetColor != tile.defaultColor && InputManager.ins.draggedBlock != null)
        {
            Block dragged = InputManager.ins.draggedBlock;
            
            // Grab the sprite image directly from the dragged block
            Transform firstTile = null;
            foreach(Transform child in dragged.transform)
            {
                if(child.name == "Block tile")
                {
                    firstTile = child;
                    break;
                }
            }

            if(firstTile != null)
            {
                targetSprite = firstTile.GetComponent<SpriteRenderer>().sprite;
            }

            // Make it slightly transparent so it looks like a preview overlay
            targetColorAlpha = new Color(1f, 1f, 1f, 0.75f); 
        }
        else
        {
            // Revert state: put the original sprite and solid color back when you move the piece away
            targetSprite = originalSprite;
            targetColorAlpha = Color.white;
        }

        // Swap the sprite image
        sr.sprite = targetSprite;

        Color startColor = sr.color;
        float elapsed = 0;
        
        while(elapsed < duration) 
        {
            sr.color = Color.Lerp(startColor, targetColorAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        sr.color = targetColorAlpha;
        this.enabled = false;
    }
}