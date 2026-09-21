using UnityEngine;
using UnityEngine.SceneManagement;

// Dedicated top-level object; survives scene changes without restarting the same BGM.
public class BlockPuzzleAudio : MonoBehaviour
{
    public static BlockPuzzleAudio Instance { get; private set; }
    public enum Effect { Button, Place, InvalidPlacement, LineClear, Hammer, Rotate, Undo, GameOver }
    [Header("Background music")]
    public AudioClip menuBgm, gameBgm;
    public string gameSceneName = "GameScene";
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Header("Sound effects")]
    public AudioClip buttonClick, blockPlace, invalidPlacement, lineClear;
    public AudioClip hammer, rotate, undo, gameOver;
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    private AudioSource music, effects;
    private bool subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        if (transform.parent != null)
        { Debug.LogError("Put BlockPuzzleAudio on a dedicated top-level GameObject.", this); enabled = false; return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = effects.playOnAwake = false;
        music.spatialBlend = effects.spatialBlend = 0f;
        music.loop = true;
        music.priority = 0;
        ApplySettings();
    }
    private void OnEnable()
    {
        if (Instance != this) return;
        SceneManager.activeSceneChanged += SceneChanged;
        subscribed = true;
        SelectMusic(SceneManager.GetActiveScene().name);
    }
    private void SceneChanged(Scene oldScene, Scene newScene) { SelectMusic(newScene.name); }
    private void SelectMusic(string sceneName)
    {
        AudioClip target = sceneName == gameSceneName ? gameBgm : menuBgm;
        if (music.clip == target)
        { if (target != null && !music.isPlaying) music.Play(); return; }
        music.Stop(); music.clip = target;
        ApplySettings();
        if (target != null) music.Play();
    }
    // Also picks up existing scripts that directly write the same preferences.
    private void Update() { ApplySettings(); }
    public void ApplySettings()
    {
        if (music == null || effects == null) return;
        music.mute = PlayerPrefs.GetInt("GameMusic", 1) == 0;
        effects.mute = PlayerPrefs.GetInt("GameSound", 1) == 0;
        music.volume = musicVolume;
        effects.volume = soundVolume;
    }
    public static void Play(Effect effect)
    {
        if (Instance != null) Instance.PlayEffect(effect);
    }
    public void PlayEffect(Effect effect)
    {
        if (!isActiveAndEnabled || effects == null || PlayerPrefs.GetInt("GameSound", 1) == 0) return;
        AudioClip clip = null;
        switch (effect)
        {
            case Effect.Button: clip = buttonClick; break;
            case Effect.Place: clip = blockPlace; break;
            case Effect.InvalidPlacement: clip = invalidPlacement; break;
            case Effect.LineClear: clip = lineClear; break;
            case Effect.Hammer: clip = hammer; break;
            case Effect.Rotate: clip = rotate; break;
            case Effect.Undo: clip = undo; break;
            case Effect.GameOver: clip = gameOver; break;
        }
        ApplySettings();
        if (clip != null) effects.PlayOneShot(clip);
    }
    private void OnDisable()
    {
        if (subscribed) { SceneManager.activeSceneChanged -= SceneChanged; subscribed = false; }
        if (music != null) music.Stop();
        if (effects != null) effects.Stop();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
