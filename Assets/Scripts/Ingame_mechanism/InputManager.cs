using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    public static InputManager ins;
    public HelpPurchaseController helps;
    public HammerHitEffect hammerEffect;
    public enum PowerUpMode { None, Hammer, Rotator }
    [Header("Power-Up States")] public PowerUpMode currentMode = PowerUpMode.None;
    [HideInInspector] public Vector3 lastPosition;
    [HideInInspector] public Block draggedBlock;
    [Header("Placement shine")] public BlockPlacementShine placementShine;

    private ScreenOrientation screenOrientation;
    private Vector3 grabbedLocalPoint;
    private readonly Dictionary<SpriteRenderer, Color> highlighted = new Dictionary<SpriteRenderer, Color>();
    private Vector2Int lastPos;
    private bool hasPreview;
    private bool canUndo;
    private int undoPrefabIndex, undoPosIndex, undoQuarterTurns, undoSession;
    private readonly List<Vector2Int> undoBoardCoords = new List<Vector2Int>();
    private readonly List<BlockTile> undoTiles = new List<BlockTile>();

    private bool Busy()
    {
        return (hammerEffect != null && hammerEffect.IsPlaying) || (helps != null && helps.IsOpen) || BoardManager.ins == null || (DestroyManager.ins != null && DestroyManager.ins.IsClearing)
            || (GameManager.ins != null && (GameManager.ins.paused || GameManager.ins.gameOver));
    }

    private void Awake()
    {
        if (ins != null && ins != this) { enabled = false; return; }
        ins = this;
        if (hammerEffect == null) hammerEffect = GetComponent<HammerHitEffect>();
        if (hammerEffect == null) hammerEffect = gameObject.AddComponent<HammerHitEffect>();
        screenOrientation = Screen.orientation;
    }

    public void ActivateHammer()
    {
        if (Busy()) return;
        if (currentMode != PowerUpMode.Hammer && (helps == null || !helps.RequireOwned(HelpPurchaseController.Kind.Hammer))) return;
        ResetBlock();
        currentMode = currentMode == PowerUpMode.Hammer ? PowerUpMode.None : PowerUpMode.Hammer;
    }

    public void ActivateRotator()
    {
        if (Busy()) return;
        if (currentMode != PowerUpMode.Rotator && (helps == null || !helps.RequireOwned(HelpPurchaseController.Kind.Rotator))) return;
        ResetBlock();
        currentMode = currentMode == PowerUpMode.Rotator ? PowerUpMode.None : PowerUpMode.Rotator;
    }

    public void TriggerUndo()
    {
        if (Busy()) return;
        if (helps == null || !helps.RequireOwned(HelpPurchaseController.Kind.Undo)) return;
        if (!canUndo || draggedBlock != null || currentMode != PowerUpMode.None) return;
        if (GameManager.ins != null && undoSession != GameManager.ins.ScoreSessionVersion) { canUndo = false; return; }
        Block restored = BoardManager.ins.SpawnBlock(undoPosIndex, undoPrefabIndex);
        if (restored == null) return;
        RemoveAllHighlights();
        for (int i = 0; i < undoBoardCoords.Count; i++)
        {
            Vector2Int cell = undoBoardCoords[i];
            if (BoardManager.ins.boardBlocks[cell.x, cell.y] != undoTiles[i]) continue;
            if (undoTiles[i] != null) Destroy(undoTiles[i].gameObject);
            BoardManager.ins.boardBlocks[cell.x, cell.y] = null;
        }
        helps.Consume(HelpPurchaseController.Kind.Undo);
        if (restored != null)
            for (int i = 0; i < undoQuarterTurns; i++) restored.Rotate90();
        BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.Undo);
        BoardManager.ins.CheckSpace(false);
        canUndo = false;
    }

    private bool PointerOnPlane(Vector3 screen, float z, out Vector3 world)
    {
        world = Vector3.zero;
        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(screen);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
        float distance;
        if (!plane.Raycast(ray, out distance)) return false;
        world = ray.GetPoint(distance);
        return true;
    }

    private void Update()
    {
        if (Busy()) { if (draggedBlock != null) ResetBlock(); return; }
        if (screenOrientation != Screen.orientation)
        {
            ResetBlock();
            screenOrientation = Screen.orientation;
            return;
        }

        Vector3 pointer = Input.mousePosition;
        bool began, held, ended, canceled = false;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer = touch.position;
            began = touch.phase == TouchPhase.Began;
            ended = touch.phase == TouchPhase.Ended;
            canceled = touch.phase == TouchPhase.Canceled;
            held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
        }
        else
        {
            began = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            ended = Input.GetMouseButtonUp(0);
        }
        if (canceled) { ResetBlock(); return; }
        if (began) BeginPointer(pointer);
        else if (draggedBlock != null && (held || ended))
        {
            // Sample the release position too, rather than using last frame's position.
            if (!MoveToPointer(pointer)) { if (ended) ResetBlock(); return; }
            if (ended) ReleaseBlock();
            else UpdatePreview();
        }
    }

    private void BeginPointer(Vector3 pointer)
    {
        if (draggedBlock != null || Camera.main == null) return;
        if (EventSystem.current != null && (Input.touchCount > 0
            ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
            : EventSystem.current.IsPointerOverGameObject())) return;
        RaycastHit hit;
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(pointer), out hit, 100f)) return;
        Collider collider = hit.collider;
        if (currentMode == PowerUpMode.Hammer)
        {
            BlockTile tile = collider.GetComponent<BlockTile>();
            if (tile == null) return;
            for (int x = 0; x < BoardManager.BOARD_SIZE; x++)
                for (int y = 0; y < BoardManager.BOARD_SIZE; y++)
                    if (BoardManager.ins.boardBlocks[x, y] == tile)
                    {
                        if (helps == null || !helps.Consume(HelpPurchaseController.Kind.Hammer)) { currentMode = PowerUpMode.None; return; }
                        canUndo = false;
                        currentMode = PowerUpMode.None;
                        if (hammerEffect != null && hammerEffect.isActiveAndEnabled)
                            hammerEffect.Strike(tile, x, y);
                        else
                        {
                            tile.Destroy(0.2f);
                            BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.Hammer);
                            BoardManager.ins.boardBlocks[x, y] = null;
                            BoardManager.ins.CheckSpace(false);
                        }
                        return;
                    }
            return;
        }

        Block block = collider.GetComponent<Block>();
        if (block == null || !block.enabled || block.IsMoving() || !block.HasValidLayout) return;
        bool inTray = false;
        foreach (Block trayBlock in BoardManager.ins.blocks) if (trayBlock == block) inTray = true;
        if (!inTray) return;
        if (currentMode == PowerUpMode.Rotator)
        {
            if (helps == null || !helps.Consume(HelpPurchaseController.Kind.Rotator)) { currentMode = PowerUpMode.None; return; }
            block.Rotate90();
            BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.Rotate);
            BoardManager.ins.CheckSpace(false);
            currentMode = PowerUpMode.None;
            return;
        }
        if (!block.movable) return;
        Vector3 worldGrab;
        if (!PointerOnPlane(pointer, block.transform.position.z, out worldGrab)) return;
        RemoveAllHighlights();
        grabbedLocalPoint = block.transform.InverseTransformPoint(worldGrab);
        grabbedLocalPoint.z = 0f;
        draggedBlock = block;
        // Use a stable full-size footprint for both preview and placement.
        block.Scale(true, 0f);
        Color color = block.defaultColor; color.a = 0.66f;
        block.ChangeColor(color);
        MoveToPointer(pointer);
        UpdatePreview();
    }

    private bool MoveToPointer(Vector3 pointer)
    {
        Vector3 world;
        if (!PointerOnPlane(pointer, -2f, out world)) return false;
        draggedBlock.transform.position = world - draggedBlock.transform.TransformVector(grabbedLocalPoint);
        return true;
    }

    private void UpdatePreview()
    {
        Vector2Int origin;
        Vector3 target;
        if (!BoardManager.ins.TryGetPlacement(draggedBlock, out origin, out target))
        {
            RemoveAllHighlights();
            return;
        }
        if (hasPreview && origin == lastPos) return;
        RemoveAllHighlights();
        BoardManager.ins.HighlightBlocks();
        foreach (Vector2Int offset in draggedBlock.TileCells)
        {
            Vector2Int cell = origin + offset;
            SpriteRenderer renderer = BoardManager.ins.boardTiles[cell.x, cell.y];
            if (renderer == null || highlighted.ContainsKey(renderer)) continue;
            highlighted.Add(renderer, renderer.color);
            renderer.color = BoardManager.ins.highlightColor;
        }
        lastPos = origin;
        hasPreview = true;
    }

    private void ReleaseBlock()
    {
        RemoveAllHighlights();
        Vector2Int origin;
        Vector3 target;
        Block block = draggedBlock;
        if (!BoardManager.ins.TryPlace(block, out origin, out target))
        {
            BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.InvalidPlacement);
            ResetBlock();
            return;
        }
        BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.Place);
        canUndo = true;
        undoPrefabIndex = block.prefabIndex;
        undoPosIndex = block.posIndex;
        undoQuarterTurns = block.QuarterTurns;
        undoSession = GameManager.ins != null ? GameManager.ins.ScoreSessionVersion : 0;
        undoBoardCoords.Clear();
        undoTiles.Clear();
        for (int i = 0; i < block.Tiles.Length; i++)
        {
            undoBoardCoords.Add(origin + block.TileCells[i]);
            undoTiles.Add(block.Tiles[i]);
        }
        lastPosition = target;
        block.Move(0.08f, target);
        block.ChangeColor(block.defaultColor);
        BoxCollider collider = block.GetComponent<BoxCollider>();
        if (collider != null) collider.enabled = false;
        block.enabled = false;
        draggedBlock = null;
        if (placementShine == null)
        {
            placementShine = GetComponent<BlockPlacementShine>();
            if (placementShine == null) placementShine = gameObject.AddComponent<BlockPlacementShine>();
        }
        placementShine.Play(block);
        BoardManager.ins.MoveBlocks(block.posIndex);
        BoardManager.ins.CheckBoard();
        if (DestroyManager.ins != null && DestroyManager.ins.destroyedLines > 0) canUndo = false;
    }

    public void ResetBlock()
    {
        RemoveAllHighlights();
        if (draggedBlock == null) return;
        draggedBlock.Scale(false, 0.2f);
        draggedBlock.SetBasePosition(draggedBlock.posIndex, false);
        draggedBlock.Move(0.25f, draggedBlock.basePosition);
        draggedBlock.ChangeColor(draggedBlock.defaultColor);
        draggedBlock = null;
    }

    private void RemoveAllHighlights()
    {
        if (BoardManager.ins != null) BoardManager.ins.ClearBlockHighlights();
        foreach (KeyValuePair<SpriteRenderer, Color> item in highlighted)
            if (item.Key != null) item.Key.color = item.Value;
        highlighted.Clear();
        hasPreview = false;
    }

    private void OnApplicationPause(bool paused) { if (paused) ResetBlock(); }
    private void OnDisable() { ResetBlock(); }
}
