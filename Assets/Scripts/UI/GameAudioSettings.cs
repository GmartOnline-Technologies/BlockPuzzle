using UnityEngine;
using UnityEngine.Audio;

// Use the same mixer asset in every scene. Does not create or play AudioSources.
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
        if (mixer == null)
        { Debug.LogWarning("Assign the game's AudioMixer to GameAudioSettings to apply sound/music toggles.", this); return; }
        if (!mixer.SetFloat(soundParameter, SoundEnabled ? enabledSoundDb : -80f))
            Debug.LogWarning("Expose the Sound mixer group's volume as " + soundParameter, this);
        if (!mixer.SetFloat(musicParameter, MusicEnabled ? enabledMusicDb : -80f))
            Debug.LogWarning("Expose the Music mixer group's volume as " + musicParameter, this);
    }
}
