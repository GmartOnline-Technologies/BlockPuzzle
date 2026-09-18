using System.Collections;
using UnityEngine;

// Put on a dedicated ROOT object; it survives scene changes, not the whole Canvas.
public class CoinSync : MonoBehaviour
{
    public static CoinSync Instance { get; private set; }
    public AppConfig config;
    [Min(0f)] public float batchDelay = 1f;
    public bool IsSending { get; private set; }
    public string LastMessage { get; private set; }
    private float nextAttempt;
    private int observedUser = -1;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LeaderboardAPI.Config = config;
    }
    public void RequestSync() { nextAttempt = Mathf.Min(nextAttempt, Time.unscaledTime + Mathf.Max(0f, batchDelay)); }
    private void Update()
    {
        if (observedUser != CoinWallet.UserId)
        {
            observedUser = CoinWallet.UserId; LastMessage = "";
            CoinWallet.RefreshActiveAccount();
            nextAttempt = Time.unscaledTime + Mathf.Max(0f, batchDelay);
        }
        if (IsSending || Time.unscaledTime < nextAttempt) return;
        nextAttempt = Time.unscaledTime + Mathf.Max(0.25f, batchDelay);
        SyncNow();
    }
    public void SyncNow()
    {
        if (IsSending) return;
        if (!CoinWallet.HasAccount || config == null || string.IsNullOrWhiteSpace(config.gameBaseAPIUrl) || string.IsNullOrWhiteSpace(PlayerPrefs.GetString("AccessToken", ""))) return;
        CoinWallet.Record value = CoinWallet.Read(CoinWallet.UserId);
        if (value.inFlight > 0)
        { LastMessage = "Coins saved locally. A previous score submission needs server verification."; return; }
        if (value.pending <= 0) { LastMessage = ""; return; }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        { LastMessage = "Coins saved locally. Connect to upload your score."; return; }
        StartCoroutine(Send());
    }
    private IEnumerator Send()
    {
        int user = CoinWallet.UserId;
        string token = PlayerPrefs.GetString("AccessToken", "");
        int amount; string attempt;
        if (!CoinWallet.BeginSubmission(user, out amount, out attempt)) yield break;
        IsSending = true; LastMessage = "Syncing earned coins...";
        bool success = false;
        try
        {
            yield return LeaderboardAPI.SubmitForAccount(user, token, amount, () => success = true, () => success = false);
            if (success) CoinWallet.Acknowledge(user, attempt);
            if (user == CoinWallet.UserId)
                LastMessage = success ? "" : "Coins saved locally. Score submission could not be confirmed.";
        }
        finally { IsSending = false; }
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { Instance = null; }
}
