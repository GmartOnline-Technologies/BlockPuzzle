using UnityEngine;
public class LeaderboardShopButton : MonoBehaviour
{
    public HomeSceneController home;
    public LeaderboardController leaderboard;
    public void OpenShop()
    {
        if (home == null || leaderboard == null || home.IsTransitioning) return;
        if (home.shopPanel == null || home.homePanel == null)
        { Debug.LogError("Assign Home and Shop panels on HomeSceneController.", this); return; }
        leaderboard.Close();
        home.OnKeyStoreClicked();
    }
}
