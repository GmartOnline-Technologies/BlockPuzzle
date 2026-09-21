using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }
    private void PlayClick() { BlockPuzzleAudio.Play(BlockPuzzleAudio.Effect.Button); }
    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }
}
