using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Keep on an always-active object OUTSIDE Home_Panel and Leaderboard_panel.
public class LeaderboardController : MonoBehaviour
{
    public HomeSceneController home;
    public GameObject homePanel, leaderboardPanel;
    public Button backButton;
    public LeaderboardRowUI firstPlace, secondPlace, thirdPlace;
    [Tooltip("Existing scene rows, in order: ranks 4, 5, 6, 7, 8, 9, 10.")]
    public LeaderboardRowUI[] otherPlayers = new LeaderboardRowUI[7];

    [Header("Leaderboard Loading")]
    [Tooltip("The loading bar root inside the leaderboard panel; not the whole leaderboard.")]
    public GameObject loadingPanel;
    public LoadingScreen loadingScreen;

    private Coroutine routine;
    private bool opened;
    private int displayedAccount;
    private int requestVersion;

    private void Awake()
    {
        HideLoading();
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (backButton != null) backButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (opened || (home != null && home.IsTransitioning)) return;
        if (homePanel == null || leaderboardPanel == null)
        { Debug.LogError("Assign Home Panel and Leaderboard Panel.", this); return; }

        opened = true;
        displayedAccount = CoinWallet.UserId;
        ClearRows();
        if (home != null) home.SetModalOpen(true);
        homePanel.SetActive(false);
        leaderboardPanel.SetActive(true);
        leaderboardPanel.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        CancelLoad();
        opened = false;
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
        if (home != null) { home.SetModalOpen(false); home.RefreshBalances(); }
    }

    // Kept for compatibility. Open calls this automatically; no refresh button needed.
    public void Refresh()
    {
        if (!opened || routine != null) return;
        displayedAccount = CoinWallet.UserId;
        ClearRows();
        int version = ++requestVersion;
        routine = StartCoroutine(Load(version));
    }

    private IEnumerator Load(int version)
    {
        BeginLoading();
        try
        {
            yield return null; // Assign the coroutine handle before any immediate failure.
            if (!IsCurrent(version)) yield break;
            if (CoinSync.Instance != null)
            {
                CoinSync.Instance.SyncNow();
                while (CoinSync.Instance != null && CoinSync.Instance.IsSending)
                {
                    if (!IsCurrent(version)) yield break;
                    yield return null;
                }
            }
            if (!IsCurrent(version)) yield break;
            if (loadingScreen != null) loadingScreen.SetProgress(0.85f);
            List<LeaderboardAPI.LeaderboardEntryAPI> loadedEntries = null;
            bool succeeded = false;
            yield return LeaderboardAPI.FetchLeaderboard(
                entries =>
                {
                    if (!IsCurrent(version)) return;
                    loadedEntries = entries;
                    succeeded = true;
                },
                error => { if (IsCurrent(version)) Debug.LogWarning(error, this); });
            if (!IsCurrent(version)) yield break;
            if (succeeded)
            {
                if (loadingScreen != null && loadingScreen.isActiveAndEnabled)
                    yield return loadingScreen.CompleteLoading();
                if (IsCurrent(version)) ShowEntries(loadedEntries);
            }
        }
        finally
        {
            if (version == requestVersion)
            {
                HideLoading();
                routine = null;
            }
        }
    }

    private void BeginLoading()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            loadingPanel.transform.SetAsLastSibling();
        }
        if (loadingScreen != null)
        {
            loadingScreen.BeginLoading();
            // Visual stages, not a measurement of downloaded bytes.
            loadingScreen.SetProgress(0.35f);
        }
    }

    private void HideLoading()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private bool IsCurrent(int version)
    {
        return opened && version == requestVersion && displayedAccount == CoinWallet.UserId;
    }

    private LeaderboardRowUI RowForRank(int rank)
    {
        if (rank == 1) return firstPlace;
        if (rank == 2) return secondPlace;
        if (rank == 3) return thirdPlace;
        int index = rank - 4;
        return rank <= 10 && index >= 0 && otherPlayers != null && index < otherPlayers.Length
            ? otherPlayers[index] : null;
    }

    private void ClearRows()
    {
        for (int rank = 1; rank <= 10; rank++)
        {
            LeaderboardRowUI row = RowForRank(rank);
            if (row != null) row.Clear();
        }
    }

    private void ShowEntries(List<LeaderboardAPI.LeaderboardEntryAPI> entries)
    {
        if (entries == null) return;
        var ranks = new HashSet<int>();
        var users = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (entry == null || entry.placement < 1 || entry.placement > 10 ||
                ranks.Contains(entry.placement) || users.Contains(entry.userId)) continue;
            ranks.Add(entry.placement);
            users.Add(entry.userId);
            LeaderboardRowUI row = RowForRank(entry.placement);
            if (row != null) row.Bind(entry.placement.ToString(), entry.username, entry.points);
        }
    }

    private void CancelLoad()
    {
        requestVersion++;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        HideLoading();
    }
    private void OnDisable() { CancelLoad(); opened = false; }
    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(Close);
    }
}
