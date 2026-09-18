using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;

// One dedicated persistent ROOT. Do not put on Canvas or a Buy button.
public class KeyShopService : MonoBehaviour
{
    public static KeyShopService Instance { get; private set; }
    public AppConfig config;
    public string Message { get; private set; } = "Connecting to store...";
    private StoreController store;
    private bool connecting, connected, fetched, processing, fetchingBalance;
    private int observedUser = -1;
    private readonly Dictionary<string, Product> products = new Dictionary<string, Product>();
    private readonly Dictionary<string, PendingOrder> pending = new Dictionary<string, PendingOrder>();
    private readonly HashSet<string> attemptedThisSession = new HashSet<string>();
    private const string IntentKey = "BlockPuzzle.KeyPurchase.Intent";
    private static readonly int[] Packs = { 50, 100, 250, 500, 1000, 1500 };
    private static int User => PlayerPrefs.GetInt("UserId", 0);
    private static string Token => PlayerPrefs.GetString("AccessToken", "");
    private bool Authenticated => User > 0 && !string.IsNullOrEmpty(Token);
    private bool BackendReady => config != null && !string.IsNullOrWhiteSpace(config.gameBaseAPIUrl);
    [Serializable] private class Intent
    {
        public int user;
        public string product, transaction, currency;
        public float price;
    }
    [Serializable] private class Journal
    {
        public int user, phase; // 1 = submission uncertain, 2 = server accepted.
        public string product, transaction;
    }
    private static string ProductId(int count) => "com.gmartonline.blockpuzzle" + count + "keys";
    private static int Amount(string id)
    {
        foreach (int count in Packs) if (ProductId(count) == id) return count;
        return 0;
    }
    private static T Read<T>(string key) where T : class
    {
        if (!PlayerPrefs.HasKey(key)) return null;
        // A malformed journal must not be silently replaced and replayed.
        return JsonUtility.FromJson<T>(PlayerPrefs.GetString(key));
    }
    private static void Save(string key, object value)
    { PlayerPrefs.SetString(key, JsonUtility.ToJson(value)); PlayerPrefs.Save(); }
    private static string JournalKey(string transaction)
    {
        using (var hash = SHA256.Create())
            return "BlockPuzzle.KeyPurchase.Tx." + BitConverter.ToString(
                hash.ComputeHash(Encoding.UTF8.GetBytes(transaction))).Replace("-", "");
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }
    private void Start() { Connect(); }
    private async void Connect()
    {
        if (connecting || connected) return;
        if (!BackendReady) { Message = "Assign AppConfig to KeyShopService."; return; }
        connecting = true;
        try
        {
            if (store == null)
            {
                store = UnityIAPServices.StoreController();
                store.OnProductsFetched += ProductsFetched;
                store.OnProductsFetchFailed += ProductsFailed;
                store.OnPurchasesFetched += PurchasesFetched;
                store.OnPurchasesFetchFailed += PurchasesFailed;
                store.OnPurchasePending += PurchasePending;
                store.OnPurchaseFailed += PurchaseFailed;
                store.OnPurchaseDeferred += PurchaseDeferred;
                store.OnPurchaseConfirmed += PurchaseConfirmed;
                store.OnStoreDisconnected += Disconnected;
            }
            await store.Connect();
            if (this == null) return;
            connected = true;
            FetchProducts();
        }
        catch (Exception) { if (this != null) Message = "Store unavailable. Reopen the shop to retry."; }
        finally { if (this != null) connecting = false; }
    }
    private void FetchProducts()
    {
        var definitions = new List<ProductDefinition>();
        foreach (int count in Packs) definitions.Add(new ProductDefinition(ProductId(count), ProductType.Consumable));
        store.FetchProducts(definitions);
    }
    private void ProductsFetched(List<Product> result)
    {
        products.Clear();
        foreach (Product product in result) products[product.definition.id] = product;
        Message = "Checking pending purchases...";
        store.FetchPurchases();
    }
    private void ProductsFailed(ProductFetchFailed failure)
    { fetched = false; Message = "Products unavailable. Check store setup and reopen the shop."; }
    private void PurchasesFailed(PurchasesFetchFailureDescription failure)
    { fetched = false; Message = "Could not check purchases. Reopen the shop to retry."; }
    private void Disconnected(StoreConnectionFailureDescription failure)
    { connected = false; fetched = false; Message = "Store disconnected. Reopen the shop to reconnect."; }
    private void PurchasesFetched(Orders orders)
    {
        fetched = true;
        // Also handles a restart after store confirmation but before its callback was saved.
        foreach (var order in orders.ConfirmedOrders) ClearConfirmedIntent(order);
        foreach (var order in orders.PendingOrders) PurchasePending(order);
        Intent remaining = Read<Intent>(IntentKey);
        if (remaining != null && !string.IsNullOrEmpty(remaining.transaction))
        {
            Journal completed = Read<Journal>(JournalKey(remaining.transaction));
            // Store may omit confirmed consumables on the next launch.
            if (completed != null && completed.phase == 2 &&
                !orders.PendingOrders.Any(o => o.Info.TransactionID == remaining.transaction))
            {
                pending.Remove(remaining.transaction);
                PlayerPrefs.DeleteKey(IntentKey); PlayerPrefs.Save();
                remaining = null;
            }
        }
        if (pending.Count == 0)
            Message = remaining == null ? "" : "A previous purchase is awaiting a store result. Contact support if it remains stuck.";
        RefreshBalance();
    }
    private void Update()
    {
        if (observedUser != User)
        {
            observedUser = User;
            PlayerPrefs.SetInt("PlayerKeys", User > 0 ? PlayerPrefs.GetInt("BlockPuzzle.Keys." + User, 0) : 0);
            if (Authenticated) RefreshBalance();
        }
        if (!Authenticated || !BackendReady || processing || fetchingBalance) return;
        foreach (var entry in pending.ToArray())
        {
            if (attemptedThisSession.Contains(entry.Key)) continue;
            Journal journal = Read<Journal>(JournalKey(entry.Key));
            Intent intent = Read<Intent>(IntentKey);
            int owner = journal != null ? journal.user : intent != null ? intent.user : 0;
            if (owner != User)
            { Message = "A pending purchase needs its original game account."; continue; }
            attemptedThisSession.Add(entry.Key);
            StartCoroutine(Fulfill(entry.Value));
            break;
        }
    }
    public bool TryGetDisplayPrice(string id, out string price)
    {
        price = null;
        if (!products.TryGetValue(id, out Product product) || product.metadata == null ||
            product.metadata.localizedPrice <= 0m ||
            string.IsNullOrWhiteSpace(product.metadata.localizedPriceString)) return false;
        price = product.metadata.localizedPriceString;
        return true;
    }
    public string Price(string id)
    {
        if (TryGetDisplayPrice(id, out string price)) return price;
        return connected ? "Unavailable" : "Loading...";
    }
    public bool CanBuy(string id)
    {
        return Authenticated && BackendReady && connected && fetched && !processing &&
            pending.Count == 0 && !PlayerPrefs.HasKey(IntentKey) &&
            products.TryGetValue(id, out Product product) && product.availableToPurchase && Amount(id) > 0 &&
            (Application.isEditor || TryGetDisplayPrice(id, out _));
    }
    public void Buy(string id)
    {
        if (!CanBuy(id)) return;
        var product = products[id];
        // Record the account before opening the store, including deferred purchases.
        Save(IntentKey, new Intent { user = User, product = id,
            price = (float)product.metadata.localizedPrice, currency = product.metadata.isoCurrencyCode });
        Message = "Waiting for store...";
        try { store.PurchaseProduct(id); }
        catch (Exception) { Message = "Purchase state is uncertain. Reopen the shop; do not buy again yet."; }
    }
    private void PurchasePending(PendingOrder order)
    {
        if (order == null || order.Info == null || string.IsNullOrEmpty(order.Info.TransactionID))
        { Message = "Purchase has no transaction ID. Contact support."; return; }
        pending[order.Info.TransactionID] = order;
    }
    private IEnumerator Fulfill(PendingOrder order)
    {
        processing = true;
        try
        {
            string transaction = order.Info.TransactionID;
            var items = order.CartOrdered.Items().ToList();
            if (items.Count != 1 || items[0].Quantity != 1)
            { Message = "Unexpected purchase contents. Contact support."; yield break; }
            string id = items[0].Product.definition.id;
            int amount = Amount(id);
            if (amount == 0) { Message = "Unrecognized key product. Contact support."; yield break; }
            string key = JournalKey(transaction);
            Journal journal = Read<Journal>(key);
            Intent intent = Read<Intent>(IntentKey);
            int owner = journal != null ? journal.user : intent != null ? intent.user : 0;
            if (owner != User || owner <= 0) yield break;
            string token = Token; // Capture the original authenticated account.
            if (journal != null && (journal.product != id || journal.transaction != transaction))
            { Message = "Purchase record mismatch. Contact support."; yield break; }
            if (journal != null && journal.phase == 1)
            { Message = "Payment needs server reconciliation. Contact support; do not repurchase."; yield break; }
            if (journal == null)
            {
                if (intent == null || intent.product != id ||
                    (!string.IsNullOrEmpty(intent.transaction) && intent.transaction != transaction))
                { Message = "Purchase ownership could not be matched. Contact support."; yield break; }
                intent.transaction = transaction; Save(IntentKey, intent);
                if (string.IsNullOrEmpty(order.Info.Receipt))
                { Message = "Purchase receipt missing. Contact support."; yield break; }
                journal = new Journal { user = owner, product = id, transaction = transaction, phase = 1 };
                Save(key, journal); // Never blindly retry an uncertain additive backend request.
                Message = "Verifying purchase...";
                bool accepted = false;
                yield return KeyShopBackend.Buy(config, owner, token, amount, intent.price, intent.currency,
                    order.Info.Receipt, transaction, ok => accepted = ok);
                if (!accepted)
                { Message = "Could not verify delivery. Purchase kept pending; contact support."; yield break; }
                journal.phase = 2; Save(key, journal);
            }
            bool refreshed = false;
            yield return KeyShopBackend.Balance(config, token,
                total => { CacheBalance(owner, total); refreshed = true; }, () => { });
            if (!refreshed)
            { Message = "Payment recorded. Reopen the shop to refresh keys and finish delivery."; yield break; }
            Message = "Keys updated. Finishing purchase...";
            store.ConfirmPurchase(order);
        }
        finally { processing = false; }
    }
    private static void CacheBalance(int user, int total)
    {
        PlayerPrefs.SetInt("BlockPuzzle.Keys." + user, total);
        if (User == user) PlayerPrefs.SetInt("PlayerKeys", total);
        PlayerPrefs.Save();
    }
    private void PurchaseConfirmed(Order order)
    {
        if (order is ConfirmedOrder)
        {
            ClearConfirmedIntent(order);
            Intent intent = Read<Intent>(IntentKey);
            if (string.IsNullOrEmpty(order.Info.TransactionID) && intent != null &&
                !string.IsNullOrEmpty(intent.transaction))
            {
                Journal journal = Read<Journal>(JournalKey(intent.transaction));
                if (journal != null && journal.phase == 2)
                {
                    pending.Remove(intent.transaction);
                    PlayerPrefs.DeleteKey(IntentKey); PlayerPrefs.Save();
                }
            }
            Message = "Purchase complete.";
        }
        else Message = "Keys recorded; store confirmation needs retry. Reopen the shop.";
    }
    private void ClearConfirmedIntent(Order order)
    {
        string tx = order.Info.TransactionID;
        if (string.IsNullOrEmpty(tx)) return;
        Journal journal = Read<Journal>(JournalKey(tx));
        if (journal == null || journal.phase != 2) return;
        pending.Remove(tx);
        Intent intent = Read<Intent>(IntentKey);
        if (intent != null && intent.transaction == tx)
        { PlayerPrefs.DeleteKey(IntentKey); PlayerPrefs.Save(); }
    }
    private void PurchaseFailed(FailedOrder order)
    {
        // Do not clear an intent whose payment has already reached fulfillment.
        Intent intent = Read<Intent>(IntentKey);
        if (intent != null && string.IsNullOrEmpty(intent.transaction))
        { PlayerPrefs.DeleteKey(IntentKey); PlayerPrefs.Save(); }
        Message = "Purchase cancelled or failed. No keys added.";
    }
    private void PurchaseDeferred(DeferredOrder order)
    { Message = "Purchase awaiting store approval. No keys added yet."; }
    public void RefreshShop()
    {
        if (!connected) { Connect(); return; }
        if (processing) return;
        fetched = false;
        attemptedThisSession.Clear(); // Phase-1 journals still block repeat POSTs.
        if (products.Count == 0) FetchProducts();
        else store.FetchPurchases();
        RefreshBalance();
    }
    private void RefreshBalance()
    {
        if (fetchingBalance || processing || !Authenticated || !BackendReady) return;
        StartCoroutine(FetchBalance());
    }
    private IEnumerator FetchBalance()
    {
        fetchingBalance = true;
        int user = User;
        try
        {
            yield return KeyShopBackend.Balance(config, Token,
                total => CacheBalance(user, total),
                () => { if (User == user && pending.Count == 0) Message = "Could not refresh keys. Showing saved balance."; });
        }
        finally { fetchingBalance = false; }
    }
    private void OnDestroy()
    {
        if (Instance != this) return;
        if (store != null)
        {
            store.OnProductsFetched -= ProductsFetched; store.OnProductsFetchFailed -= ProductsFailed;
            store.OnPurchasesFetched -= PurchasesFetched; store.OnPurchasesFetchFailed -= PurchasesFailed;
            store.OnPurchasePending -= PurchasePending; store.OnPurchaseFailed -= PurchaseFailed;
            store.OnPurchaseDeferred -= PurchaseDeferred; store.OnPurchaseConfirmed -= PurchaseConfirmed;
            store.OnStoreDisconnected -= Disconnected;
        }
        Instance = null;
    }
}