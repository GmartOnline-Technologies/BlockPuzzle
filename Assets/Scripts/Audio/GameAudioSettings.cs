using UnityEngine;
using UnityEngine.Audio;

// Settings bridge: keeps existing preferences and supports the persistent audio player.
public class GameAudioSettings : MonoBehaviour
{
    public AudioMixer mixer;
    public string soundParameter = "SoundVolume";
    public string musicParameter = "MusicVolume";
    public float enabledSoundDb = 0f;
    public float enabledMusicDb = 0f;
    public bool SoundEnabled { get { return PlayerPrefs.GetInt("GameSound", 1) == 1; } }
    public bool MusicEnabled { get { return PlayerPrefs.GetInt("GameMusic", 1) == 1; } }

    private void Start() { Apply(); }
    public void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt("GameSound", enabled ? 1 : 0); PlayerPrefs.Save(); Apply();
    }
    public void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt("GameMusic", enabled ? 1 : 0); PlayerPrefs.Save(); Apply();
    }
    public void Apply()
    {
        if (BlockPuzzleAudio.Instance != null) BlockPuzzleAudio.Instance.ApplySettings();
        if (mixer == null) return;
        if (!mixer.SetFloat(soundParameter, SoundEnabled ? enabledSoundDb : -80f))
            Debug.LogWarning("Expose the Sound mixer group's volume as " + soundParameter, this);
        if (!mixer.SetFloat(musicParameter, MusicEnabled ? enabledMusicDb : -80f))
            Debug.LogWarning("Expose the Music mixer group's volume as " + musicParameter, this);
    }
}
