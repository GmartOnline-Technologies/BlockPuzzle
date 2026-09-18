using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardRowUI : MonoBehaviour
{
    public TMP_Text rankText, playerNameText, pointsText;
    public Image avatar;
    public Sprite defaultAvatar;
    public void Bind(string rank, string playerName, int points)
    {
        if (rankText != null) rankText.text = rank;
        if (playerNameText != null) playerNameText.text = playerName;
        if (pointsText != null) pointsText.text = points.ToString();
        if (avatar != null && defaultAvatar != null) avatar.sprite = defaultAvatar;
    }
    public void Clear()
    {
        if (rankText != null) rankText.text = "—";
        if (playerNameText != null) playerNameText.text = "—";
        if (pointsText != null) pointsText.text = "—";
    }
}
