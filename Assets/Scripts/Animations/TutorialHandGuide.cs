using UnityEngine;
using UnityEngine.UI;

// Add to an empty UI object under the Tutorial Canvas. No Animator required.
[RequireComponent(typeof(RectTransform))]
public class TutorialHandGuide : MonoBehaviour
{
    [Header("References")]
    public Sprite handSprite;
    public BoardManager board;
    public InputManager input;
    public Camera boardCamera;
    [Range(0, 2)] public int traySlot = 1;

    [Header("Hand appearance (Canvas units)")]
    public float handWidth = 105f;
    [Tooltip("Fingertip location within the sprite: bottom-left is (0,0), top-right is (1,1).")]
    public Vector2 fingertip = new Vector2(0.25f, 0.8f);
    public float handAngle = 0f;
    public Color glowColor = new Color(0.3f, 1f, 0.9f, 1f);
    public float glowSize = 110f;

    [Header("Motion")]
    public float travelSeconds = 1.05f;
    public float startHold = 0.45f;
    public float endHold = 0.35f;
    public float fadeSeconds = 0.25f;
    public float arcAmount = 24f;

    private Canvas canvas;
    private RectTransform surface, pointer, handRect, haloRect, ringRect;
    private Image halo, ring;
    private CanvasGroup visibility;
    private Texture2D haloTexture, ringTexture;
    private Sprite haloSprite, ringSprite;
    private Block piece;
    private bool acquired, completed;
    private float age, returnDelay;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null || handSprite == null)
        {
            Debug.LogError("TutorialHandGuide: place under a Canvas and assign Hand Sprite.", this);
            enabled = false;
            return;
        }
        surface = (RectTransform)transform;
        surface.anchorMin = Vector2.zero;
        surface.anchorMax = Vector2.one;
        surface.offsetMin = surface.offsetMax = Vector2.zero;
        transform.SetAsLastSibling();
        visibility = gameObject.AddComponent<CanvasGroup>();
        visibility.interactable = false;
        visibility.blocksRaycasts = false;
        visibility.alpha = 0f;

        pointer = new GameObject("Animated guide", typeof(RectTransform)).GetComponent<RectTransform>();
        pointer.SetParent(surface, false);
        pointer.anchorMin = pointer.anchorMax = surface.pivot;
        haloSprite = MakeSprite(false, out haloTexture);
        ringSprite = MakeSprite(true, out ringTexture);
        halo = MakeImage("Soft glow", haloSprite, pointer);
        haloRect = halo.rectTransform;
        ring = MakeImage("Expanding pulse", ringSprite, pointer);
        ringRect = ring.rectTransform;
        Image hand = MakeImage("Hand", handSprite, pointer);
        handRect = hand.rectTransform;
        handRect.pivot = fingertip;
        handRect.sizeDelta = new Vector2(handWidth, handWidth * handSprite.rect.height / handSprite.rect.width);
        handRect.localRotation = Quaternion.Euler(0f, 0f, handAngle);
    }

    private void Update()
    {
        if (visibility == null) return;
        if (completed) { visibility.alpha = 0f; return; }
        if (board == null) board = BoardManager.ins;
        if (input == null) input = InputManager.ins;
        if (boardCamera == null) boardCamera = Camera.main;
        if (board == null || input == null || boardCamera == null || Time.timeScale == 0f)
        { visibility.alpha = 0f; return; }

        if (!acquired)
        {
            if (board.blocks == null || traySlot >= board.blocks.Length) return;
            piece = board.blocks[traySlot];
            if (piece == null) return;
            acquired = true;
        }
        // BoardManager removes the tray reference only after successful placement.
        if (piece == null || board.blocks[traySlot] != piece)
        { CompleteGuide(); return; }
        if (input.draggedBlock != null)
        {
            visibility.alpha = 0f;
            age = 0f;
            returnDelay = 0.4f;
            return;
        }
        if (returnDelay > 0f)
        {
            returnDelay -= Time.deltaTime;
            visibility.alpha = 0f;
            return;
        }
        SpriteRenderer low = board.boardTiles[3, 3];
        SpriteRenderer high = board.boardTiles[4, 4];
        if (low == null || high == null) { visibility.alpha = 0f; return; }
        Vector2 from, to;
        if (!Project(PieceCenter(), out from) ||
            !Project((low.transform.position + high.transform.position) * 0.5f, out to))
        { visibility.alpha = 0f; return; }

        float travel = Mathf.Max(0.1f, travelSeconds);
        float fade = Mathf.Max(0.05f, fadeSeconds);
        float holdStart = Mathf.Max(0.1f, startHold);
        float holdEnd = Mathf.Max(0f, endHold);
        float fadeAt = holdStart + travel + holdEnd;
        age = (age + Time.deltaTime) % (fadeAt + fade + 0.15f);
        float move = Mathf.Clamp01((age - holdStart) / travel);
        float eased = move * move * (3f - 2f * move);
        Vector2 position = Vector2.Lerp(from, to, eased);
        position.x += Mathf.Sin(eased * Mathf.PI) * arcAmount;
        pointer.anchoredPosition = position;
        visibility.alpha = Mathf.Clamp01(age / fade) * (1f - Mathf.Clamp01((age - fadeAt) / fade));

        float pulse = 0.5f + 0.5f * Mathf.Sin(age * Mathf.PI * 3f);
        handRect.localScale = Vector3.one * Mathf.Lerp(0.97f, 1.04f, pulse);
        haloRect.sizeDelta = Vector2.one * glowSize * Mathf.Lerp(0.85f, 1.1f, pulse);
        halo.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.45f * glowColor.a);
        float ripple = Mathf.Repeat(age / 0.9f, 1f);
        ringRect.sizeDelta = Vector2.one * glowSize * Mathf.Lerp(0.35f, 1.3f, ripple);
        ring.color = new Color(glowColor.r, glowColor.g, glowColor.b, (1f - ripple) * 0.6f * glowColor.a);
    }

    private Vector3 PieceCenter()
    {
        bool found = false;
        Bounds bounds = new Bounds(piece.transform.position, Vector3.zero);
        foreach (SpriteRenderer renderer in piece.GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.gameObject.name.Trim() != "Block tile") continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds.center;
    }

    private bool Project(Vector3 world, out Vector2 local)
    {
        Vector3 screen = boardCamera.WorldToScreenPoint(world);
        Camera uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.rootCanvas.worldCamera;
        local = Vector2.zero;
        return screen.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screen, uiCamera, out local);
    }

    private static Image MakeImage(string name, Sprite sprite, RectTransform parent)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.sprite = sprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    // Small procedural UI textures: no extra art, particles or glow shader needed.
    private static Sprite MakeSprite(bool outline, out Texture2D texture)
    {
        const int size = 96;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
            float alpha = outline ? 1f - Mathf.SmoothStep(0.025f, 0.075f, Mathf.Abs(radius - 0.82f))
                : Mathf.Pow(Mathf.Clamp01(1f - radius), 2f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
    }

    public void CompleteGuide()
    {
        completed = true;
        if (visibility != null) visibility.alpha = 0f;
    }

    private void OnDisable()
    {
        if (visibility != null) visibility.alpha = 0f;
        age = 0f;
    }

    private void OnDestroy()
    {
        if (pointer != null) Destroy(pointer.gameObject);
        if (haloSprite != null) Destroy(haloSprite);
        if (ringSprite != null) Destroy(ringSprite);
        if (haloTexture != null) Destroy(haloTexture);
        if (ringTexture != null) Destroy(ringTexture);
        if (visibility != null) Destroy(visibility);
    }
}
