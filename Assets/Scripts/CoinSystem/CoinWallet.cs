using System;
using UnityEngine;

// One local ledger per backend user id. Tutorial/guest play does not earn account coins.
public static class CoinWallet
{
    [Serializable] public class Record
    {
        public int total;
        public int pending;
        public int inFlight;
        public string attemptId;
    }
    public static event Action<int> Changed;
    public static int UserId { get { return PlayerPrefs.GetInt("UserId", 0); } }
    public static bool HasAccount { get { return UserId > 0 && PlayerPrefs.GetInt("IsRegisteredUser", 0) == 1; } }
    private static string Key(int user) { return "BlockPuzzle.Coins.v1." + user; }
    public static int Total { get { return UserId > 0 ? Read(UserId).total : 0; } }
    public static Record Read(int user)
    {
        if (user <= 0 || !PlayerPrefs.HasKey(Key(user))) return new Record();
        try
        {
            Record value = JsonUtility.FromJson<Record>(PlayerPrefs.GetString(Key(user)));
            if (value != null && value.total >= 0 && value.pending >= 0 && value.inFlight >= 0) return value;
        }
        catch (Exception) { }
        // Do not replace corrupt saved data with a fresh record silently.
        throw new InvalidOperationException("Coin record is invalid. Preserve saved data and investigate before awarding coins.");
    }
    private static void Save(int user, Record value)
    {
        PlayerPrefs.SetString(Key(user), JsonUtility.ToJson(value));
        if (user == UserId) PlayerPrefs.SetInt("PlayerCoins", value.total); // Existing Home compatibility.
        PlayerPrefs.Save();
    }
    public static bool AddEarned(int amount)
    {
        if (amount <= 0 || !HasAccount) return false;
        int user = UserId; Record value = Read(user);
        if ((long)value.total + amount > int.MaxValue || (long)value.pending + amount > int.MaxValue)
        { Debug.LogError("Coin balance capacity reached; reward was not saved."); return false; }
        value.total += amount; value.pending += amount;
        Save(user, value);
        Changed?.Invoke(value.total);
        if (CoinSync.Instance != null) CoinSync.Instance.RequestSync();
        return true;
    }
    public static void RefreshActiveAccount()
    {
        int total = Total;
        PlayerPrefs.SetInt("PlayerCoins", total);
        Changed?.Invoke(total);
    }
    public static bool BeginSubmission(int user, out int amount, out string attempt)
    {
        amount = 0; attempt = null;
        Record value = Read(user);
        if (value.inFlight > 0 || value.pending <= 0) return false;
        amount = value.pending; attempt = Guid.NewGuid().ToString("N");
        value.pending = 0; value.inFlight = amount; value.attemptId = attempt;
        Save(user, value); // Persist before sending; a crash must not cause a blind duplicate retry.
        return true;
    }
    public static void Acknowledge(int user, string attempt)
    {
        if (!PlayerPrefs.HasKey(Key(user))) return; // Local deactivation may have removed the record.
        Record value = Read(user);
        if (value.attemptId != attempt) return;
        value.inFlight = 0; value.attemptId = null; Save(user, value);
    }
    // Only call after verifying this exact pending attempt against the backend.
    // Never attach this to a normal player retry button.
    public static void ResolveUncertainSubmission(int user, bool serverApplied)
    {
        Record value = Read(user);
        if (value.inFlight == 0) return;
        if (!serverApplied) value.pending = checked(value.pending + value.inFlight);
        value.inFlight = 0; value.attemptId = null; Save(user, value);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() { Changed = null; }
}
