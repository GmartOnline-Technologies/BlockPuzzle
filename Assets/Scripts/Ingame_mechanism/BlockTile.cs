using UnityEngine;

public class BlockTile : MonoBehaviour
{
    [HideInInspector]
    public Color defaultColor;

    private BlockFadeAnimation fadeAnimation;

    private BlockFadeAnimation GetFadeAnimation()
    {
        if (fadeAnimation == null)
        {
            fadeAnimation = GetComponent<BlockFadeAnimation>();
            if (fadeAnimation == null)
                fadeAnimation = gameObject.AddComponent<BlockFadeAnimation>();
        }
        return fadeAnimation;
    }

    public void Fade(float d, Color c)
    {
        GetFadeAnimation().SetAnimation(d, c);
    }

    public void ShowPreview(float duration, Sprite sprite)
    {
        GetFadeAnimation().ShowPreview(duration, sprite);
    }

    public void ClearPreview()
    {
        if (fadeAnimation != null)
            fadeAnimation.ClearPreview();
    }

    public void Fall(float d, BlockFallAnimation.Direction dir)
    {
        BlockFallAnimation anim = GetComponent<BlockFallAnimation>();
        anim.enabled = true;
        anim.SetAnimation(d, dir);
    }

    public void Destroy(float d)
    {
        BlockDestroyAnimation anim = GetComponent<BlockDestroyAnimation>();
        anim.enabled = true;
        anim.SetAnimation(d);
    }

    private void Awake()
	{
        defaultColor = GetComponent<SpriteRenderer>().color;
	}
}