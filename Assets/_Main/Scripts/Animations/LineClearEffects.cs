using UnityEngine;
using System.Collections.Generic;

// Generated visuals reuse the existing block sprites. No particle prefab or TMP required.
[DisallowMultipleComponent]
public class LineClearEffects : MonoBehaviour
{
    [Range(32, 256)] public int maxVisuals = 144;
    public Color beamColor = new Color(0.35f, 0.9f, 1f, 0.65f);
    public bool showComboText = true;

    private class Visual
    {
        public SpriteRenderer renderer;
        public Vector3 start, end, scaleFrom, scaleTo;
        public Color color;
        public float age, life, rotation, spin;
    }

    private readonly List<Visual> active = new List<Visual>();
    private readonly Stack<Visual> pool = new Stack<Visual>();
    private Sprite whiteSprite;
    private Transform visualRoot;
    private string comboText;
    private float comboAge = 1f;
    private Vector3 comboPosition;
    private GUIStyle comboStyle;
    private const float ComboLife = 0.65f;

    private Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }
            return whiteSprite;
        }
    }

    private void Spawn(Sprite sprite, SpriteRenderer source, Vector3 from,
        Vector3 to, Vector3 scaleFrom, Vector3 scaleTo, Color color,
        float life, float rotation = 0f, float spin = 0f)
    {
        if (sprite == null || source == null || active.Count >= maxVisuals) return;
        if (visualRoot == null)
            visualRoot = new GameObject("Line clear visuals").transform;

        Visual v;
        if (pool.Count > 0) v = pool.Pop();
        else
        {
            GameObject go = new GameObject("Clear spark");
            go.transform.SetParent(visualRoot, false);
            v = new Visual { renderer = go.AddComponent<SpriteRenderer>() };
        }

        v.renderer.gameObject.SetActive(true);
        v.renderer.sprite = sprite;
        v.renderer.sortingLayerID = source.sortingLayerID;
        v.renderer.sortingOrder = source.sortingOrder + 10;
        v.start = from; v.end = to;
        v.scaleFrom = scaleFrom; v.scaleTo = scaleTo;
        v.color = color; v.age = 0f; v.life = Mathf.Max(0.01f, life);
        v.rotation = rotation; v.spin = spin;
        v.renderer.color = color;
        v.renderer.transform.position = from;
        v.renderer.transform.localScale = scaleFrom;
        v.renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        active.Add(v);
    }

    public void Charge(SpriteRenderer source, float duration)
    {
        if (duration <= 0f) return;
        Vector3 size = source.bounds.size;
        size = new Vector3(size.x * 0.82f, size.y * 0.82f, 1f);
        Spawn(WhiteSprite, source, source.bounds.center, source.bounds.center,
            size, size * 1.12f, new Color(1f, 1f, 1f, 0.2f), duration);
    }

    public void Burst(SpriteRenderer source, int count)
    {
        if (source.sprite == null) return;
        Vector3 center = source.bounds.center;
        float cellSize = Mathf.Max(0.01f, Mathf.Min(source.bounds.size.x, source.bounds.size.y));
        for (int i = 0; i < count; i++)
        {
            // Alternate miniature colored artwork and small white glints.
            bool glint = i % 3 == 2;
            Sprite sprite = glint ? WhiteSprite : source.sprite;
            float width = cellSize * (glint ? 0.065f : Random.Range(0.11f, 0.18f));
            Vector3 scale = new Vector3(width / sprite.bounds.size.x,
                width / sprite.bounds.size.y, 1f);
            float angle = (i / (float)Mathf.Max(1, count)) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Vector3 end = center + direction * cellSize * Random.Range(0.35f, 0.7f);
            Spawn(sprite, source, center, end, scale, scale * 0.15f,
                Color.white, Random.Range(0.22f, 0.32f), glint ? 45f : 0f,
                Random.Range(-120f, 120f));
        }
    }

    public void Line(BoardManager board, int index, bool vertical, float duration)
    {
        int n = BoardManager.BOARD_SIZE;
        SpriteRenderer first = vertical ? board.boardTiles[index, n - 1] : board.boardTiles[0, index];
        SpriteRenderer last = vertical ? board.boardTiles[index, 0] : board.boardTiles[n - 1, index];
        if (first == null || last == null) return;

        // Match the block sorting layer, which may differ from the board background.
        BlockTile firstBlock = vertical ? board.boardBlocks[index, n - 1] : board.boardBlocks[0, index];
        SpriteRenderer sortingSource = firstBlock != null ? firstBlock.GetComponent<SpriteRenderer>() : first;
        if (sortingSource == null) sortingSource = first;

        Vector3 from = first.bounds.center;
        Vector3 to = last.bounds.center;
        from.z = to.z = -1.5f;
        float cell = Mathf.Min(first.bounds.size.x, first.bounds.size.y);
        float length = Vector3.Distance(from, to) + cell;
        Vector3 full = vertical ? new Vector3(cell * 0.35f, length, 1f)
            : new Vector3(length, cell * 0.35f, 1f);
        Vector3 thin = vertical ? new Vector3(0f, length, 1f) : new Vector3(length, 0f, 1f);
        Vector3 center = (from + to) * 0.5f;
        Spawn(WhiteSprite, sortingSource, center, center, full, thin, beamColor, duration);

        // A moving white core follows the same direction as the tile ripple.
        Vector3 core = vertical ? new Vector3(cell * 0.65f, cell * 0.16f, 1f)
            : new Vector3(cell * 0.16f, cell * 0.65f, 1f);
        Spawn(WhiteSprite, sortingSource, from, to, core, core * 0.5f,
            new Color(1f, 1f, 1f, 0.9f), duration);
    }

    public void Combo(int lines, BoardManager board)
    {
        if (!showComboText || lines < 2) return;
        comboText = lines == 2 ? "DOUBLE!" : "AMAZING!";
        comboAge = 0f;
        comboPosition = (board.boardTiles[0, BoardManager.BOARD_SIZE - 1].bounds.center
            + board.boardTiles[BoardManager.BOARD_SIZE - 1, BoardManager.BOARD_SIZE - 1].bounds.center) * 0.5f;
    }

    private void Update()
    {
        comboAge += Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Visual v = active[i];
            v.age += Time.deltaTime;
            float t = Mathf.Clamp01(v.age / v.life);
            v.renderer.transform.position = Vector3.Lerp(v.start, v.end, t);
            v.renderer.transform.localScale = Vector3.Lerp(v.scaleFrom, v.scaleTo, t);
            v.renderer.transform.rotation = Quaternion.Euler(0f, 0f, v.rotation + v.spin * t);
            v.renderer.color = new Color(v.color.r, v.color.g, v.color.b, v.color.a * (1f - t));
            if (t >= 1f)
            {
                v.renderer.gameObject.SetActive(false);
                active.RemoveAt(i);
                pool.Push(v);
            }
        }
    }

    // Built-in GUI avoids requiring a Canvas, a font asset, or TextMesh Pro setup.
    private void OnGUI()
    {
        if (!showComboText || comboAge >= ComboLife || string.IsNullOrEmpty(comboText)) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 screen = camera.WorldToScreenPoint(comboPosition);
        if (screen.z <= 0f) return;
        if (comboStyle == null)
            comboStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

        float t = comboAge / ComboLife;
        float size = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.045f, 18f, 48f);
        comboStyle.fontSize = Mathf.RoundToInt(size * (1f + 0.18f * Mathf.Sin(Mathf.Clamp01(t * 4f) * Mathf.PI)));
        float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.55f));
        float width = Mathf.Min(Screen.width, size * 9f);
        float x = Mathf.Clamp(screen.x - width * 0.5f, 0f, Screen.width - width);
        float y = Mathf.Clamp(Screen.height - screen.y - size * 1.6f - t * size * 0.6f, 0f, Screen.height - size * 2f);
        Rect rect = new Rect(x, y, width, size * 2f);
        Color savedColor = GUI.color;
        GUI.color = Color.white;
        comboStyle.normal.textColor = new Color(0.06f, 0.08f, 0.17f, alpha);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), comboText, comboStyle);
        comboStyle.normal.textColor = new Color(1f, 0.87f, 0.3f, alpha);
        GUI.Label(rect, comboText, comboStyle);
        GUI.color = savedColor;
    }

    private void OnDisable()
    {
        foreach (Visual v in active)
        {
            if (v.renderer != null) v.renderer.gameObject.SetActive(false);
            pool.Push(v);
        }
        active.Clear();
        comboAge = ComboLife;
    }

    private void OnDestroy()
    {
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        if (whiteSprite != null) Destroy(whiteSprite);
    }
}