using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class KeyBalanceText : MonoBehaviour
{
    private TMP_Text label;
    private void Awake() { label = GetComponent<TMP_Text>(); }
    private void OnEnable() { Refresh(); }
    private void LateUpdate() { Refresh(); }
    private void Refresh()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        int user = PlayerPrefs.GetInt("UserId", 0);
        string value = (user > 0 ? PlayerPrefs.GetInt("BlockPuzzle.Keys." + user, 0) : 0).ToString();
        if (label.text != value) label.text = value;
    }
}
