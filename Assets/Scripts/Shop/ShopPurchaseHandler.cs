using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Serialization;

// Instructor flow: grant locally, then send the receipt to the backend.
public class ShopPurchaseHandler : MonoBehaviour
{
    public AppConfig config;

    [Header("Package Details")]
    public string productId = "com.gmartonline.blockpuzzle50keys";
    [FormerlySerializedAs("coinsAmount")]
    public int keysAmount = 50;
    public float priceLKR;

    private void Awake()
    {
        if (config != null) EconomyAPI.Config = config;
    }

    // Connect ONLY to the IAP Button purchase fulfillment event.
    // Do not connect to the ordinary Button.onClick event.
    public void OnPurchaseSuccess(Product product)
    {
        if (product == null || product.definition.id != productId)
        {
            Debug.LogError("Purchase product does not match this key bundle.", this);
            return;
        }
        int userId = PlayerPrefs.GetInt("UserId", 0);
        if (userId <= 0 || keysAmount <= 0 || EconomyAPI.Config == null)
        {
            Debug.LogError("Key purchase needs a logged-in player, AppConfig and positive Keys Amount.", this);
            return;
        }

        string receipt = product.receipt;
        string transactionId = product.transactionID;

        // Keep the existing per-player key balance and legacy HUD mirror.
        string balanceKey = "BlockPuzzle.Keys." + userId;
        int balance = PlayerPrefs.GetInt(balanceKey, 0) + keysAmount;
        PlayerPrefs.SetInt(balanceKey, balance);
        PlayerPrefs.SetInt("PlayerKeys", balance);
        PlayerPrefs.Save();
        // Existing KeyBalanceText components refresh their labels each frame.

        StartCoroutine(EconomyAPI.BuyGems(keysAmount, priceLKR, receipt, transactionId,
            onSuccess: () => Debug.Log("Key purchase server request succeeded."),
            onFailure: () => Debug.LogError("Key purchase server request failed. Local keys were already added.")));
    }
}
