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
        return GetComponent<BlockMovingAnimation>().IsAnimating;
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
        return GetComponent<BlockScaleAnimation>().IsAnimating;
    }

    public Color GetColor()
    {
        if (Tiles != null)
            foreach (BlockTile tile in Tiles)
                if (tile != null)
                {
                    SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                    if (renderer != null) return renderer.color;
                }
        return Color.white;
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
    if (!RebuildLayout())
        Debug.LogError("Block tiles must occupy distinct cells one unit apart. Check this prefab's child positions.", this);
    // Grab the perfect fit scale from the board manager
    baseScale = BoardManager.ins.boardTileScale;
    
    // Initialize the block in the "Tray State" (with gaps)
    Scale(false, 0f); 
}

    public BlockTile[] Tiles { get; private set; }
    public Vector2Int[] TileCells { get; private set; }
    public Vector3 LocalGridOrigin { get; private set; }
    public bool HasValidLayout { get; private set; }
    public int QuarterTurns { get; private set; }

    public static int RoundCell(float value)
    {
        return Mathf.FloorToInt(value + 0.5f);
    }

    public bool RebuildLayout()
    {
        HasValidLayout = false;
        var tiles = new System.Collections.Generic.List<BlockTile>();
        var childIndices = new System.Collections.Generic.List<int>();
        Vector3 minimum = new Vector3(float.MaxValue, float.MaxValue, 0f);
        Vector3 maximum = new Vector3(float.MinValue, float.MinValue, 0f);
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Trim() != "Block tile") continue;
            BlockTile tile = child.GetComponent<BlockTile>();
            if (tile == null) return false;
            // Normalize whitespace so existing effects recognize this real tile too.
            child.name = "Block tile";
            tiles.Add(tile);
            childIndices.Add(i);
            minimum.x = Mathf.Min(minimum.x, child.localPosition.x);
            minimum.y = Mathf.Min(minimum.y, child.localPosition.y);
            maximum.x = Mathf.Max(maximum.x, child.localPosition.x);
            maximum.y = Mathf.Max(maximum.y, child.localPosition.y);
        }
        if (tiles.Count == 0) return false;
        Tiles = tiles.ToArray();
        TileCells = new Vector2Int[Tiles.Length];
        structure = new Vector2Int[transform.childCount];
        var occupied = new System.Collections.Generic.HashSet<Vector2Int>();
        for (int i = 0; i < Tiles.Length; i++)
        {
            Vector3 offset = Tiles[i].transform.localPosition - minimum;
            Vector2Int cell = new Vector2Int(RoundCell(offset.x), RoundCell(offset.y));
            if (Mathf.Abs(offset.x - cell.x) > 0.01f || Mathf.Abs(offset.y - cell.y) > 0.01f
                || !occupied.Add(cell)) return false;
            TileCells[i] = cell;
            structure[childIndices[i]] = cell;
        }
        LocalGridOrigin = minimum;
        size = new Vector2(RoundCell(maximum.x - minimum.x) + 1, RoundCell(maximum.y - minimum.y) + 1);
        HasValidLayout = true;
        return true;
    }

    public Vector2Int GetFirstCoords()
    {
        Vector3 origin = transform.TransformPoint(LocalGridOrigin);
        return new Vector2Int(RoundCell(origin.x), RoundCell(origin.y));
    }

    public Vector3 GetPlacementPosition(Vector2Int origin)
    {
        // Preserve the actual prefab pivot, including centered half-cell offsets.
        return new Vector3(origin.x, origin.y, -1f) - transform.TransformVector(LocalGridOrigin);
    }

    public void Rotate90()
    {
        if (!HasValidLayout || !enabled) return;
        GetComponent<BlockMovingAnimation>().Cancel();
        Scale(false, 0f);
        foreach (Transform child in transform)
        {
            if (child.name.Trim() != "Block tile" && child.name.Trim() != "Fake block tile") continue;
            Vector3 p = child.localPosition;
            // Rotate floats first: centered coordinates such as +/-0.5 stay distinct.
            child.localPosition = new Vector3(p.y, -p.x, p.z);
        }
        QuarterTurns = (QuarterTurns + 1) % 4;
        if (!RebuildLayout()) return;
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.center = LocalGridOrigin + new Vector3((size.x - 1f) * 0.5f, (size.y - 1f) * 0.5f, 0f);
            collider.size = new Vector3(size.x, size.y, collider.size.z);
        }
        SetBasePosition(posIndex, true);
    }
}
