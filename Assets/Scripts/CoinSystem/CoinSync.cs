using System.Collections;
using UnityEngine;

// One dedicated persistent ROOT object, with AppConfig assigned.
public class CoinSync : MonoBehaviour
{
    public static CoinSync Instance { get; private set; }
    public AppConfig config;
    [Min(0f)] public float batchDelay = 1f;
    [Min(5f)] public float refreshInterval = 30f;
    // Covers both GET and POST so leaderboard callers wait for the complete sync.
    public bool IsSending { get; private set; }
    public string LastMessage { get; private set; }
    private float nextAttempt, nextRefresh;
    private int observedUser = -1;
    private string observedToken;
    private bool needsRefresh = true;
    private int authVersion;
    private string Token => PlayerPrefs.GetString("AccessToken", "");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LeaderboardAPI.Config = config;
    }
    public void RequestSync()
    {
        nextAttempt = Mathf.Min(nextAttempt, Time.unscaledTime + Mathf.Max(0f, batchDelay));
    }
    private void ObserveAccount()
    {
        if (observedUser == CoinWallet.UserId && observedToken == Token) return;
        observedUser = CoinWallet.UserId;
        observedToken = Token;
        authVersion++;
        needsRefresh = true;
        nextAttempt = 0f;
        LastMessage = "";
        CoinWallet.RefreshActiveAccount();
    }
    private bool IsCurrent(int user, string token, int version)
    {
        return this != null && version == authVersion && CoinWallet.HasAccount &&
            CoinWallet.UserId == user && Token == token;
    }
    private void Update()
    {
        ObserveAccount();
        if (IsSending || Time.unscaledTime < nextAttempt) return;
        nextAttempt = Time.unscaledTime + Mathf.Max(0.25f, batchDelay);
        SyncNow();
    }
    public void SyncNow()
    {
        ObserveAccount();
        if (IsSending || !CoinWallet.HasAccount || config == null ||
            string.IsNullOrWhiteSpace(config.gameBaseAPIUrl) || string.IsNullOrWhiteSpace(Token)) return;
        LeaderboardAPI.Config = config;
        CoinWallet.Record record = CoinWallet.Read(CoinWallet.UserId);
        if (record.inFlight > 0)
        {
            LastMessage = "Saved progress kept. A previous score upload needs server verification.";
            return;
        }
        if (!needsRefresh && record.pending <= 0 && Time.unscaledTime < nextRefresh) return;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        { LastMessage = "Offline. Showing saved progress; score will sync when connected."; return; }
        StartCoroutine(Synchronize(CoinWallet.UserId, Token, authVersion));
    }
    private IEnumerator Synchronize(int user, string token, int version)
    {
        IsSending = true;
        try
        {
            // Fetch first on login. Rewards earned during GET remain in pending.
            if (needsRefresh || Time.unscaledTime >= nextRefresh)
            {
                yield return FetchTotal(user, token, version);
                if (!IsCurrent(user, token, version)) yield break;
                if (needsRefresh) yield break;
            }
            if (!IsCurrent(user, token, version)) yield break;
            int amount;
            string attempt;
            if (!CoinWallet.BeginSubmission(user, out amount, out attempt)) yield break;
            LastMessage = "Syncing earned coins...";
            bool success = false;
            yield return LeaderboardAPI.SubmitForAccount(user, token, amount,
                () => success = true, () => success = false);
            // Acknowledge the captured account even if the player signed out meanwhile.
            if (success) CoinWallet.Acknowledge(user, attempt);
            if (!IsCurrent(user, token, version)) yield break;
            if (!success)
            {
                LastMessage = "Saved progress kept. Score upload could not be confirmed.";
                Debug.LogWarning("[Coins] " + LastMessage, this);
                yield break;
            }
            needsRefresh = true;
            yield return FetchTotal(user, token, version);
        }
        finally
        {
            IsSending = false;
            // GET failures may be retried; uncertain POSTs are never replayed blindly.
            nextAttempt = Time.unscaledTime + (needsRefresh ? 5f : Mathf.Max(0.25f, batchDelay));
        }
    }
    private IEnumerator FetchTotal(int user, string token, int version)
    {
        needsRefresh = true;
        LastMessage = "Loading your saved score...";
        int total = -1;
        string error = null;
        yield return LeaderboardAPI.FetchTotalForAccount(user, token,
            value => total = value, message => error = message);
        if (!IsCurrent(user, token, version)) yield break;
        if (total < 0 || !CoinWallet.ApplyServerTotal(user, total))
        {
            LastMessage = error ?? "Could not reconcile your score. Saved progress kept.";
            Debug.LogWarning("[Coins] " + LastMessage, this);
            yield break;
        }
        needsRefresh = false;
        nextRefresh = Time.unscaledTime + Mathf.Max(5f, refreshInterval);
        LastMessage = "";
        Debug.Log("[Coins] Server total loaded; Home balance refreshed.", this);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { Instance = null; }
}
