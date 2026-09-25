using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TermsPanelController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public RectTransform viewportRect; // visible mask area
    public RectTransform contentRect;  // long text/content area
    public Button closeButton;

    [Header("Scroll Settings")]
    public float scrollSensitivity = 1.0f;
    public float inertiaPower = 12f;
    public float inertiaDamping = 8f;

    [Header("Scroll Arrow")]
    public RectTransform scrollArrowT;
    public GameObject scrollArrow;
    public float arrowMoveDistance = 10f;
    public float arrowMoveSpeed = 3f;

    [Header("Close Button Rule")]
    public bool requireScrollToBottomBeforeClose = false;

    private float minY = 0f;
    private float maxY = 0f;

    private Vector2 lastDragPosition;
    private float dragVelocityY;
    private bool isDragging = false;
    private bool useInertia = false;

    private Vector2 arrowStartPos;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (scrollArrowT != null)
            arrowStartPos = scrollArrowT.anchoredPosition;
    }

    void OnEnable()
    {
        ResetPanel();
    }

    void Update()
    {
        HandleInertia();
        AnimateArrow();
    }

    void ResetPanel()
    {
        isDragging = false;
        useInertia = false;
        dragVelocityY = 0f;

        if (contentRect != null)
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0f);

        if (closeButton != null)
            closeButton.interactable = !requireScrollToBottomBeforeClose;

        if (scrollArrow != null)
            scrollArrow.SetActive(true);

        Invoke(nameof(CalculateBounds), 0.1f);
    }

    void CalculateBounds()
    {
        if (contentRect == null || viewportRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float contentHeight = contentRect.rect.height;
        float viewportHeight = viewportRect.rect.height;

        if (contentHeight <= viewportHeight)
        {
            minY = 0f;
            maxY = 0f;

            if (closeButton != null)
                closeButton.interactable = true;

            if (scrollArrow != null)
                scrollArrow.SetActive(false);
        }
        else
        {
            minY = 0f;
            maxY = contentHeight - viewportHeight;
        }

        ClampContentPosition();
        CheckIfReadFinished();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        useInertia = false;
        lastDragPosition = eventData.position;
        dragVelocityY = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (contentRect == null)
            return;

        float diffY = (eventData.position.y - lastDragPosition.y) * scrollSensitivity;

        Vector2 newPos = contentRect.anchoredPosition;
        newPos.y += diffY;
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        contentRect.anchoredPosition = newPos;

        dragVelocityY = diffY;
        lastDragPosition = eventData.position;

        CheckIfReadFinished();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        useInertia = true;
    }

    void HandleInertia()
    {
        if (!useInertia || isDragging || contentRect == null)
            return;

        if (Mathf.Abs(dragVelocityY) < 0.01f)
        {
            useInertia = false;
            return;
        }

        Vector2 pos = contentRect.anchoredPosition;
        pos.y += dragVelocityY * inertiaPower * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        contentRect.anchoredPosition = pos;

        dragVelocityY = Mathf.Lerp(dragVelocityY, 0f, inertiaDamping * Time.deltaTime);

        CheckIfReadFinished();

        if (pos.y <= minY || pos.y >= maxY)
            useInertia = false;
    }

    void ClampContentPosition()
    {
        if (contentRect == null)
            return;

        Vector2 pos = contentRect.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        contentRect.anchoredPosition = pos;
    }

    void CheckIfReadFinished()
    {
        if (contentRect == null)
            return;

        float threshold = 20f;
        bool reachedBottom = contentRect.anchoredPosition.y >= maxY - threshold;

        if (reachedBottom)
        {
            if (closeButton != null)
                closeButton.interactable = true;

            if (scrollArrow != null)
                scrollArrow.SetActive(false);
        }
    }

    void AnimateArrow()
    {
        if (scrollArrow == null || scrollArrowT == null || !scrollArrow.activeSelf)
            return;

        float yOffset = Mathf.Sin(Time.time * arrowMoveSpeed) * arrowMoveDistance;
        scrollArrowT.anchoredPosition = arrowStartPos + new Vector2(0f, yOffset);
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}