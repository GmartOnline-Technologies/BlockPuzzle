using UnityEngine;

public class Block : MonoBehaviour
{
    public int prefabIndex;
    public Color defaultColor;
    public Vector2 size;
    public Vector2Int[] structure;

    [HideInInspector]
    public bool movable = true;
    [HideInInspector]
    public int posIndex;
    [HideInInspector]
    public Vector3 basePosition;
    [HideInInspector]
    public Vector3 baseScale;
    [HideInInspector]
    public Vector3 scaledScale;

   public void SetBasePosition(int i, bool cp = true)
{
    float boardCenterX = (BoardManager.BOARD_SIZE - 1) / 2.0f;
    float blockSpacing = 3.2f; // Distance between the 3 tray slots
    
    float slotCenterX = boardCenterX + ((i - 1) * blockSpacing);

    // Calculate how wide the shape is, multiplied by our new smaller tray scale (0.5f)
    float shapeCenterX = (size.x - 1) / 2.0f;
    float scaledOffset = shapeCenterX * 0.5f; // <-- This MUST match the 0.5f in the Scale method!

    // Shift the block left by the offset so it centers perfectly in its UI slot
    float finalX = slotCenterX - scaledOffset;

    basePosition = new Vector3(finalX, GameScaler.GetBlockY(), 0);

    if (cp)
        transform.position = basePosition;

    posIndex = i;
} 

public void Move(float t, Vector3 d)
{
    GetComponent<BlockMovingAnimation>().enabled = true;
    GetComponent<BlockMovingAnimation>().SetAnimation(t, d);
}
    public bool IsMoving()
    {
        return GetComponent<BlockMovingAnimation>().enabled;
    }

    public void Scale(bool isDragged, float t)
{
    GetComponent<BlockScaleAnimation>().enabled = true;
    GetComponent<BlockScaleAnimation>().SetAnimation(isDragged, t);
    
    if (isDragged)
    {
        // Full size tiles
        ScaleTiles(baseScale);
    }
    else
    {
        // Tiles scale down slightly more to keep the crisp gaps between cells.
        ScaleTiles(baseScale * 0.85f); 
    }
}

    public bool IsScaling()
    {
        return GetComponent<BlockScaleAnimation>().enabled;
    }

    public Vector2Int GetFirstCoords()
    {
        Vector3 p;
        p = transform.GetChild(0).transform.position;
        return new Vector2Int((int)(p.x + 0.5f), (int)(p.y + 0.5f));
    }

    public Color GetColor()
    {
        if (transform.GetChild(0).name == "Block tile")
            return transform.GetChild(0).GetComponent<SpriteRenderer>().color;

        return transform.GetChild(1).GetComponent<SpriteRenderer>().color;
    }

   public void ChangeColor(Color c)
    {
        foreach (Transform t in transform)
        {
            if (t.name == "Block tile")
            {
                // Only change the Alpha (transparency) so the pre-colored sprite stays bright!
                Color originalColor = t.GetComponent<SpriteRenderer>().color;
                originalColor.a = c.a; 
                t.GetComponent<SpriteRenderer>().color = originalColor;
            }
        }
    }

    public void ScaleTiles(Vector3 s)
    {
        foreach (Transform t in transform)
            t.localScale = s;
    }

   private void Awake()
{
    // Grab the perfect fit scale from the board manager
    baseScale = BoardManager.ins.boardTileScale;
    
    // Initialize the block in the "Tray State" (with gaps)
    Scale(false, 0f); 
}
}
