using System.Collections;
using UnityEngine;

// Keep on the always-active Managers object, beside InputManager.
public class HammerHitEffect : MonoBehaviour
{
    public Sprite hammerSprite;
    [Tooltip("Hammer head contact point in sprite bounds, measured from bottom-left (0..1).")]
    public Vector2 headPoint = new Vector2(0.28f, 0.65f);
    public float hammerSizeInCells = 1.6f;
    public float windupSeconds = 0.16f;
    public float crackHoldSeconds = 0.14f;
    public bool IsPlaying { get; private set; }
    private GameObject visual;
    private Material lineMaterial;
    private BlockTile target;
    private BoardManager board;
    private int cellX, cellY;

    public void Strike(BlockTile tile, int x, int y)
    {
        if (IsPlaying || tile == null) return;
        target = tile; board = BoardManager.ins; cellX = x; cellY = y;
        IsPlaying = true;
        StartCoroutine(Animate());
    }
    private IEnumerator Animate()
    {
        SpriteRenderer tileRenderer = target.GetComponent<SpriteRenderer>();
        Vector3 center = tileRenderer != null ? tileRenderer.bounds.center : target.transform.position;
        Vector3 size = tileRenderer != null ? tileRenderer.bounds.size : Vector3.one;
        float cell = Mathf.Max(0.01f, Mathf.Min(size.x, size.y));
        int layer = tileRenderer != null ? tileRenderer.sortingLayerID : 0;
        int order = tileRenderer != null ? tileRenderer.sortingOrder : 0;
        visual = new GameObject("Hammer Hit Visual");
        visual.transform.position = center + Vector3.back * 0.08f;
        var pivot = new GameObject("Hammer Swing");
        pivot.transform.SetParent(visual.transform, false);
        if (hammerSprite != null)
        {
            var image = new GameObject("Hammer");
            image.transform.SetParent(pivot.transform, false);
            var sr = image.AddComponent<SpriteRenderer>();
            sr.sprite = hammerSprite; sr.sortingLayerID = layer; sr.sortingOrder = order + 5;
            float scale = cell * Mathf.Max(0.1f, hammerSizeInCells) / Mathf.Max(0.01f, hammerSprite.bounds.size.x);
            image.transform.localScale = Vector3.one * scale;
            Vector3 contact = hammerSprite.bounds.min + new Vector3(
                hammerSprite.bounds.size.x * headPoint.x, hammerSprite.bounds.size.y * headPoint.y, 0f);
            image.transform.localPosition = -contact * scale;
        }
        float duration = Mathf.Max(0.01f, windupSeconds), t = 0f;
        while (t < duration)
        {
            if (target == null) { Complete(false); yield break; }
            float f = Mathf.Clamp01(t / duration);
            pivot.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-65f, 0f, f * f));
            pivot.transform.localPosition = Vector3.up * cell * 0.65f * (1f - f);
            t += Time.deltaTime; yield return null;
        }
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localPosition = Vector3.zero;
        BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.Hammer);
        DrawCracks(cell, layer, order + 3);
        yield return new WaitForSeconds(Mathf.Max(0.01f, crackHoldSeconds));
        Complete(true);
    }
    private void DrawCracks(float cell, int layer, int order)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        lineMaterial = new Material(shader);
        // Short branching polylines stay inside the cell; no crack texture required.
        for (int i = 0; i < 9; i++)
        {
            float a = i * Mathf.PI * 2f / 9f;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector3 side = new Vector3(-dir.y, dir.x, 0f);
            Vector3 bend = (dir * 0.24f + side * (i % 2 == 0 ? 0.045f : -0.045f)) * cell;
            Line(new[] { Vector3.zero, dir * cell * 0.11f + side * cell * 0.025f,
                bend, dir * cell * 0.43f }, cell, layer, order);
            Line(new[] { bend, bend + (dir * 0.08f + side * 0.13f) * cell }, cell, layer, order);
        }
    }
    private void Line(Vector3[] points, float cell, int layer, int order)
    {
        var go = new GameObject("Crack"); go.transform.SetParent(visual.transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false; line.sharedMaterial = lineMaterial;
        line.sortingLayerID = layer; line.sortingOrder = order;
        line.startWidth = cell * 0.012f; line.endWidth = cell * 0.004f;
        line.startColor = line.endColor = new Color(0.85f, 0.95f, 1f, 0.95f);
        line.positionCount = points.Length; line.SetPositions(points);
    }
    private void Complete(bool checkSpace)
    {
        if (target != null)
        {
            if (board != null && board.boardBlocks[cellX, cellY] == target)
                board.boardBlocks[cellX, cellY] = null;
            // Use the existing block-break effect, adding its component if needed.
            var animation = target.GetComponent<BlockDestroyAnimation>();
            if (animation == null) animation = target.gameObject.AddComponent<BlockDestroyAnimation>();
            animation.SetAnimation(0.2f);
        }
        target = null;
        if (visual != null) Destroy(visual);
        if (lineMaterial != null) Destroy(lineMaterial);
        visual = null; lineMaterial = null; IsPlaying = false;
        if (checkSpace && board != null) board.CheckSpace(false);
        board = null;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        // Finish an already-paid strike when this component is disabled.
        if (IsPlaying) Complete(false);
    }
}
