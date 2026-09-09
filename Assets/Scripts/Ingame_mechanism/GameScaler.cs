using UnityEngine;

public class GameScaler : MonoBehaviour
{
    public static GameScaler ins;

    [Header("UI References for Auto-Scaling")]
    [Tooltip("Drag the dark background Image from your Canvas here.")]
    public RectTransform boardUIBox;
    
    [Tooltip("Drag the wooden tray Image from your Canvas here.")]
    public RectTransform trayUIBox;

    [Header("Fine-Tuning")]
    [Tooltip("Increase this number to push the blocks UP into the tray. Decrease it to push them DOWN.")]
    public float trayYOffset = 0.8f;

    [HideInInspector]
    public float trayBlockYPosition = -1.5f;

    private void Awake()
    {
        ins = this;
    }

    private void Start()
    {
        // 1. Force the UI to calculate its layout instantly before the screen ever renders
        Canvas.ForceUpdateCanvases();

        if (boardUIBox != null)
        {
            // 2. Measure and apply the camera zoom instantly
            Vector3[] corners = new Vector3[4];
            boardUIBox.GetWorldCorners(corners);
            float currentWorldWidth = corners[2].x - corners[0].x;

            Camera.main.orthographicSize = Camera.main.orthographicSize * (8.2f / currentWorldWidth);

            // 3. Re-measure the UI after the zoom and apply the camera position instantly
            boardUIBox.GetWorldCorners(corners);
            Vector3 boxWorldCenter = (corners[0] + corners[2]) / 2f;

            Vector3 difference = new Vector3(3.5f, 3.5f, 0) - boxWorldCenter;
            Camera.main.transform.position += new Vector3(difference.x, difference.y, 0);
        }

        if (trayUIBox != null)
        {
            // 4. Calculate the tray position instantly
            Vector3[] trayCorners = new Vector3[4];
            trayUIBox.GetWorldCorners(trayCorners);
            
            trayBlockYPosition = ((trayCorners[0].y + trayCorners[2].y) / 2f) + trayYOffset; 
            
            // 5. Move the 3 starting blocks to the tray before the player sees them
            for (int i = 0; i < BoardManager.BLOCKS_AMOUNT; i++)
            {
                if (BoardManager.ins.blocks[i] != null)
                {
                    BoardManager.ins.blocks[i].SetBasePosition(i, true);
                }
            }
        }
    }

    public static float GetBlockY()
    {
        if (ins != null) return ins.trayBlockYPosition;
        return -1.5f; 
    }

    public static Vector3 GetBoardTileScale()
    {
        return new Vector3(0.95f, 0.95f, 1f); 
    }

    public static Vector3 GetScaledBlockTileScale()
    {
        return new Vector3(0.95f, 0.95f, 1f);
    }
}