using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    private const string MixerMusicVol = "MusicVolume";
    private const string MixerSfxVol = "SFXVolume";

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip farmMusic;

    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip plantSfx;
    [SerializeField] private AudioClip harvestSfx;
    [SerializeField] private AudioClip buySfx;
    [SerializeField] private AudioClip sellSfx;

    [Header("Default Volumes")]
    [Range(0f, 1f)][SerializeField] private float defaultMusicVolume = 0.3f;
    [Range(0f, 1f)][SerializeField] private float defaultSfxVolume = 1.0f;

    // Ключи PlayerPrefs
    private const string KeyMusicVol = "musicVolume";
    private const string KeySfxVol = "sfxVolume";
    private const string KeySoundEnabled = "soundEnabled";

    // Ключи для совместимости с текущим меню настроек
    private const string KeyMusicMute = "musicMute";
    private const string KeySfxMute = "sfxMute";

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null || sfxSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length >= 2)
            {
                musicSource = sources[0];
                sfxSource = sources[1];
            }
            else
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        // ВАЖНО: применяем настройки сразу при запуске
        ApplySavedAudioSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (I == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForActiveScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedAudioSettings();
        PlayMusicForActiveScene();
    }

    private void PlayMusicForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        AudioClip target = null;
        if (sceneName.Contains("MainMenu"))
            target = mainMenuMusic;
        else if (sceneName.Contains("Farm"))
            target = farmMusic;

        if (target == null)
            return;

        if (musicSource.clip == target && musicSource.isPlaying)
            return;

        musicSource.clip = target;

        bool soundEnabled = PlayerPrefs.GetInt(KeySoundEnabled, 1) == 1;
        bool musicMuted = PlayerPrefs.GetInt(KeyMusicMute, 0) == 1;
        float v = PlayerPrefs.GetFloat(KeyMusicVol, defaultMusicVolume);

        if (soundEnabled && !musicMuted && v > 0.001f && !musicSource.isPlaying)
            musicSource.Play();
    }

    // ---------- Public API ----------

    public void PlayUIClick() => PlaySfx(uiClick);
    public void PlayPlant() => PlaySfx(plantSfx);
    public void PlayHarvest() => PlaySfx(harvestSfx);
    public void PlayBuy() => PlaySfx(buySfx);
    public void PlaySell() => PlaySfx(sellSfx);

    public void PlaySfx(AudioClip clip, float volumeMul = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeMul));
    }

    public void SetMusicVolume(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeyMusicVol, v);
        PlayerPrefs.Save();

        if (audioMixer != null)
            audioMixer.SetFloat(MixerMusicVol, LinearToDb(v));

        if (v <= 0.001f && musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
        else if (v > 0.001f && musicSource != null && !musicSource.mute && musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void SetSfxVolume(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeySfxVol, v);
        PlayerPrefs.Save();

        if (audioMixer != null)
            audioMixer.SetFloat(MixerSfxVol, LinearToDb(v));
    }

    public void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KeySoundEnabled, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (musicSource != null) musicSource.mute = !enabled || PlayerPrefs.GetInt(KeyMusicMute, 0) == 1;
        if (sfxSource != null) sfxSource.mute = !enabled || PlayerPrefs.GetInt(KeySfxMute, 0) == 1;

        if (!enabled && musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
        else if (enabled)
            PlayMusicForActiveScene();
    }

    // Методы для совместимости с твоим AudioSettingsController
    public void SetMusicMute(bool mute)
    {
        PlayerPrefs.SetInt(KeyMusicMute, mute ? 1 : 0);
        PlayerPrefs.Save();

        bool soundEnabled = PlayerPrefs.GetInt(KeySoundEnabled, 1) == 1;

        if (musicSource != null)
            musicSource.mute = mute || !soundEnabled;

        if (!mute && soundEnabled)
            PlayMusicForActiveScene();
        else if (mute && musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void SetSfxMute(bool mute)
    {
        PlayerPrefs.SetInt(KeySfxMute, mute ? 1 : 0);
        PlayerPrefs.Save();

        bool soundEnabled = PlayerPrefs.GetInt(KeySoundEnabled, 1) == 1;

        if (sfxSource != null)
            sfxSource.mute = mute || !soundEnabled;
    }

    private void ApplySavedAudioSettings()
    {
        float musicVol = PlayerPrefs.GetFloat(KeyMusicVol, defaultMusicVolume);
        float sfxVol = PlayerPrefs.GetFloat(KeySfxVol, defaultSfxVolume);

        bool soundEnabled = PlayerPrefs.GetInt(KeySoundEnabled, 1) == 1;
        bool musicMuted = PlayerPrefs.GetInt(KeyMusicMute, 0) == 1;
        bool sfxMuted = PlayerPrefs.GetInt(KeySfxMute, 0) == 1;

        if (audioMixer != null)
        {
            audioMixer.SetFloat(MixerMusicVol, LinearToDb(musicVol));
            audioMixer.SetFloat(MixerSfxVol, LinearToDb(sfxVol));
        }

        if (musicSource != null) musicSource.mute = !soundEnabled || musicMuted;
        if (sfxSource != null) sfxSource.mute = !soundEnabled || sfxMuted;

        if (!soundEnabled && musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    private static float LinearToDb(float value01)
    {
        value01 = Mathf.Clamp01(value01);
        return value01 > 0.001f ? Mathf.Log10(value01) * 20f : -80f;
    }
}
