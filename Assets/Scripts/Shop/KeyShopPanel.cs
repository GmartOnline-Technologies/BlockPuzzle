using TMPro;
using UnityEngine;

// Attach to Shop_panel. A small message label displays loading/payment/failure states.
public class KeyShopPanel : MonoBehaviour
{
    public TMP_Text messageText;
    private void OnEnable()
    {
        if (KeyShopService.Instance != null) KeyShopService.Instance.RefreshShop();
    }
    private void Update()
    {
        if (messageText != null) messageText.text = KeyShopService.Instance != null
            ? KeyShopService.Instance.Message : "Shop service is not configured.";
    }
}
