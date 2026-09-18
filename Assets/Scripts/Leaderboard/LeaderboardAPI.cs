using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class LeaderboardAPI
{
    public static AppConfig Config;
    public const string GameType = "blockpuzzle";
    [Serializable] public class ScoreSubmission { public int userId, points; public string platformName, gameType; }
    [Serializable] public class LeaderboardEntryAPI { public int placement, userId, points; public string username, country; }
    [Serializable] public class LeaderboardRoot { public List<LeaderboardEntryAPI> leaderboard; }
    private static bool Ready(out string error)
    {
        error = "";
        if (Config == null || string.IsNullOrWhiteSpace(Config.gameBaseAPIUrl)) error = "Assign AppConfig to CoinSync.";
        else if (PlayerPrefs.GetInt("UserId", 0) <= 0 || string.IsNullOrWhiteSpace(PlayerPrefs.GetString("AccessToken", ""))) error = "Please log in to view or submit rankings.";
        return error.Length == 0;
    }
    public static IEnumerator FetchLeaderboard(Action<List<LeaderboardEntryAPI>> onSuccess, Action<string> onFailure)
    {
        string error;
        if (!Ready(out error)) { onFailure?.Invoke(error); yield break; }
        int account = PlayerPrefs.GetInt("UserId", 0);
        string url = Config.gameBaseAPIUrl.Trim().TrimEnd('/') + "/api/leaderboard?platformName=mobile&gameType=" + GameType + "&limit=20";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 20;
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AccessToken", ""));
            yield return request.SendWebRequest();
            if (account != PlayerPrefs.GetInt("UserId", 0)) { onFailure?.Invoke("Account changed. Reopen the leaderboard."); yield break; }
            if (request.result != UnityWebRequest.Result.Success)
            { onFailure?.Invoke("Could not load leaderboard. Please try again."); yield break; }
            LeaderboardRoot root = null;
            try { root = JsonUtility.FromJson<LeaderboardRoot>(request.downloadHandler.text); }
            catch (Exception) { }
            if (root == null || root.leaderboard == null)
            { onFailure?.Invoke("Unexpected leaderboard response."); yield break; }
            onSuccess?.Invoke(root.leaderboard);
        }
    }
    public static IEnumerator SubmitScore(int points, Action onSuccess, Action onFailure)
    {
        string error;
        if (points <= 0 || !Ready(out error)) { onFailure?.Invoke(); yield break; }
        int user = PlayerPrefs.GetInt("UserId", 0);
        string token = PlayerPrefs.GetString("AccessToken", "");
        yield return SubmitForAccount(user, token, points, onSuccess, onFailure);
    }
    // Captured identity prevents a scene/account change from sending another player's reward.
    public static IEnumerator SubmitForAccount(int user, string token, int points, Action onSuccess, Action onFailure)
    {
        if (Config == null || string.IsNullOrWhiteSpace(Config.gameBaseAPIUrl) || user <= 0 || points <= 0 || string.IsNullOrWhiteSpace(token))
        { onFailure?.Invoke(); yield break; }
        ScoreSubmission data = new ScoreSubmission { userId = user, points = points, platformName = "mobile", gameType = GameType };
        string url = Config.gameBaseAPIUrl.Trim().TrimEnd('/') + "/api/leaderboard/points";
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.timeout = 20;
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();
            // Preserves instructor's HTTP-success contract. Never log tokens or raw account data.
            if (request.result == UnityWebRequest.Result.Success) onSuccess?.Invoke();
            else onFailure?.Invoke();
        }
    }
}
// Preserved public helper for other instructor scripts.
public static class JsonHelper
{
    public static List<T> FromJson<T>(string json)
    { Wrapper<T> result = JsonUtility.FromJson<Wrapper<T>>("{\"array\":" + json + "}"); return result != null ? result.array : new List<T>(); }
    [Serializable] private class Wrapper<T> { public List<T> array; }
}
