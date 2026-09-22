using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Language is selected in Bootstrap. This scene owns tutorial play and completion only.
public class TutorialOnboardingFlow : MonoBehaviour
{
    public BoardManager board;
    public DestroyManager destroyManager;
    public RegistrationController registration;
    public GameObject handGuide;
    public GameObject languagePanel; // Legacy reference: hidden, because Bootstrap owns selection.
    public Behaviour[] gameplayInputs;
    public float celebrationSeconds = 1.2f;
    public bool freezeGameplayBehindForms = true;
    public bool observeBoardAutomatically = true;
    private bool ready, sawFilledBoard, finishing, ownsPause;
    private float previousTimeScale;
    private readonly Dictionary<Behaviour, bool> inputStates = new Dictionary<Behaviour, bool>();

    private void Awake()
    {
        if (languagePanel != null) languagePanel.SetActive(false);
        if (handGuide != null) handGuide.SetActive(false);
        if (gameplayInputs != null) foreach (Behaviour input in gameplayInputs) LockInput(input);
    }
    private void Start()
    {
        if (board == null) board = BoardManager.ins;
        if (destroyManager == null) destroyManager = DestroyManager.ins;
        if (registration == null) registration = RegistrationController.Instance;
        LockInput(InputManager.ins);
        if (!PlayerPrefs.HasKey("SelectedLanguage"))
        {
            Debug.LogError("Start from Bootstrap and select a language before playing the tutorial.", this);
            return;
        }
        ready = true;
        foreach (var entry in inputStates) if (entry.Key != null) entry.Key.enabled = entry.Value;
        if (handGuide != null) handGuide.SetActive(true);
    }
    private void LockInput(Behaviour input)
    {
        if (input == null || input == this || input is RegistrationController || inputStates.ContainsKey(input)) return;
        inputStates.Add(input, input.enabled); input.enabled = false;
    }
    private void Update()
    {
        if (!ready || finishing || !observeBoardAutomatically || board == null || destroyManager == null) return;
        int occupied = 0;
        foreach (BlockTile tile in board.boardBlocks) if (tile != null) occupied++;
        if (occupied > 0) sawFilledBoard = true;
        if (sawFilledBoard && occupied == 0 && !destroyManager.IsClearing) OnTutorialCompleted();
    }
    public void OnTutorialCompleted()
    {
        if (!ready || finishing) return;
        if (registration == null) { Debug.LogError("Assign Tutorial RegistrationController.", this); return; }
        finishing = true;
        StartCoroutine(Finish());
    }
    private IEnumerator Finish()
    {
        if (handGuide != null) handGuide.SetActive(false);
        if (InputManager.ins != null) InputManager.ins.enabled = false;
        if (gameplayInputs != null) foreach (Behaviour input in gameplayInputs)
            if (input != null && input != this && !(input is RegistrationController)) input.enabled = false;
        PlayerPrefs.SetInt("TutorialFinished", 1); PlayerPrefs.Save();
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, celebrationSeconds));
        if (freezeGameplayBehindForms)
        { previousTimeScale = Time.timeScale; ownsPause = true; Time.timeScale = 0f; }
        registration.OpenRegistration();
    }
    // Retained for an existing welcome button callback; use just one binding.
    public void OnStartClicked() { if (registration != null) registration.OnStartClicked(); }
    private void OnDestroy() { if (ownsPause) Time.timeScale = previousTimeScale; }
}