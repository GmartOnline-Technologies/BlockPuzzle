using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local integration. Server item IDs/inventory contract are still required.
public class HelpPurchaseController : MonoBehaviour
{
    public enum Kind { Hammer, Rotator, Undo }
    [Serializable] public class HelpUI
    {
        public GameObject board;
        public TMP_Text badge, quantityText, costText, keysText;
        public GameObject insufficientText;
        public Button buyButton;
    }
    public InputManager input;
    public GameObject modalRoot;
    public HelpUI hammer = new HelpUI(), rotator = new HelpUI(), undo = new HelpUI();
    private const int Cost = 10;
    private int active = -1, quantity = 1, owner;
    private float previousTimeScale;
    private bool previousPaused;
    public bool IsOpen => active >= 0;
    private HelpUI UI(Kind kind) => kind == Kind.Hammer ? hammer : kind == Kind.Rotator ? rotator : undo;
    private int User => PlayerPrefs.GetInt("UserId", 0);
    private string InventoryKey(Kind kind) => "BlockPuzzle.Help." + User + "." + kind;
    private string BalanceKey => "BlockPuzzle.Keys." + User;
    public int Count(Kind kind) => User > 0 ? Mathf.Max(0, PlayerPrefs.GetInt(InventoryKey(kind), 0)) : 0;

    private void Start()
    {
        if (input == null) input = InputManager.ins;
        if (input != null) input.helps = this;
        HideBoards();
        Refresh();
    }
    private void Update()
    {
        if (IsOpen && User != owner) Close();
        Refresh();
    }
    private void HideBoards()
    {
        if (hammer.board != null) hammer.board.SetActive(false);
        if (rotator.board != null) rotator.board.SetActive(false);
        if (undo.board != null) undo.board.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
    }
    public bool RequireOwned(Kind kind)
    {
        if (Count(kind) > 0) return true;
        Open(kind);
        return false;
    }
    public bool Consume(Kind kind)
    {
        int count = Count(kind);
        if (count <= 0) return false;
        PlayerPrefs.SetInt(InventoryKey(kind), count - 1);
        PlayerPrefs.Save();
        Refresh();
        return true;
    }
    private void Open(Kind kind)
    {
        if (IsOpen || User <= 0 || modalRoot == null || UI(kind).board == null) return;
        if (GameManager.ins != null && (GameManager.ins.paused || GameManager.ins.gameOver)) return;
        owner = User;
        if (input != null) { input.ResetBlock(); input.currentMode = InputManager.PowerUpMode.None; }
        previousTimeScale = Time.timeScale;
        previousPaused = GameManager.ins != null && GameManager.ins.paused;
        if (GameManager.ins != null) GameManager.ins.paused = true;
        Time.timeScale = 0f;
        active = (int)kind;
        quantity = 1;
        HideBoards();
        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();
        UI(kind).board.SetActive(true);
        Refresh();
    }
    public void Increase() { if (IsOpen && quantity < 999) { quantity++; Refresh(); } }
    public void Decrease() { if (IsOpen && quantity > 1) { quantity--; Refresh(); } }
    public void Buy()
    {
        if (!IsOpen || User <= 0 || User != owner) return;
        int balance = PlayerPrefs.GetInt(BalanceKey, 0);
        int total = quantity * Cost;
        Kind kind = (Kind)active;
        if (balance < total || Count(kind) > int.MaxValue - quantity) { Refresh(); return; }
        PlayerPrefs.SetInt(BalanceKey, balance - total);
        PlayerPrefs.SetInt("PlayerKeys", balance - total);
        PlayerPrefs.SetInt(InventoryKey(kind), Count(kind) + quantity);
        PlayerPrefs.Save();
        Close();
        Refresh();
    }
    public void Close()
    {
        if (!IsOpen) return;
        active = -1;
        HideBoards();
        if (GameManager.ins != null) GameManager.ins.paused = previousPaused;
        Time.timeScale = previousTimeScale;
    }
    private static void Text(TMP_Text label, int value)
    {
        if (label != null && label.text != value.ToString()) label.text = value.ToString();
    }
    private void Refresh()
    {
        Text(hammer.badge, Count(Kind.Hammer));
        Text(rotator.badge, Count(Kind.Rotator));
        Text(undo.badge, Count(Kind.Undo));
        if (!IsOpen) return;
        HelpUI ui = UI((Kind)active);
        int balance = PlayerPrefs.GetInt(BalanceKey, 0);
        int total = quantity * Cost;
        bool affordable = User == owner && User > 0 && balance >= total && Count((Kind)active) <= int.MaxValue - quantity;
        Text(ui.quantityText, quantity); Text(ui.costText, total); Text(ui.keysText, balance);
        if (ui.insufficientText != null) ui.insufficientText.SetActive(balance < total);
        if (ui.buyButton != null) ui.buyButton.interactable = affordable;
    }
    private void OnDisable() { Close(); }
}
