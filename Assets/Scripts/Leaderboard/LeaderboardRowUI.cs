using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardRowUI : MonoBehaviour
{
    [Tooltip("Assign the whole player card: frame, avatar, rank, name and score.")]
    public GameObject cardRoot;
    private bool hasEntry;

    private GameObject Card => cardRoot != null ? cardRoot : gameObject;

    private void OnEnable()
    {
        // Also hide never-bound cards if a parent panel activates them.
        if (!hasEntry) Card.SetActive(false);
    }

    [ContextMenu("Test Hide Card")]
    private void TestHideCard() { Clear(); }

    public TMP_Text rankText, playerNameText, pointsText;
    public Image avatar;
    public Sprite defaultAvatar;
    public void Bind(string rank, string playerName, int points)
    {
        hasEntry = true;
        if (rankText != null) rankText.text = rank;
        if (playerNameText != null) playerNameText.text = playerName;
        if (pointsText != null) pointsText.text = points.ToString();
        if (avatar != null && defaultAvatar != null) avatar.sprite = defaultAvatar;
        Card.SetActive(true);
    }
    public void Clear()
    {
        hasEntry = false;
        if (rankText != null) rankText.text = "—";
        if (playerNameText != null) playerNameText.text = "—";
        if (pointsText != null) pointsText.text = "—";
        Card.SetActive(false);
    }
}
