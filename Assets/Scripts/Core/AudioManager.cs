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
        Instance = this;
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

        Instance._sfxSource.pitch  = Random.Range(
            1f - (entry.PitchVariance - 1f),
            entry.PitchVariance);
        Instance._sfxSource.PlayOneShot(
            entry.Clip, entry.Volume * Instance.masterVolume);
    }
}