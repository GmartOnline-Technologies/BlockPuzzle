using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// Generated visuals reuse the existing block sprites. No particle prefab or TMP required.
[DisallowMultipleComponent]
public class LineClearEffects : MonoBehaviour
{
    [Range(32, 256)] public int maxVisuals = 144;
    public Color beamColor = new Color(0.35f, 0.9f, 1f, 0.65f);
    public bool showComboText = true;

    [Header("Popup artwork")]
    public Sprite goodPopupSprite;
    public Sprite greatPopupSprite;
    [Range(0.2f, 0.8f)] public float popupWidth = 0.52f;
    [Min(0.1f)] public float popupDuration = 0.8f;
    public float popupOffsetY = 12f;

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
    private float ComboLife { get { return Mathf.Max(0.1f, popupDuration); } }
    private Sprite comboSprite;
    private Canvas popupCanvas;
    private CanvasGroup popupGroup;
    private Image popupImage;

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
        comboText = lines == 2 ? "GOOD!" : "GREAT!";
        comboSprite = lines == 2 ? goodPopupSprite : greatPopupSprite;
        comboAge = 0f;
        comboPosition = (board.boardTiles[0, BoardManager.BOARD_SIZE - 1].bounds.center
            + board.boardTiles[BoardManager.BOARD_SIZE - 1, BoardManager.BOARD_SIZE - 1].bounds.center) * 0.5f;
        if (comboSprite != null)
        {
            EnsurePopupCanvas();
            popupImage.sprite = comboSprite;
        }
        UpdatePopup();
    }

    private void EnsurePopupCanvas()
    {
        if (popupCanvas != null) return;

        // Native UI Image handles transparent sprites and sprite-atlas UVs.
        GameObject root = new GameObject("Line clear popup", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasGroup));
        popupCanvas = root.GetComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = 300;
        popupGroup = root.GetComponent<CanvasGroup>();
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;
        popupGroup.alpha = 0f;

        GameObject artwork = new GameObject("Popup artwork", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        artwork.transform.SetParent(root.transform, false);
        popupImage = artwork.GetComponent<Image>();
        popupImage.preserveAspect = true;
        popupImage.raycastTarget = false;
        popupImage.color = Color.white;
        popupImage.rectTransform.anchorMin = Vector2.zero;
        popupImage.rectTransform.anchorMax = Vector2.zero;
        popupImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void UpdatePopup()
    {
        if (popupCanvas == null) return;
        Camera camera = Camera.main;
        if (!showComboText || comboSprite == null || comboAge >= ComboLife || camera == null)
        {
            popupGroup.alpha = 0f;
            return;
        }

        Vector3 screen = camera.WorldToScreenPoint(comboPosition);
        if (screen.z <= 0f)
        {
            popupGroup.alpha = 0f;
            return;
        }

        float t = Mathf.Clamp01(comboAge / ComboLife);
        // Fast entrance, a small overshoot, then a readable hold and soft exit.
        float pop = t < 0.18f
            ? Mathf.Lerp(0.65f, 1.12f, Mathf.SmoothStep(0f, 1f, t / 0.18f))
            : Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.18f) / 0.16f));
        float alphaIn = Mathf.Clamp01(t / 0.07f);
        float alphaOut = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f);
        popupGroup.alpha = alphaIn * alphaOut;

        Rect safe = Screen.safeArea;
        float aspect = comboSprite.rect.width / Mathf.Max(1f, comboSprite.rect.height);
        float baseWidth = Mathf.Min(safe.width, safe.height) * Mathf.Clamp(popupWidth, 0.2f, 0.8f);
        float width = Mathf.Min(baseWidth * pop, safe.width * 0.94f);
        float height = width / aspect;
        if (height > safe.height * 0.4f)
        {
            height = safe.height * 0.4f;
            width = height * aspect;
        }
        popupImage.rectTransform.sizeDelta = new Vector2(width, height);
        // Place above the board, keeping the animated artwork inside the safe area.
        float x = Mathf.Clamp(screen.x, safe.xMin + width * 0.5f, safe.xMax - width * 0.5f);
        float y = Mathf.Clamp(screen.y + baseWidth / aspect * 0.55f + popupOffsetY + t * 22f,
            safe.yMin + height * 0.5f, safe.yMax - height * 0.5f);
        popupImage.rectTransform.anchoredPosition = new Vector2(x, y);
    }

    private void Update()
    {
        comboAge += Time.deltaTime;
        UpdatePopup();
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

    // Keep the original text as a fallback when artwork has not been assigned.
    private void OnGUI()
    {
        if (!showComboText || comboAge >= ComboLife || string.IsNullOrEmpty(comboText)) return;
        if (comboSprite != null) return;
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
        if (popupGroup != null) popupGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        if (whiteSprite != null) Destroy(whiteSprite);
        if (popupCanvas != null) Destroy(popupCanvas.gameObject);
    }
}
