using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BlockPlacementShine : MonoBehaviour
{
    [Min(0.05f)] public float shineDuration = 0.3f;
    [Range(0f, 1f)] public float shineOpacity = 0.85f;
    public Color shineColor = Color.white;

    [Header("Around the placed shape")]
    public bool showPlacementOutline = true;
    public Color outlineColor = new Color(0.45f, 0.92f, 1f, 1f);
    [Min(0.05f)] public float outlineDuration = 0.38f;
    [Range(0.02f, 0.4f)] public float outlineExpansion = 0.18f;
    [Range(0, 16)] public int outwardSparkles = 8;

    private class OutlineEdge
    {
        public SpriteRenderer glow, core;
        public Vector3 center, normal;
        public bool vertical;
    }

    private class OuterSparkle
    {
        public SpriteRenderer horizontal, vertical;
        public Vector3 start, end;
    }

    private Sprite whiteSprite;
    private readonly List<GameObject> liveVisuals = new List<GameObject>();

    private class TileShine
    {
        public SpriteRenderer source, sweep, sparkleA, sparkleB;
        public Bounds bounds;
    }

    public void Play(Block block)
    {
        if (!isActiveAndEnabled || block == null) return;
        // Snapshot only real tiles. Fake anchors never receive a shine.
        List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        foreach (Transform child in block.transform)
        {
            if (child.name.Trim() != "Block tile") continue;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) tiles.Add(renderer);
        }
        StartCoroutine(Shine(block, tiles));
    }

    private SpriteRenderer CreateVisual(SpriteRenderer source, string name)
    {
        if (whiteSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width);
        }
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(source.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = whiteSprite;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + 2;
        renderer.color = Color.clear;
        liveVisuals.Add(visual);
        return renderer;
    }

    private IEnumerator Shine(Block block, List<SpriteRenderer> sources)
    {
        // Start on landing, not while the shape is still traveling to its cell.
        while (block != null && block.IsMoving()) yield return null;
        if (block == null) yield break;

        if (showPlacementOutline) StartCoroutine(PlacementOutline(sources));

        List<TileShine> tiles = new List<TileShine>();
        foreach (SpriteRenderer source in sources)
        {
            if (source == null || source.sprite == null) continue;
            Bounds bounds = source.sprite.bounds;
            Vector3 center = bounds.center;
            if (source.flipX) center.x = -center.x;
            if (source.flipY) center.y = -center.y;
            bounds.center = center;
            TileShine tile = new TileShine { source = source, bounds = bounds,
                sweep = CreateVisual(source, "Placement highlight"),
                sparkleA = CreateVisual(source, "Placement sparkle A"),
                sparkleB = CreateVisual(source, "Placement sparkle B") };
            tiles.Add(tile);
        }

        float duration = Mathf.Max(0.05f, shineDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Sin(t * Mathf.PI) * shineOpacity * shineColor.a;
            float sparkle = Mathf.Sin(Mathf.Clamp01((t - 0.2f) / 0.8f) * Mathf.PI);
            foreach (TileShine tile in tiles)
            {
                if (tile.source == null || tile.sweep == null) continue;
                Vector3 size = tile.bounds.size;
                Vector3 center = tile.bounds.center;
                // Keep the streak inset from the edges of the glossy square artwork.
                tile.sweep.transform.localPosition = center + new Vector3(0f, Mathf.Lerp(-0.25f, 0.25f, t) * size.y, -0.01f);
                tile.sweep.transform.localScale = new Vector3(size.x * 0.58f, size.y * 0.045f, 1f);
                tile.sweep.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                tile.sweep.color = new Color(shineColor.r, shineColor.g, shineColor.b, alpha);

                if (tile.sparkleA != null && tile.sparkleB != null)
                {
                    Vector3 position = center + new Vector3(size.x * 0.25f, size.y * 0.25f, -0.01f);
                    tile.sparkleA.transform.localPosition = tile.sparkleB.transform.localPosition = position;
                    float length = Mathf.Min(size.x, size.y) * 0.26f * sparkle;
                    tile.sparkleA.transform.localScale = new Vector3(length, length * 0.15f, 1f);
                    tile.sparkleB.transform.localScale = new Vector3(length * 0.15f, length, 1f);
                    tile.sparkleA.transform.localRotation = tile.sparkleB.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    Color color = new Color(shineColor.r, shineColor.g, shineColor.b, alpha * sparkle);
                    tile.sparkleA.color = tile.sparkleB.color = color;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (TileShine tile in tiles)
        {
            RemoveVisual(tile.sweep);
            RemoveVisual(tile.sparkleA);
            RemoveVisual(tile.sparkleB);
        }
        // Cleared tiles may already have destroyed their child effects.
        liveVisuals.RemoveAll(item => item == null);
    }

    private SpriteRenderer CreateOuterVisual(SpriteRenderer source, string name)
    {
        SpriteRenderer visual = CreateVisual(source, name);
        // Keep the burst stable if a completed line immediately shrinks these tiles.
        visual.transform.SetParent(null, false);
        return visual;
    }

    private IEnumerator PlacementOutline(List<SpriteRenderer> sources)
    {
        Dictionary<Vector2Int, SpriteRenderer> cells = new Dictionary<Vector2Int, SpriteRenderer>();
        foreach (SpriteRenderer source in sources)
        {
            if (source == null) continue;
            Vector3 p = source.transform.position;
            cells[new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y))] = source;
        }

        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        List<OutlineEdge> edges = new List<OutlineEdge>();
        foreach (KeyValuePair<Vector2Int, SpriteRenderer> cell in cells)
        {
            foreach (Vector2Int direction in directions)
            {
                // Draw the silhouette, omitting shared edges inside the shape.
                if (cells.ContainsKey(cell.Key + direction)) continue;
                Vector3 normal = new Vector3(direction.x, direction.y, 0f);
                Vector3 center = new Vector3(cell.Key.x, cell.Key.y, cell.Value.transform.position.z - 0.02f);
                edges.Add(new OutlineEdge { center = center + normal * 0.49f,
                    normal = normal, vertical = direction.x != 0,
                    glow = CreateOuterVisual(cell.Value, "Placement outline glow"),
                    core = CreateOuterVisual(cell.Value, "Placement outline core") });
            }
        }

        if (edges.Count == 0) yield break;
        List<OuterSparkle> sparkles = new List<OuterSparkle>();
        SpriteRenderer reference = null;
        foreach (SpriteRenderer source in sources)
            if (source != null) { reference = source; break; }

        int count = Mathf.Clamp(outwardSparkles, 0, 16);
        for (int i = 0; i < count; i++)
        {
            OutlineEdge edge = edges[(i * edges.Count) / count];
            Vector3 tangent = new Vector3(-edge.normal.y, edge.normal.x, 0f);
            Vector3 start = edge.center + tangent * Random.Range(-0.3f, 0.3f);
            sparkles.Add(new OuterSparkle { start = start,
                end = start + edge.normal * Random.Range(0.2f, 0.45f) + tangent * Random.Range(-0.12f, 0.12f),
                horizontal = CreateOuterVisual(reference, "Outer placement sparkle A"),
                vertical = CreateOuterVisual(reference, "Outer placement sparkle B") });
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, outlineDuration);
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float fade = Mathf.Clamp01(t / 0.08f) * (1f - t) * outlineColor.a;
            foreach (OutlineEdge edge in edges)
            {
                Vector3 position = edge.center + edge.normal * outlineExpansion * ease;
                float length = 1.02f + 2f * outlineExpansion * ease;
                edge.glow.transform.position = edge.core.transform.position = position;
                edge.glow.transform.rotation = edge.core.transform.rotation = Quaternion.identity;
                float thickness = Mathf.Lerp(0.1f, 0.015f, ease);
                edge.glow.transform.localScale = edge.vertical ? new Vector3(thickness, length, 1f) : new Vector3(length, thickness, 1f);
                edge.core.transform.localScale = edge.vertical ? new Vector3(thickness * 0.25f, length, 1f) : new Vector3(length, thickness * 0.25f, 1f);
                edge.glow.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, fade * 0.4f);
                edge.core.color = new Color(1f, 1f, 1f, fade * 0.85f);
            }
            foreach (OuterSparkle sparkle in sparkles)
            {
                Vector3 position = Vector3.Lerp(sparkle.start, sparkle.end, ease);
                sparkle.horizontal.transform.position = sparkle.vertical.transform.position = position;
                sparkle.horizontal.transform.rotation = sparkle.vertical.transform.rotation = Quaternion.Euler(0f, 0f, 20f + 40f * t);
                float size = Mathf.Sin(t * Mathf.PI) * 0.16f;
                sparkle.horizontal.transform.localScale = new Vector3(size, size * 0.15f, 1f);
                sparkle.vertical.transform.localScale = new Vector3(size * 0.15f, size, 1f);
                Color color = Color.Lerp(outlineColor, Color.white, 0.65f);
                color.a = fade;
                sparkle.horizontal.color = sparkle.vertical.color = color;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (OutlineEdge edge in edges) { RemoveVisual(edge.glow); RemoveVisual(edge.core); }
        foreach (OuterSparkle sparkle in sparkles) { RemoveVisual(sparkle.horizontal); RemoveVisual(sparkle.vertical); }
    }

    private void RemoveVisual(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        liveVisuals.Remove(renderer.gameObject);
        Destroy(renderer.gameObject);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (GameObject visual in liveVisuals)
            if (visual != null) Destroy(visual);
        liveVisuals.Clear();
    }

    private void OnDestroy()
    {
        if (whiteSprite != null) Destroy(whiteSprite);
    }
}
