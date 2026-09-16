using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-local bridge: observes the existing tutorial clear without changing game mechanics.
public class TutorialOnboardingFlow : MonoBehaviour
{
    public BoardManager board;
    public DestroyManager destroyManager;
    public RegistrationController registration;
    public GameObject handGuide;
    public GameObject languagePanel;
    public Behaviour[] gameplayInputs;
    public float celebrationSeconds = 1.2f;
    public bool freezeGameplayBehindForms = true;
    private bool ownsPause;
    private float previousTimeScale;
    public string homeSceneName = "HomeScene";
    public bool observeBoardAutomatically = true;
    private bool sawFilledBoard, finishing, loadingHome, languageSelected;
    private readonly Dictionary<Behaviour, bool> inputStates = new Dictionary<Behaviour, bool>();

    private void Awake()
    {
        PopupMotion.Show(languagePanel);
        if (handGuide != null) handGuide.SetActive(false);
        if (gameplayInputs != null)
            foreach (Behaviour input in gameplayInputs) LockInput(input);
    }

    private void LockInput(Behaviour input)
    {
        if (input == null || input == this || input == registration || inputStates.ContainsKey(input)) return;
        inputStates.Add(input, input.enabled);
        input.enabled = false;
    }

    public void OnLanguageSelected(int languageIndex)
    {
        if (languageSelected || finishing || languageIndex < 0 || languageIndex > 1) return;
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.SetLanguage(languageIndex);
        else
        {
            PlayerPrefs.SetInt("SelectedLanguage", languageIndex);
            PlayerPrefs.SetInt("HasSelectedLanguage", 1);
            PlayerPrefs.Save();
        }
        languageSelected = true;
        if (languagePanel != null) languagePanel.SetActive(false);
        foreach (var entry in inputStates)
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        if (handGuide != null) handGuide.SetActive(true);
    }

    private void Start()
    {
        if (board == null) board = BoardManager.ins;
        if (destroyManager == null) destroyManager = DestroyManager.ins;
        if (registration == null) registration = RegistrationController.Instance;
        if (!languageSelected) LockInput(InputManager.ins);
        if (languagePanel == null)
            Debug.LogError("Assign the Tutorial scene's Language Panel to TutorialOnboardingFlow.", this);
    }
    private void Update()
    {
        if (!languageSelected || !observeBoardAutomatically || finishing || board == null || destroyManager == null) return;
        int occupied = 0;
        foreach (BlockTile tile in board.boardBlocks) if (tile != null) occupied++;
        if (occupied > 0) sawFilledBoard = true;
        if (sawFilledBoard && occupied == 0 && !destroyManager.IsClearing) OnTutorialCompleted();
    }
    // Can also be called once by a custom tutorial completion event.
    public void OnTutorialCompleted()
    {
        if (!languageSelected || finishing || registration == null) return;
        finishing = true;
        StartCoroutine(Finish());
    }
    private IEnumerator Finish()
    {
        if (handGuide != null) handGuide.SetActive(false);
        if (InputManager.ins != null) InputManager.ins.enabled = false;
        if (gameplayInputs != null) foreach (Behaviour input in gameplayInputs) if (input != null) input.enabled = false;
        PlayerPrefs.SetInt("TutorialFinished", 1); PlayerPrefs.Save();
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, celebrationSeconds));
        if (freezeGameplayBehindForms)
        {
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0f;
        }
        registration.OpenRegistration();
    }
    private void OnDestroy()
    {
        if (ownsPause) Time.timeScale = previousTimeScale;
    }
    public void OnStartClicked()
    {
        if (loadingHome || registration == null || !registration.IsAuthenticated) return;
        if (!Application.CanStreamedLevelBeLoaded(homeSceneName))
        { Debug.LogError("Add HomeScene to the build scene list.", this); return; }
        loadingHome = true; Time.timeScale = 1f; SceneManager.LoadSceneAsync(homeSceneName);
    }
}
