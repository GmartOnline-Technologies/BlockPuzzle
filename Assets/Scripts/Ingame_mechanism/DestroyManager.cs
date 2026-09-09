using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DestroyManager : MonoBehaviour
{
    public static DestroyManager ins;
    [HideInInspector] public int destroyedLines;
    public bool IsClearing { get; private set; }

    [Header("Clear timing (seconds)")]
    [Min(0f)] public float placementSettleTime = 0.08f;
    [Min(0f)] public float chargeTime = 0.09f;
    [Min(0f)] public float rippleStep = 0.018f;
    [Min(0f)] public float shrinkTime = 0.16f;
    [Range(1f, 1.3f)] public float pulseScale = 1.12f;

    [Header("Sparks per tile")]
    [Range(0, 8)] public int oneLineSparks = 3;
    [Range(0, 8)] public int twoLineSparks = 4;
    [Range(0, 8)] public int threePlusSparks = 6;

    private BoardManager bm;
    private LineClearEffects effects;
    // Up to eight rows PLUS eight columns, with no fixed eight-line limit.
    private readonly List<Vector2Int> lines = new List<Vector2Int>();

    private struct ClearTile
    {
        public BlockTile tile;
        public int x, y, step;
    }

    public void SetDestroy()
    {
        if (IsClearing) return;
        destroyedLines = 0;
        lines.Clear();
    }

    public void PrepareToDestroy(int index, bool vertical)
    {
        if (IsClearing || index < 0 || index >= BoardManager.BOARD_SIZE) return;
        Vector2Int line = vertical ? new Vector2Int(index, -1) : new Vector2Int(-1, index);
        if (lines.Contains(line)) return;
        lines.Add(line);
        destroyedLines = lines.Count;
    }

    public IEnumerator DestroyAllBlocks(bool playFeedback = true)
    {
        if (IsClearing || lines.Count == 0) yield break;
        if (bm == null) bm = BoardManager.ins;
        if (bm == null) yield break;
        IsClearing = true;

        // Snapshot both the lines and their tiles before any animation begins.
        Vector2Int[] clearLines = lines.ToArray();
        int count = clearLines.Length;
        int n = BoardManager.BOARD_SIZE;
        int[,] steps = new int[n, n];
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                steps[x, y] = -1;

        foreach (Vector2Int line in clearLines)
        {
            for (int step = 0; step < n; step++)
            {
                int x = line.x >= 0 ? line.x : step;
                int y = line.x >= 0 ? n - 1 - step : line.y;
                // At an intersection, use the first arriving ripple only.
                steps[x, y] = steps[x, y] < 0 ? step : Mathf.Min(steps[x, y], step);
            }
        }

        List<ClearTile> tiles = new List<ClearTile>();
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                if (steps[x, y] >= 0 && bm.boardBlocks[x, y] != null)
                    tiles.Add(new ClearTile { tile = bm.boardBlocks[x, y], x = x, y = y, step = steps[x, y] });

        float settle = Mathf.Max(0f, placementSettleTime);
        float charge = Mathf.Max(0f, chargeTime);
        float ripple = Mathf.Max(0f, rippleStep);
        float shrink = Mathf.Max(0f, shrinkTime);
        float sweep = ripple * (n - 1);
        int sparks = count == 1 ? oneLineSparks : count == 2 ? twoLineSparks : threePlusSparks;

        try
        {
            bm.ClearBlockHighlights();
            // InputManager snaps the newly placed shape over 0.08 seconds.
            if (settle > 0f) yield return new WaitForSeconds(settle);
            foreach (ClearTile entry in tiles)
            {
                if (entry.tile == null) continue;
                BlockDestroyAnimation animation = entry.tile.GetComponent<BlockDestroyAnimation>();
                if (animation == null)
                    animation = entry.tile.gameObject.AddComponent<BlockDestroyAnimation>();
                animation.PlayClear(shrink, entry.step * ripple, charge,
                    pulseScale, playFeedback ? effects : null, sparks);
            }

            if (charge > 0f) yield return new WaitForSeconds(charge);
            if (playFeedback)
            {
                // One shake at burst time, after the charge, for the entire clear.
                CameraShake.ShakeMainCamera(count);
                foreach (Vector2Int line in clearLines)
                    effects.Line(bm, line.x >= 0 ? line.x : line.y,
                        line.x >= 0, Mathf.Max(0.08f, sweep));
                effects.Combo(count, bm);
            }

            yield return new WaitForSeconds(sweep + shrink);
            // Complete cleanup even if a tile's own animation was interrupted.
            foreach (ClearTile entry in tiles)
            {
                if (bm.boardBlocks[entry.x, entry.y] == entry.tile)
                    bm.boardBlocks[entry.x, entry.y] = null;
                if (entry.tile != null) Destroy(entry.tile.gameObject);
            }
        }
        finally
        {
            IsClearing = false;
            lines.Clear();
        }

        bm.CheckSpace(false);
    }

    public void DestroyBlocks()
    {
        if (IsClearing || BoardManager.ins == null) return;
        int a = (int)Random.Range(0, BoardManager.BOARD_SIZE - 2.001f);
        if (BoardManager.ins.blocks[0] == null || BoardManager.ins.blocks[0].size.x >= BoardManager.ins.blocks[0].size.y)
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
        if (ins != null && ins != this)
        {
            enabled = false;
            return;
        }
        ins = this;
        bm = GetComponent<BoardManager>();
        effects = GetComponent<LineClearEffects>();
        if (effects == null) effects = gameObject.AddComponent<LineClearEffects>();
    }
}
