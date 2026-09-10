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
        float boardCenterX = (BoardManager.BOARD_SIZE - 1) / 2f;
        float slotSpacing = BoardManager.BOARD_SIZE / (float)BoardManager.BLOCKS_AMOUNT;
        float slotCenterX = boardCenterX + (i - 1) * slotSpacing;
        Vector3 slotCenter = new Vector3(slotCenterX, GameScaler.GetBlockY(), 0f);

        // Measure the artwork at its final tray size, independent of drag animations.
        Bounds artwork = new Bounds();
        bool hasArtwork = false;
        Vector3 trayTileScale = baseScale * 0.85f;
        foreach (Transform child in transform)
        {
            if (child.name.Trim() != "Block tile") continue;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) continue;
            Bounds spriteBounds = renderer.sprite.bounds;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? spriteBounds.min.x : spriteBounds.max.x,
                    (corner & 2) == 0 ? spriteBounds.min.y : spriteBounds.max.y,
                    spriteBounds.center.z);
                if (renderer.flipX) point.x = -point.x;
                if (renderer.flipY) point.y = -point.y;
                point = child.localPosition + child.localRotation * Vector3.Scale(point, trayTileScale);
                if (!hasArtwork) { artwork = new Bounds(point, Vector3.zero); hasArtwork = true; }
                else artwork.Encapsulate(point);
            }
        }

        Vector3 center = hasArtwork ? artwork.center : Vector3.zero;
        Vector3 offset = transform.localRotation * Vector3.Scale(center, new Vector3(0.5f, 0.5f, 1f));
        if (transform.parent != null) offset = transform.parent.TransformVector(offset);
        basePosition = slotCenter - offset;
        if (cp) transform.position = basePosition;
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
