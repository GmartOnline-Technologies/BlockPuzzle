using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager ins;

    public const int BOARD_SIZE = 8;
    public const int BLOCKS_AMOUNT = 3;

    public GameObject boardTilePrefabA;
    public GameObject boardTilePrefabB;
    public GameObject blockTilePrefab;
    public GameObject[] blockPrefabs; 
    
    public Transform gameTransform;
    public Transform boardTransform;
    public Color boardColor;
    public Color highlightColor;

    [Header("Tutorial auto-fill")]
    [Tooltip("Enable only in your Tutorial scene. Generates the cross and one square piece.")]
    public bool tutorialMode;
    public Sprite tutorialBlueSprite;
    public Sprite tutorialPurpleSprite;
    [Tooltip("Zero-based index of the 2 by 2 square in Block Prefabs.")]
    [Min(0)] public int tutorialSquarePrefabIndex;
    public GameOverController gameOverUI;

    private Block tutorialPiece;

    [HideInInspector]
    public Vector3 boardTileScale;
    [HideInInspector]
    public Vector3 scaledBlockTileScale;
    [HideInInspector]
    public SpriteRenderer[,] boardTiles = new SpriteRenderer[BOARD_SIZE, BOARD_SIZE];
    [HideInInspector]
    public BlockTile[,] boardBlocks = new BlockTile[BOARD_SIZE, BOARD_SIZE];
    [HideInInspector]
    public Block[] blocks = new Block[BLOCKS_AMOUNT];

    public bool IsInRange(Vector2 o, Vector2 e)
    {
        return o.x >= -0.5f && o.y >= -0.5f && e.x >= o.x && e.y >= o.y
            && e.x < BOARD_SIZE - 0.5f && e.y < BOARD_SIZE - 0.5f;
    }

    public bool IsEmpty(Block block, Vector2 origin)
    {
        if (!(origin.x >= 0f && origin.x < BOARD_SIZE && origin.y >= 0f && origin.y < BOARD_SIZE)) return false;
        return CanPlace(block, new Vector2Int(Block.RoundCell(origin.x), Block.RoundCell(origin.y)));
    }

    public bool CanPlace(Block block, Vector2Int origin)
    {
        if (block == null || !block.HasValidLayout || block.Tiles == null || block.Tiles.Length == 0) return false;
        if (tutorialMode && (block != tutorialPiece || origin != new Vector2Int(3, 3))) return false;
        if (origin.x < 0 || origin.x >= BOARD_SIZE || origin.y < 0 || origin.y >= BOARD_SIZE) return false;
        for (int i = 0; i < block.Tiles.Length; i++)
        {
            if (block.Tiles[i] == null) return false;
            long x = (long)origin.x + block.TileCells[i].x;
            long y = (long)origin.y + block.TileCells[i].y;
            if (x < 0 || x >= BOARD_SIZE || y < 0 || y >= BOARD_SIZE) return false;
            if (boardBlocks[(int)x, (int)y] != null) return false;
        }
        return true;
    }

    public bool TryGetPlacement(Block block, out Vector2Int origin, out Vector3 rootPosition)
    {
        origin = Vector2Int.zero;
        rootPosition = Vector3.zero;
        if (block == null || !block.HasValidLayout) return false;
        origin = block.GetFirstCoords();
        if (!CanPlace(block, origin)) return false;
        rootPosition = block.GetPlacementPosition(origin);
        // Require every visible tile to land at the same cell used by the logic.
        // This also rejects unsupported rotated/scaled parent transforms safely.
        for (int i = 0; i < block.Tiles.Length; i++)
        {
            Vector3 final = rootPosition + block.transform.TransformVector(block.Tiles[i].transform.localPosition);
            Vector2Int cell = origin + block.TileCells[i];
            if (Mathf.Abs(final.x - cell.x) > 0.01f || Mathf.Abs(final.y - cell.y) > 0.01f) return false;
        }
        return true;
    }

    public bool TryPlace(Block block, out Vector2Int origin, out Vector3 rootPosition)
    {
        if (!TryGetPlacement(block, out origin, out rootPosition)) return false;
        // Validate the whole shape first, then commit it as one placement.
        for (int i = 0; i < block.Tiles.Length; i++)
        {
            Vector2Int cell = origin + block.TileCells[i];
            boardBlocks[cell.x, cell.y] = block.Tiles[i];
        }
        return true;
    }

    public static int Rand(int min, int max)
    {
        // Integer Range excludes max; float rounding could previously select max.
        return UnityEngine.Random.Range(min, max);
    }

    public BlockTile SpawnBlockTile(int x, int y)
    {
        boardBlocks[x, y] = Instantiate(blockTilePrefab, boardTransform).GetComponent<BlockTile>();
        Vector3 pos = new Vector3(x, y, -1);
        boardBlocks[x, y].transform.position = pos;
        boardBlocks[x, y].transform.localScale = boardTileScale;
        return boardBlocks[x, y];
    }

    private bool IsValidBlockPrefab(int index)
    {
        return blockPrefabs != null && index >= 0 && index < blockPrefabs.Length
            && blockPrefabs[index] != null && blockPrefabs[index].GetComponent<Block>() != null;
    }

    public int GetRandomBlockPrefabIndex()
    {
        int selected = -1;
        int validCount = 0;
        if (blockPrefabs == null) return selected;
        for (int index = 0; index < blockPrefabs.Length; index++)
        {
            if (!IsValidBlockPrefab(index)) continue;
            validCount++;
            if (UnityEngine.Random.Range(0, validCount) == 0) selected = index;
        }
        return selected;
    }

    public Block SpawnBlock(int i, int x)
    {
        if (i < 0 || i >= BLOCKS_AMOUNT)
        {
            Debug.LogError("Cannot spawn a tray block: slot index must be 0, 1 or 2.", this);
            return null;
        }

        // A saved prefab index can become invalid after editing the prefab list.
        if (!IsValidBlockPrefab(x)) x = GetRandomBlockPrefabIndex();
        if (x < 0)
        {
            Debug.LogError("BoardManager needs at least one assigned shape prefab with a Block component.", this);
            return blocks[i];
        }

        // Each tray slot owns exactly one piece. Hide the previous one immediately;
        // Destroy alone waits until the end of the frame and can leave it visible.
        Block previous = blocks[i];
        blocks[i] = null;
        if (previous != null)
        {
            previous.gameObject.SetActive(false);
            Destroy(previous.gameObject);
        }

        Block b = Instantiate(blockPrefabs[x], gameTransform).GetComponent<Block>();
        b.prefabIndex = x;
        b.SetBasePosition(i);
        blocks[i] = b;
        return b;
    }

    public int GetEmptyFieldsAmount()
    {
        int x = 0;
        foreach (BlockTile b in boardBlocks)
            if (!b) x++;
        return x;
    }

    public void MoveBlocks(int i)
    {
        blocks[i] = null;
        if (tutorialMode) return;

        bool isTrayEmpty = true;
        for (int j = 0; j < BLOCKS_AMOUNT; j++)
        {
            if (blocks[j] != null)
            {
                isTrayEmpty = false;
                break;
            }
        }

        if (isTrayEmpty)
        {
            for (int j = 0; j < BLOCKS_AMOUNT; j++)
            {
                SpawnBlock(j, GetRandomBlockPrefabIndex());
            }
        }
    }

    public void CheckSpace(bool oa)
    {
        int blockedCount = 0;
        int activeBlocks = 0;

        for (int i = 0; i < BLOCKS_AMOUNT; i++)
        {
            if (blocks[i] == null)
                continue;

            activeBlocks++;

            if (CheckBlock(i))
            {
                blocks[i].movable = true;
                Color c = blocks[i].GetColor();
                c.a = 1;
                blocks[i].ChangeColor(c);
            }
            else
            {
                blocks[i].movable = false;
                blockedCount++;
                Color c = blocks[i].GetColor();
                c.a = 0.5f;
                blocks[i].ChangeColor(c);
            }
        }

        if (gameOverUI != null)
    gameOverUI.OnSpaceChecked(activeBlocks, blockedCount);
    }

    public void CheckBoard(bool onAwake = false)
    {
        if (DestroyManager.ins.IsClearing) return;
        DestroyManager.ins.SetDestroy();

        for (int x = 0; x < BOARD_SIZE; x++)
            CheckVLine(x);
        for (int y = 0; y < BOARD_SIZE; y++)
            CheckHLine(y);

        if (DestroyManager.ins.destroyedLines > 0)
        {
            // DestroyManager synchronizes the flare, tile burst and camera shake.
            StartCoroutine(DestroyManager.ins.DestroyAllBlocks(!onAwake));
        }
        else
            CheckSpace(onAwake);
    }

    private readonly HashSet<BlockTile> previewedBlocks = new HashSet<BlockTile>();
    private Sprite previewSprite;

    public void ClearBlockHighlights()
    {
        foreach (BlockTile tile in previewedBlocks)
            if (tile != null)
                tile.ClearPreview();

        previewedBlocks.Clear();
        previewSprite = null;
    }

    private void PreviewBlock(BlockTile tile)
    {
        // A row/column intersection should animate only once.
        if (tile != null && previewedBlocks.Add(tile))
            tile.ShowPreview(0.2f, previewSprite);
    }

    public void HighlightBlocks()
    {
        ClearBlockHighlights();
        Block block = InputManager.ins != null ? InputManager.ins.draggedBlock : null;
        Vector2Int origin;
        Vector3 target;
        if (!TryGetPlacement(block, out origin, out target)) return;
        BlockTile[,] preview = (BlockTile[,])boardBlocks.Clone();
        bool[] rows = new bool[BOARD_SIZE];
        bool[] columns = new bool[BOARD_SIZE];
        foreach (BlockTile tile in block.Tiles)
        {
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) { previewSprite = renderer.sprite; break; }
        }
        if (previewSprite == null) return;
        for (int i = 0; i < block.Tiles.Length; i++)
        {
            Vector2Int cell = origin + block.TileCells[i];
            preview[cell.x, cell.y] = block.Tiles[i];
            rows[cell.y] = true;
            columns[cell.x] = true;
        }
        for (int index = 0; index < BOARD_SIZE; index++)
        {
            if (rows[index])
            {
                bool full = true;
                for (int x = 0; x < BOARD_SIZE; x++) if (preview[x, index] == null) full = false;
                if (full) for (int x = 0; x < BOARD_SIZE; x++) PreviewBlock(preview[x, index]);
            }
            if (columns[index])
            {
                bool full = true;
                for (int y = 0; y < BOARD_SIZE; y++) if (preview[index, y] == null) full = false;
                if (full) for (int y = 0; y < BOARD_SIZE; y++) PreviewBlock(preview[index, y]);
            }
        }
    }

    private void Awake()
    {
        if (ins != null && ins != this)
        {
            enabled = false;
            return;
        }
        ins = this;

        boardTileScale = GameScaler.GetBoardTileScale();
        scaledBlockTileScale = GameScaler.GetScaledBlockTileScale();

        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 scale = GameScaler.GetBoardTileScale();

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                GameObject prefabToUse = ((x + y) % 2 == 0) ? boardTilePrefabA : boardTilePrefabB;
                Transform t = Instantiate(prefabToUse, boardTransform).transform;
                
                t.position = new Vector3(x, y, 0);
                t.localScale = scale;
                boardTiles[x, y] = t.GetComponent<SpriteRenderer>();
            }
        }

        if (tutorialMode)
        {
            CreateTutorialLayout();
            return;
        }

        for (int i = 0; i < BLOCKS_AMOUNT; i++)
        {
            SpawnBlock(i, GetRandomBlockPrefabIndex());
        }
    }

    private void CreateTutorialLayout()
    {
        // Validate references before creating any playable tutorial tiles.
        if (tutorialBlueSprite == null || tutorialPurpleSprite == null ||
            blockTilePrefab == null || blockTilePrefab.GetComponent<BlockTile>() == null ||
            blockTilePrefab.GetComponent<SpriteRenderer>() == null ||
            !IsValidBlockPrefab(tutorialSquarePrefabIndex))
        {
            Debug.LogError("Tutorial setup: assign both sprites, a Block Tile prefab with BlockTile and SpriteRenderer, and a valid square prefab index.", this);
            return;
        }

        tutorialPiece = SpawnBlock(1, tutorialSquarePrefabIndex);
        if (tutorialPiece == null) return;
        bool square = tutorialPiece.HasValidLayout && tutorialPiece.TileCells != null &&
            tutorialPiece.TileCells.Length == 4;
        var cells = new HashSet<Vector2Int>();
        if (square)
        {
            foreach (Vector2Int cell in tutorialPiece.TileCells)
                square &= cell.x >= 0 && cell.x <= 1 && cell.y >= 0 && cell.y <= 1 && cells.Add(cell);
        }
        if (!square)
        {
            Debug.LogError("Tutorial Square Prefab Index must point to a valid 2 by 2 square with four real tiles.", this);
            tutorialPiece.gameObject.SetActive(false);
            Destroy(tutorialPiece.gameObject);
            tutorialPiece = null;
            blocks[1] = null;
            return;
        }

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                bool middleColumn = x == 3 || x == 4;
                bool middleRow = y == 3 || y == 4;
                // Exclusive OR leaves the center 2 by 2 and all corners empty.
                if (middleColumn == middleRow) continue;
                BlockTile tile = SpawnBlockTile(x, y);
                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                renderer.sprite = middleColumn ? tutorialBlueSprite : tutorialPurpleSprite;
                renderer.color = Color.white;
                tile.defaultColor = Color.white;
            }
        }
        CheckSpace(true);
    }

    private void CheckHLine(int y)
    {
        if (y < 0 || y >= BOARD_SIZE) return;
        for (int x = 0; x < BOARD_SIZE; x++) if (boardBlocks[x, y] == null) return;
        DestroyManager.ins.PrepareToDestroy(y, false);
    }

    private void CheckVLine(int x)
    {
        if (x < 0 || x >= BOARD_SIZE) return;
        for (int y = 0; y < BOARD_SIZE; y++) if (boardBlocks[x, y] == null) return;
        DestroyManager.ins.PrepareToDestroy(x, true);
    }

    private bool CheckBlock(int i)
    {
        for (int y = 0; y < BOARD_SIZE; y++)
            for (int x = 0; x < BOARD_SIZE; x++)
                if (CanPlace(blocks[i], new Vector2Int(x, y))) return true;
        return false;
    }
}
