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
    // Instantly snap to the destination coordinate instead of animating
    transform.position = d; 
}
    public bool IsMoving()
    {
        return GetComponent<BlockMovingAnimation>().enabled;
    }

    public void Scale(bool isDragged, float t)
{
    if (isDragged)
    {
        // 1. DRAGGED STATE: Full size (1.0) so the grid collision math works perfectly.
        transform.localScale = Vector3.one;
        ScaleTiles(baseScale);
    }
    else
    {
        // 2. TRAY STATE: Reduced from 0.65f to 0.5f so large shapes fit cleanly on screen.
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        
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
            if (t.name == "Block tile")
                t.GetComponent<SpriteRenderer>().color = c;
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
