using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager ins;

    [Header("Score UI")]
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameOverBestScoreText;
    public TextMeshProUGUI gameOverScoreText;

    [Header("Score animation")]
    [Min(0f)] public float scoreCountDuration = 0.35f;
    public ScoreFlyAnimation scoreFlyAnimation;

    private int landedScoreTarget;
    public int ScoreSessionVersion { get; private set; }

    [HideInInspector] public bool gameOver = false;
    [HideInInspector] public bool paused = false;
    [HideInInspector] public bool firstBeatenScore;
    [HideInInspector] public bool continueGame;
    [HideInInspector] public bool waitingForAd;
    [HideInInspector] public int bestScore;
    [HideInInspector] public int score = 0;

    public static int GetLineReward(int lines)
    {
        if (lines <= 0) return 0;
        if (lines == 1) return 50;
        if (lines == 2) return 150;
        if (lines == 3) return 300;
        return 500;
    }

    // Keep the original API for callers outside the line-clear manager.
    public void ChangePoints(int e, int l)
    {
        Vector3 origin = new Vector3((BoardManager.BOARD_SIZE - 1) * 0.5f,
            (BoardManager.BOARD_SIZE - 1) * 0.5f, -1f);
        AwardLineClear(l, origin);
    }

    public void AwardLineClear(int lines, Vector3 worldOrigin)
    {
        int points = GetLineReward(lines);
        if (points == 0 || gameOver) return;

        // The real total changes once. Presentation catches up when the reward lands.
        score += points;
        if (score > bestScore)
        {
            bestScore = score;
            firstBeatenScore = false;
        }

        int totalAfterClear = score;
        int session = ScoreSessionVersion;
        if (scoreFlyAnimation == null)
        {
            scoreFlyAnimation = GetComponent<ScoreFlyAnimation>();
            if (scoreFlyAnimation == null)
                scoreFlyAnimation = gameObject.AddComponent<ScoreFlyAnimation>();
        }

        bool started = scoreFlyAnimation.Play(points, worldOrigin, scoreText, () =>
        {
            if (this != null && session == ScoreSessionVersion)
                PresentScore(totalAfterClear);
        });
        if (!started) PresentScore(totalAfterClear);
    }

    private void PresentScore(int total)
    {
        // A late arrival from an older clear must never roll the counter backwards.
        landedScoreTarget = Mathf.Max(landedScoreTarget, total);
        AnimateCounter(scoreText, landedScoreTarget);
        AnimateCounter(bestScoreText, bestScore);
    }

    private void AnimateCounter(TextMeshProUGUI label, int total)
    {
        if (label == null) return;
        ScoreAddAnimation animation = label.GetComponent<ScoreAddAnimation>();
        if (animation == null) animation = label.gameObject.AddComponent<ScoreAddAnimation>();
        if (label.isActiveAndEnabled) animation.AnimateTo(total, scoreCountDuration);
        else animation.SetImmediate(total);
    }

    private void FinishScorePresentation()
    {
        if (scoreFlyAnimation != null) scoreFlyAnimation.CancelAll();
        landedScoreTarget = score;
        SetCounterImmediately(scoreText, score);
        SetCounterImmediately(bestScoreText, bestScore);
    }

    private void SetCounterImmediately(TextMeshProUGUI label, int total)
    {
        if (label == null) return;
        ScoreAddAnimation animation = label.GetComponent<ScoreAddAnimation>();
        if (animation != null) animation.SetImmediate(total);
        else label.text = total.ToString();
    }

    public void RestartGame()
    {
        gameOver = false;
        firstBeatenScore = continueGame = true;
        ScoreSessionVersion++;
        score = 0;
        FinishScorePresentation();

        for (int y = 0; y < BoardManager.BOARD_SIZE; y++)
        {
            for (int x = 0; x < BoardManager.BOARD_SIZE; x++)
            {
                if (BoardManager.ins.boardBlocks[x, y])
                {
                    Destroy(BoardManager.ins.boardBlocks[x, y].gameObject);
                    BoardManager.ins.boardBlocks[x, y] = null;
                }
            }
        }

        for (int i = 0; i < BoardManager.BLOCKS_AMOUNT; i++)
        {
            if (BoardManager.ins.blocks[i] != null)
                Destroy(BoardManager.ins.blocks[i].gameObject);
        
            int x = BoardManager.Rand(0, BoardManager.ins.blockPrefabs.Length);
            BoardManager.ins.blocks[i] = BoardManager.ins.SpawnBlock(i, x);
        }
    }

    public void PauseGame()
    {
        paused = true;
        Time.timeScale = 0.0f;
    }

    public void UnpauseGame()
    {
        paused = false;
        Time.timeScale = 1.0f;
    }

    public void SetGameOver()
    {
        gameOver = true;
        FinishScorePresentation();

        if (gameOverBestScoreText != null) gameOverBestScoreText.text = bestScore.ToString();
        if (gameOverScoreText != null) gameOverScoreText.text = score.ToString();
    }

    public IEnumerator WaitForFade()
    {
        gameOver = true;
        
        for (int y = BoardManager.BOARD_SIZE - 1; y >= 0; y--)
        {
            for (int x = 0; x < BoardManager.BOARD_SIZE; x++)
            {
                BlockTile b = BoardManager.ins.boardBlocks[x, y];
                if (b)
                    b.Fade(0.25f, new Color(0.09f, 0.122f, 0.153f));

                if (x % 2 == 0)
                   yield return new WaitForSeconds(0.01f);
            }
        }

        yield return new WaitForSeconds(0.25f);
    }

    public void ContinueGame()
    {
        gameOver = false;
        continueGame = false;
        ChangeBlocksColor();
    }

    public void DestroyBlocks()
    {
        int a = (int)Random.Range(0, BoardManager.BOARD_SIZE - 2.001f);
        if (BoardManager.ins.blocks[0].size.x >= BoardManager.ins.blocks[0].size.y)
        {
            for (int y = a; y < a + 3; y++)
            {
                for (int x = 0; x < BoardManager.BOARD_SIZE; x++)
                {
                    BlockTile b = BoardManager.ins.boardBlocks[x, y];
                    if (b) b.Destroy(0.25f);
                    BoardManager.ins.boardBlocks[x, y] = null;
                }
            }
        }
        else
        {
            for (int x = a; x < a + 3; x++)
            {
                for (int y = 0; y < BoardManager.BOARD_SIZE; y++)
                {
                    BlockTile b = BoardManager.ins.boardBlocks[x, y];
                    if (b) b.Destroy(0.25f);
                    BoardManager.ins.boardBlocks[x, y] = null;
                }
            }
        }
        
        BoardManager.ins.CheckBoard();
    }

    public void ChangeBlocksColor()
    {
        for (int y = 0; y < BoardManager.BOARD_SIZE; y++)
            for (int x = 0; x < BoardManager.BOARD_SIZE; x++)
                if (BoardManager.ins.boardBlocks[x, y])
                    BoardManager.ins.boardBlocks[x, y].Fade(0.0f, BoardManager.ins.boardBlocks[x, y].defaultColor);
    }

    private void Awake()
    {
        if (!ins) ins = this;
        Application.targetFrameRate = 60;
        bestScore = ProgressManager.GetBestScore();
        landedScoreTarget = score;
    }
}