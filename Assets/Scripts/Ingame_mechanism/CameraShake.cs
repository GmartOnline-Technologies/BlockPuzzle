using UnityEngine;

[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    [Header("One line")]
    [Min(0f)] public float oneLineStrength = 0.04f;
    [Min(0f)] public float oneLineDuration = 0.16f;

    [Header("Two lines")]
    [Min(0f)] public float twoLineStrength = 0.08f;
    [Min(0f)] public float twoLineDuration = 0.24f;

    [Header("Three or more lines")]
    [Min(0f)] public float threePlusStrength = 0.14f;
    [Min(0f)] public float threePlusDuration = 0.34f;

    [Header("Motion")]
    [Min(1f)] public float frequency = 35f;

    private Vector3 restPosition;
    private float strength;
    private float duration;
    private float elapsed;
    private float seed;
    private bool isShaking;

    // BoardManager calls this once after it has counted all completed lines.
    public static void ShakeMainCamera(int clearedLines)
    {
        if (clearedLines <= 0) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        CameraShake shake = mainCamera.GetComponent<CameraShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<CameraShake>();

        if (shake.isActiveAndEnabled)
            shake.ShakeForLines(clearedLines);
    }

    public void ShakeForLines(int clearedLines)
    {
        if (clearedLines <= 0 || !isActiveAndEnabled) return;

        float nextStrength;
        float nextDuration;
        if (clearedLines == 1)
        {
            nextStrength = oneLineStrength;
            nextDuration = oneLineDuration;
        }
        else if (clearedLines == 2)
        {
            nextStrength = twoLineStrength;
            nextDuration = twoLineDuration;
        }
        else
        {
            nextStrength = threePlusStrength;
            nextDuration = threePlusDuration;
        }

        if (nextStrength <= 0f || nextDuration <= 0f) return;

        if (isShaking)
        {
            // Reuse the original position so overlapping clears cannot cause drift.
            nextStrength = Mathf.Max(strength, nextStrength);
            nextDuration = Mathf.Max(duration - elapsed, nextDuration);
        }
        else
        {
            restPosition = transform.localPosition;
        }

        strength = nextStrength;
        duration = nextDuration;
        elapsed = 0f;
        seed = Random.Range(0f, 1000f);
        isShaking = true;
    }

    private void LateUpdate()
    {
        if (!isShaking) return;

        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            StopShake();
            return;
        }

        float fade = 1f - elapsed / duration;
        float amount = strength * fade * fade;
        float noiseTime = elapsed * frequency;
        float x = Mathf.PerlinNoise(seed, noiseTime) * 2f - 1f;
        float y = Mathf.PerlinNoise(seed + 100f, noiseTime) * 2f - 1f;

        // Shake only X/Y; preserve the camera's depth and rotation.
        transform.localPosition = restPosition + new Vector3(x, y, 0f) * amount;
    }

    public void StopShake()
    {
        if (!isShaking) return;
        transform.localPosition = restPosition;
        isShaking = false;
    }

    private void OnDisable()
    {
        StopShake();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) StopShake();
    }
}