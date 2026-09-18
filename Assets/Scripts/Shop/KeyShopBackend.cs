using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Uses the instructor's DTOs and endpoints. Purchased "coin" is displayed as keys.
public static class KeyShopBackend
{
    public static IEnumerator Buy(AppConfig config, int user, string token, int keys,
        float price, string currency, string receipt, string transaction, Action<bool> done)
    {
        var data = new EconomyAPI.GemPurchaseRequest {
            coin = keys, rs_value = price, currencyCode = currency,
            gametype = "blockpuzzle", platformname = "mobile",
            receiptToken = receipt, storeTransactionId = transaction
        };
        using (var request = new UnityWebRequest(config.gameBaseAPIUrl.Trim().TrimEnd('/') + "/api/gems/buy", "POST"))
        {
            request.timeout = 25;
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            EconomyAPI.GemPurchaseResponse response = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                try { response = JsonUtility.FromJson<EconomyAPI.GemPurchaseResponse>(request.downloadHandler.text); }
                catch (Exception) { }
            }
            var purchase = response != null ? response.purchase : null;
            done(purchase != null && purchase.userId == user && purchase.coin == keys &&
                purchase.gametype == "blockpuzzle" && purchase.platformname == "mobile" &&
                purchase.storeTransactionId == transaction);
        }
    }
    public static IEnumerator Balance(AppConfig config, string token, Action<int> success, Action failure)
    {
        using (var request = UnityWebRequest.Get(config.gameBaseAPIUrl.Trim().TrimEnd('/') +
            "/api/gems/coins?gametype=blockpuzzle&platformname=mobile"))
        {
            request.timeout = 25;
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();
            // Sentinel detects a missing field and accepts a legitimate zero balance.
            var response = new BalanceData { coin = -1, total = -1 };
            bool valid = false;
            if (request.result == UnityWebRequest.Result.Success)
            {
                try { JsonUtility.FromJsonOverwrite(request.downloadHandler.text, response); valid = true; }
                catch (Exception) { }
            }
            int balance = response.coin >= 0 ? response.coin : response.total;
            if (valid && balance >= 0) success(balance);
            else failure();
        }
    }
    [Serializable] private class BalanceData { public int coin, total; }
}
