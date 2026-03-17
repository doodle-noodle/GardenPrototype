using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Database")]
    public SoundDatabase soundDatabase;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    private AudioSource _sfxSource;

    void Awake()
    {
        Instance   = this;
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        if (soundDatabase != null)
            soundDatabase.Init();
    }

    public static void Play(SoundEvent e)
    {
        if (Instance == null || Instance.soundDatabase == null) return;
        if (!Instance.soundDatabase.TryGet(e, out var entry)) return;
        if (entry.Clip == null) return;

        float pitch = Random.Range(
            1f - (entry.PitchVariance - 1f),
            entry.PitchVariance);

        Instance._sfxSource.pitch = pitch;

        // Convert linear volume to perceptual volume so slider changes
        // feel natural — 0.5 on the slider sounds like half volume, not 90%
        float linearVolume    = entry.Volume * Instance.masterVolume;
        float perceptualVolume = LinearToPerceptual(linearVolume);

        Instance._sfxSource.PlayOneShot(entry.Clip, perceptualVolume);
    }

    // Converts a 0-1 linear slider value to a perceptually linear volume.
    // Uses a squared curve — simple, no log math, works well in practice.
    private static float LinearToPerceptual(float linear)
    {
        return linear * linear;
    }
}