using UnityEngine;

public class BlockTile : MonoBehaviour
{
    [HideInInspector]
    public Color defaultColor;

    public void Fade(float d, Color c)
{
    // Instantly apply the color since we don't have the animation script yet
    GetComponent<SpriteRenderer>().color = c;
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
