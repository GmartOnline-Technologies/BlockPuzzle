using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager ins;

    [HideInInspector]
    public Vector3 lastPosition;
    [HideInInspector]
    public Block draggedBlock;

    private ScreenOrientation screenOrientation;
    private Vector3 startPos;
    private Vector2Int lastPos = new Vector2Int(-1, -1);

    // Memory arrays restore the cell colors without causing NullReferenceExceptions
    private SpriteRenderer[] highlightedTiles = new SpriteRenderer[9];
    private Color[] highlightedColors = new Color[9]; 

    public void ResetBlock()
    {
        if (draggedBlock)
        {
            MoveDraggedBlock();
            ResetDraggedBlock();
        }
    }

    private void Awake()
    {
        if (!ins) ins = this;
    }

    private void Update()
    {
        if (screenOrientation != Screen.orientation)
            ResetBlock();

        bool inputBegan = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool inputMoved = Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved);
        bool inputEnded = Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

        Vector3 inputPosition = Input.mousePosition;
        if (Input.touchCount > 0) inputPosition = Input.GetTouch(0).position;

        if (inputBegan)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 15))
            {
                screenOrientation = Screen.orientation;
                Collider c = hit.collider;
                Block hitBlock = c.GetComponent<Block>();

                if (c.tag == "Block" && hitBlock != null && hitBlock.movable && !hitBlock.IsMoving())
                {
                    draggedBlock = hitBlock;
                    draggedBlock.Scale(true, 0.2f);
                    Color cl = draggedBlock.defaultColor; cl.a = 0.66f;
                    draggedBlock.ChangeColor(cl);

                    Vector3 p = draggedBlock.transform.position;
                    startPos = Camera.main.ScreenToWorldPoint(inputPosition);
                    startPos = new Vector3(startPos.x - p.x, startPos.y - p.y, 0);
                }
            }
        }
        else if (inputMoved && draggedBlock)
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(inputPosition);
            pos = new Vector3(pos.x, pos.y, -2);
            draggedBlock.transform.position = pos - startPos;

            Vector3 size = draggedBlock.GetComponent<Block>().size;
            size = new Vector3(size.x - 1, size.y - 1, 0);

            Vector2 origin = draggedBlock.transform.GetChild(0).position;
            Vector2 end = draggedBlock.transform.GetChild(0).position + size;

            if (IsInRange(origin, end) && IsEmpty(draggedBlock, RoundVector2(origin)))
            {
                Vector2Int start = RoundVector2(origin);

                if (lastPos != start)
                {
                    RemoveAllHighlights();
                    BoardManager.ins.HighlightBlocks();

                    for (int i = 0; i < draggedBlock.structure.Length; i++)
                    {
                        if (draggedBlock.transform.GetChild(i).name == "Block tile")
                        {
                            Vector2Int coords = draggedBlock.structure[i];
                            highlightedTiles[i] = BoardManager.ins.boardTiles[start.x + coords.x, start.y + coords.y];
                            
                            if (highlightedTiles[i] != null)
                            {
                                highlightedColors[i] = highlightedTiles[i].color;
                                highlightedTiles[i].color = BoardManager.ins.highlightColor;
                            }
                        }
                    }
                }
                lastPos = start;
            }
            else
            {
                RemoveAllHighlights();
                lastPos = new Vector2Int(-1, -1);
            }
        }
        else if (inputEnded && draggedBlock)
        {
            Vector3 size = draggedBlock.size;
            size = new Vector3(size.x - 1, size.y - 1, 0);

            Vector2 origin = draggedBlock.transform.GetChild(0).position;
            Vector2 end = draggedBlock.transform.GetChild(0).position + size;
            
            if (IsInRange(origin, end) && IsEmpty(draggedBlock, RoundVector2(origin)))
            {
                lastPosition = BlockPosition(origin, size);
                
                draggedBlock.Move(0.08f, lastPosition);
                draggedBlock.ChangeColor(draggedBlock.defaultColor);
                draggedBlock.GetComponent<BoxCollider>().enabled = false;
                draggedBlock.enabled = false;

                Vector2Int start = RoundVector2(origin);

                for (int i = 0; i < draggedBlock.structure.Length; i++)
                {
                    Vector2Int coords = draggedBlock.structure[i];
                    if (draggedBlock.transform.GetChild(i).name == "Block tile")
                    {
                        BlockTile b = draggedBlock.transform.GetChild(i).GetComponent<BlockTile>();
                        BoardManager.ins.boardBlocks[start.x + coords.x, start.y + coords.y] = b;
                    }
                }

                BoardManager.ins.MoveBlocks(draggedBlock.posIndex);
                BoardManager.ins.CheckBoard();
            }
            else
            {
                draggedBlock.Scale(false, 0.2f);
                draggedBlock.Move(0.25f, draggedBlock.basePosition);
                draggedBlock.ChangeColor(draggedBlock.defaultColor);
            }

            startPos = Vector3.zero;
            draggedBlock = null;
            RemoveAllHighlights();
        }
    }

    private Vector2Int RoundVector2(Vector2 v)
    {
        return new Vector2Int((int)(v.x + 0.5f), (int)(v.y + 0.5f));
    }

    private Vector3 BlockPosition(Vector2 o, Vector2 s)
    {
        Vector3 off = Vector3.zero;
        if (s.x % 2 == 1) off.x = 0.5f;
        if (s.y % 2 == 1) off.y = 0.5f;

        return new Vector3((int)(o.x + 0.5f) + (int)(s.x / 2), (int)(o.y + 0.5f) + (int)(s.y / 2), -1) + off;
    }

    private bool IsInRange(Vector2 o, Vector2 e)
    {
        return BoardManager.ins.IsInRange(o, e);
    }

    private bool IsEmpty(Block b, Vector2 o)
    {
        return BoardManager.ins.IsEmpty(b, o);
    }

    private void RemoveAllHighlights()
    {
        foreach (BlockTile b in BoardManager.ins.boardBlocks)
            if (b != null)
                b.Fade(0.2f, b.defaultColor);

        if (highlightedTiles != null && highlightedColors != null)
        {
            for (int i = 0; i < 9; i++)
            {
                if (highlightedTiles[i] != null)
                {
                    highlightedTiles[i].color = highlightedColors[i];
                    highlightedTiles[i] = null;
                }
            }
        }
    }
    
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
            ResetBlock();
    }
    
    private void MoveDraggedBlock()
    {
        draggedBlock.Scale(false, 0.2f);
        draggedBlock.Move(0.25f, draggedBlock.basePosition);
        draggedBlock.ChangeColor(draggedBlock.defaultColor);
    }

    private void ResetDraggedBlock()
    {
        startPos = Vector3.zero;
        draggedBlock = null;
        RemoveAllHighlights();
    }
}