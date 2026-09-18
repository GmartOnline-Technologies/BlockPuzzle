using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class EconomyAPI
{
    public static AppConfig Config;

    // PERFORMANCE FIX: Cache the URL to prevent repeated GC allocations from .Trim() in every method
    private static string _cachedBaseUrl;
    private static string BaseUrl 
    {
        get 
        {
            if (string.IsNullOrEmpty(_cachedBaseUrl) && Config != null)
                _cachedBaseUrl = Config.gameBaseAPIUrl.Trim();
            return _cachedBaseUrl;
        }
    }

    private static string GetToken() => PlayerPrefs.GetString("AccessToken", "");
    private static int GetUserId() => PlayerPrefs.GetInt("UserId", 0);

    // ==========================================
    // DATA TRANSFER OBJECTS (DTOs)
    // Note: C# naming conventions are intentionally bypassed here to match external JSON payload requirements.
    // ==========================================

    [Serializable]
    public class GemPurchaseRequest
    {
        public int coin;
        public float rs_value;
        public string gametype = "blockpuzzle";
        public string platformname = "mobile";
        public string currencyCode = "LKR";
        public string receiptToken;
        public string storeTransactionId;
    }

    [Serializable]
    public class AssetBuyRequest
    {
        public string type;
        public string name;
        public string id;
        public int coin;
        public string gametype; // Will be set dynamically to "arrowarena"
    }

    [Serializable]
    public class ConsumableValueRequest
    {
        public int user_id;
        public int value;
        public string type;   // "add" or "deduct"
    }

    [Serializable]
    public class TotalPointsResponse 
    { 
        public int userId; 
        public int totalPoints; 
        public string gameType; 
    }

    [Serializable]
    public class TotalGemsResponse 
    { 
        public int total; 
        public int coin; 
    }

    [Serializable]
    public class PurchaseData
    {
        public int id;
        public int userId;
        public int coin; 
        public float rs_value;
        public string gametype;
        public string platformname;
        public string currencyCode;
        public string receiptToken;
        public string storeTransactionId;
        public string datetime;
    }

    [Serializable]
    public class GemPurchaseResponse { public PurchaseData purchase; }

    [Serializable]
    public class AssetData
    {
        public int id;
        public int userId;
        public string type;
        public string name;
        public string assetId;
        public string gameType;
        public int coin;
        public string createdAt;
    }

    [Serializable]
    public class AssetBuyResponse { public AssetData purchase; }

    [Serializable]
    public class OwnedAssetsResponse
    {
        public int userId;
        public string gametype;
        public string platformname;
        public List<AssetData> assets;
    }

    [Serializable]
    private class AutoAimFetchData { public int value; }

    [Serializable]
    private class AutoAimFetchResponse { public AutoAimFetchData data; }

    // ==========================================
    // ARROW ARENA: CONSUMABLE ENDPOINTS
    // ==========================================

    /// <summary>
    /// Executes the two-step purchase flow for Arrow Arena: 1. Deduct Coins, 2. Add Item to Inventory
    /// </summary>
    // ARCHITECTURE FIX: Added 'amount' and 'totalCoinCost' to support bulk purchasing from the UI
    public static IEnumerator PurchaseArrowConsumable(string itemName, string itemId, int amount, int totalCoinCost, Action onSuccess, Action<string> onFailure)
    {
        AssetBuyRequest buyRequest = new AssetBuyRequest
        {
            type = "help", 
            name = itemName,
            id = itemId,
            coin = totalCoinCost, // Pass the bulk cost here
            gametype = "arrowarena"
        };

        string buyUrl = $"{BaseUrl}/api/assets/buy";
        string buyJsonBody = JsonUtility.ToJson(buyRequest);
        bool buySuccessful = false;
        string errorMessage = "";

        using (UnityWebRequest request = new UnityWebRequest(buyUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(buyJsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) buySuccessful = true;
            else errorMessage = $"Asset Buy Failed: {request.error}";
        }

        if (!buySuccessful)
        {
            onFailure?.Invoke(errorMessage);
            yield break; 
        }

        // ARCHITECTURE FIX: Pass the bulk 'amount' to the server instead of a hardcoded '1'
        yield return ModifyArrowConsumableValue(amount, "add", onSuccess, onFailure);
    }

    /// <summary>
    /// Modifies backend inventory values. Call with "add" after purchase, or "deduct" when used in gameplay.
    /// </summary>
    public static IEnumerator ModifyArrowConsumableValue(int amount, string transactionType, Action onSuccess, Action<string> onFailure)
    {
        string url = $"{BaseUrl}/api/users/value";
 
        ConsumableValueRequest data = new ConsumableValueRequest
        {
            user_id = GetUserId(),
            value = amount,
            type = transactionType 
        };
 
        string jsonBody = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());
 
            yield return request.SendWebRequest();
 
            if (request.result == UnityWebRequest.Result.Success) onSuccess?.Invoke();
            else onFailure?.Invoke($"Value Update Failed: {request.error} - {request.downloadHandler.text}");
        }
    }

    public static IEnumerator FetchArrowConsumableValue(Action<int> onSuccess, Action<string> onFailure)
    {
        string url = $"{BaseUrl}/api/users/value/{GetUserId()}";
 
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());
            yield return request.SendWebRequest();
 
            if (request.result == UnityWebRequest.Result.Success)
            {
                AutoAimFetchResponse response = JsonUtility.FromJson<AutoAimFetchResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response.data.value);
            }
            else
            {
                onFailure?.Invoke(request.error);
            }
        }
    }

    // ==========================================
    // LEGACY arrowarena / BASE ENDPOINTS
    // ==========================================

    public static IEnumerator FetchTotalCoins(Action<int> onSuccess, Action<string> onFailure)
    {
        string url = $"{BaseUrl}/api/users/points/total?gameType=arrowarena";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TotalPointsResponse response = JsonUtility.FromJson<TotalPointsResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response.totalPoints);
            }
            else
            {
                onFailure?.Invoke(request.error);
            }
        }
    }

    public static IEnumerator FetchTotalGems(Action<int> onSuccess, Action<string> onFailure)
    {
        string url = $"{BaseUrl}/api/gems/coins?gametype=blockpuzzle&platformname=mobile";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TotalGemsResponse response = JsonUtility.FromJson<TotalGemsResponse>(request.downloadHandler.text);
                int gems = response.coin > 0 ? response.coin : response.total; 
                onSuccess?.Invoke(gems);
            }
            else
            {
                onFailure?.Invoke(request.error);
            }
        }
    }

    public static IEnumerator BuyGems(int gemAmount, float rsValue, string receiptToken, string storeTransactionId, Action onSuccess, Action onFailure)
    {
        string url = $"{BaseUrl}/api/gems/buy";

        GemPurchaseRequest data = new GemPurchaseRequest
        {
            coin = gemAmount,
            rs_value = rsValue,
            receiptToken = receiptToken,
            storeTransactionId = storeTransactionId
        };

        string jsonBody = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success) onSuccess?.Invoke();
            else onFailure?.Invoke();
        }
    }

    public static IEnumerator FetchOwnedAssets(Action<OwnedAssetsResponse> onSuccess, Action<string> onFailure)
    {
        string url = $"{BaseUrl}/api/assets?gametype=arrowarena&platformname=mobile";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + GetToken());
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OwnedAssetsResponse response = JsonUtility.FromJson<OwnedAssetsResponse>(request.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onFailure?.Invoke(request.error);
            }
        }
    }
}