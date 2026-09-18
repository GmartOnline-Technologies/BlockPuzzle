using TMPro;
using UnityEngine;

// Add to each account coin label, except GameManager.scoreText which owns its flying animation.
[RequireComponent(typeof(TMP_Text))]
public class CoinBalanceText : MonoBehaviour
{
    private TMP_Text label;
    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        CoinWallet.Changed += Refresh;
        Refresh(CoinWallet.Total);
    }
    private void Refresh(int total) { if (label != null) label.text = total.ToString(); }
    private void OnDisable() { CoinWallet.Changed -= Refresh; }
}
