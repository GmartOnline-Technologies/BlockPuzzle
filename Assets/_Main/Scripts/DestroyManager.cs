using UnityEngine;
using System.Collections;

public class DestroyManager : MonoBehaviour
{
    public static DestroyManager ins;

    [HideInInspector]
    public int destroyedLines;
    [HideInInspector]
    private BlockDestroyAnimation[,] blocksAnimations = new BlockDestroyAnimation[BoardManager.BOARD_SIZE, BoardManager.BOARD_SIZE];

    private BoardManager bm;
    private Vector2Int[] desLinesPos = new Vector2Int[BoardManager.BOARD_SIZE];

    public void SetDestroy()
    {
        destroyedLines = 0;
        for (int i = 0; i < BoardManager.BOARD_SIZE; i++)
            desLinesPos[i] = new Vector2Int(-1, -1);
    }

    public void PrepareToDestroy(int i, bool v)
{
    destroyedLines++;

    for (int j = 0; j < BoardManager.BOARD_SIZE; j++)
    {
        if (desLinesPos[j] == new Vector2Int(-1, -1))
        {
            desLinesPos[j] = v ? new Vector2Int(i, -1) : new Vector2Int(-1, i);
            break;
        }
    }
}

public IEnumerator DestroyAllBlocks()
{
    // Instantly destroy the GameObjects in the completed lines
    for (int i = 0; i < BoardManager.BOARD_SIZE; i++)
    {
        for (int j = 0; j < BoardManager.BOARD_SIZE; j++)
        {
            if (desLinesPos[j] == new Vector2Int(-1, -1))
                break;

            int y = BoardManager.BOARD_SIZE - i - 1;
            Vector2Int p = desLinesPos[j];
            
            if (p.x != -1 && bm.boardBlocks[p.x, y])
            {
                Destroy(bm.boardBlocks[p.x, y].gameObject);
                bm.boardBlocks[p.x, y] = null;
            }
            else if (p.y != -1 && bm.boardBlocks[i, p.y])
            {
                Destroy(bm.boardBlocks[i, p.y].gameObject);
                bm.boardBlocks[i, p.y] = null;
            }
        }
    }

    // Check if the remaining blocks can fit on the board
    BoardManager.ins.CheckSpace(false);
    yield return null;
}

    public void DestroyBlocks()
    {
        int a = (int)Random.Range(0, BoardManager.BOARD_SIZE - 2.001f);
        if (BoardManager.ins.blocks[0].size.x >= BoardManager.ins.blocks[0].size.y)
        {
            for (int y = a; y < a + 3; y++)
            {
                for (int x = 0; x < BoardManager.BOARD_SIZE; x++)
                {
                    BlockTile b = BoardManager.ins.boardBlocks[x, y];
                    if (b)
                        b.Destroy(0.25f);

                    BoardManager.ins.boardBlocks[x, y] = null;
                }
            }
        }
        else
        {
            for (int x = a; x < a + 3; x++)
            {
                for (int y = 0; y < BoardManager.BOARD_SIZE; y++)
                {
                    BlockTile b = BoardManager.ins.boardBlocks[x, y];
                    if (b)
                        b.Destroy(0.25f);

                    BoardManager.ins.boardBlocks[x, y] = null;
                }
            }
        }

        BoardManager.ins.CheckBoard();
    }

    private void Awake()
	{
        if (!ins)
            ins = this;

        bm = GetComponent<BoardManager>();
	}
}
