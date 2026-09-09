using UnityEngine;
using System;
using System.Collections;

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
        return o.x >= -0.5f && e.x <= BOARD_SIZE - 0.5f &&
               o.y >= -0.5f && e.y <= BOARD_SIZE - 0.5f;
    }

    public bool IsEmpty(Block b, Vector2 o)
    {
        for (int i = 0; i < b.structure.Length; i++)
        {
            if (b.transform.GetChild(i).name == "Block tile")
            {
                Vector2Int coords = b.structure[i];
                if (boardBlocks[(int)o.x + coords.x, (int)o.y + coords.y])
                    return false;
            }
        }
        return true;
    }

    public static int Rand(int min, int max)
    {
        return (int)UnityEngine.Random.Range(min, max - 0.000001f);
    }

    public BlockTile SpawnBlockTile(int x, int y)
    {
        boardBlocks[x, y] = Instantiate(blockTilePrefab, boardTransform).GetComponent<BlockTile>();
        Vector3 pos = new Vector3(x, y, -1);
        boardBlocks[x, y].transform.position = pos;
        boardBlocks[x, y].transform.localScale = boardTileScale;
        return boardBlocks[x, y];
    }

    public Block SpawnBlock(int i, int x)
    {
        Block b = Instantiate(blockPrefabs[x], gameTransform).GetComponent<Block>();
        b.SetBasePosition(i);
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
                int randomIndex = Rand(0, blockPrefabs.Length);
                blocks[j] = SpawnBlock(j, randomIndex);
                blocks[j].SetBasePosition(j, true);
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
    }

    public void CheckBoard(bool onAwake = false)
    {
        DestroyManager.ins.SetDestroy();

        for (int x = 0; x < BOARD_SIZE; x++)
            CheckVLine(x);
        for (int y = 0; y < BOARD_SIZE; y++)
            CheckHLine(y);

        if (DestroyManager.ins.destroyedLines > 0)
            StartCoroutine(DestroyManager.ins.DestroyAllBlocks());
        else
            CheckSpace(onAwake);
    }

    public void HighlightBlocks()
    {
        Block db = InputManager.ins.draggedBlock;
        Vector2Int c = db.GetFirstCoords();

        for (int x = c.x; x < c.x + db.size.x; x++)
            CheckVLine(x, true);
        for (int y = c.y; y < c.y + db.size.y; y++)
            CheckHLine(y, true);
    }

    private void Awake()
    {
        if (!ins)
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

        for (int i = 0; i < BLOCKS_AMOUNT; i++)
        {
            int randomIndex = Rand(0, blockPrefabs.Length);
            blocks[i] = SpawnBlock(i, randomIndex);
        }
    }

    private void CheckHLine(int y, bool h = false)
    {
        if (h)
        {
            BlockTile[,] b = new BlockTile[BOARD_SIZE, BOARD_SIZE];
            Array.Copy(boardBlocks, b, boardBlocks.Length);

            Block db = InputManager.ins.draggedBlock;
            Vector2Int c = db.GetFirstCoords();
            for (int i = 0; i < db.structure.Length; i++)
            {
                if (db.transform.GetChild(i).name == "Block tile")
                {
                    BlockTile bt = db.transform.GetChild(i).GetComponent<BlockTile>();
                    b[c.x + db.structure[i].x, c.y + db.structure[i].y] = bt;
                }
            }
            
            for (int x = 0; x < BOARD_SIZE; x++)
                if (!b[x, y]) return;
            
            // Using b[x, y] instead of boardBlocks[x, y] ensures the dragged piece's tiles also fade/swap sprites
            for (int x = 0; x < BOARD_SIZE; x++)
                if (b[x, y])
                    b[x, y].Fade(0.2f, db.defaultColor);
        }
        else
        {
            for (int x = 0; x < BOARD_SIZE; x++)
                if (!boardBlocks[x, y]) return;

            DestroyManager.ins.PrepareToDestroy(y, false);
        }
    }

    private void CheckVLine(int x, bool h = false)
    {
        if (h)
        {
            BlockTile[,] b = new BlockTile[BOARD_SIZE, BOARD_SIZE];
            Array.Copy(boardBlocks, b, boardBlocks.Length);

            Block db = InputManager.ins.draggedBlock;
            Vector2Int c = db.GetFirstCoords();
            for (int i = 0; i < db.structure.Length; i++)
            {
                if (db.transform.GetChild(i).name == "Block tile")
                {
                    BlockTile bt = db.transform.GetChild(i).GetComponent<BlockTile>();
                    b[c.x + db.structure[i].x, c.y + db.structure[i].y] = bt;
                }
            }

            for (int y = 0; y < BOARD_SIZE; y++)
                if (!b[x, y]) return;

            // Using b[x, y] instead of boardBlocks[x, y] ensures the dragged piece's tiles also fade/swap sprites
            for (int y = 0; y < BOARD_SIZE; y++)
                if (b[x, y])
                    b[x, y].Fade(0.2f, db.defaultColor);
        }
        else
        {
            for (int y = 0; y < BOARD_SIZE; y++)
                if (!boardBlocks[x, y]) return;

            DestroyManager.ins.PrepareToDestroy(x, true);
        }
    }

    private bool CheckBlock(int i)
    {
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                Vector2 size = new Vector2(blocks[i].size.x - 1, blocks[i].size.y - 1);
                Vector2 origin = new Vector2(x, y);
                Vector2 end = origin + size;

                if (IsInRange(origin, end) && IsEmpty(blocks[i], origin))
                    return true;
            }
        }
        return false;
    }
}