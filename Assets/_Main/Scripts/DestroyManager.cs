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
        // THIS IS THE MISSING LINE! It tells the game to actually fire the beam.
        StartCoroutine(SpawnLineFlare(i, v));

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

    private IEnumerator SpawnLineFlare(int index, bool isVertical)
    {
        // 1. Create a dynamic empty object for the laser
        GameObject flare = new GameObject("LineFlare");
        SpriteRenderer sr = flare.AddComponent<SpriteRenderer>();

        // 2. THE FIX: Generate a pure, blinding white 1x1 texture in code!
        Texture2D tex = Texture2D.whiteTexture;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        
        sr.color = new Color(1f, 1f, 1f, 1f); // Maximum brightness
        sr.sortingOrder = 30; // Render in front of the blocks

        Transform flareTransform = flare.transform;
        float fadeTime = 0.35f; // Slightly faster for a punchy flash
        int boardSize = BoardManager.BOARD_SIZE;

        // 3. Stretch the laser across the board
        Vector3 baseScale = BoardManager.ins.boardTiles[0, 0].transform.localScale;

        if (isVertical)
        {
            Vector3 bottom = BoardManager.ins.boardTiles[index, 0].transform.position;
            Vector3 top = BoardManager.ins.boardTiles[index, boardSize - 1].transform.position;
            flareTransform.position = (bottom + top) / 2f;

            // X (thickness) bulges to 1.5x. Y (length) covers the full board
            flareTransform.localScale = new Vector3(baseScale.x * 1.5f, baseScale.y * boardSize + 0.5f, 1f);
        }
        else
        {
            Vector3 left = BoardManager.ins.boardTiles[0, index].transform.position;
            Vector3 right = BoardManager.ins.boardTiles[boardSize - 1, index].transform.position;
            flareTransform.position = (left + right) / 2f;

            // X (length) covers the full board. Y (thickness) bulges to 1.5x
            flareTransform.localScale = new Vector3(baseScale.x * boardSize + 0.5f, baseScale.y * 1.5f, 1f);
        }

        // 4. Animate it like a collapsing laser
        Vector3 startScale = flareTransform.localScale;
        Vector3 targetScale = startScale;
        
        // Only shrink the thickness, keep the length stretched!
        if (isVertical) targetScale.x = 0f; 
        else targetScale.y = 0f;

        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            float progress = elapsed / fadeTime;
            
            // Creates a fast-out curve for a snappier flash
            float ease = 1f - Mathf.Pow(1f - progress, 3f); 
            
            flareTransform.localScale = Vector3.Lerp(startScale, targetScale, ease);
            sr.color = new Color(1f, 1f, 1f, 1f - ease); 
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(flare);
    }
}
