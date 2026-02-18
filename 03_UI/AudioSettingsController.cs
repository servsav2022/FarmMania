using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Defaults")]
    [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float defaultSfxVolume = 1.0f;
    [SerializeField] private bool defaultSoundEnabled = true;

    private const string KeyMusicVol = "musicVolume";
    private const string KeySfxVol = "sfxVolume";
    private const string KeySoundEnabled = "soundEnabled";

    private bool _loading;

    private void Awake()
    {
        SubscribeUI();
    }

    private void Start()
    {
        ReloadFromPrefs();
    }

    private void OnDestroy()
    {
        UnsubscribeUI();
    }

    private void SubscribeUI()
    {
        if (soundToggle != null)
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    private void UnsubscribeUI()
    {
        if (soundToggle != null)
            soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    public void ReloadFromPrefs()
    {
        _loading = true;

        bool soundEnabled = PlayerPrefs.GetInt(KeySoundEnabled, defaultSoundEnabled ? 1 : 0) == 1;
        float musicVol = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusicVol, defaultMusicVolume));
        float sfxVol = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfxVol, defaultSfxVolume));

        if (soundToggle != null) soundToggle.SetIsOnWithoutNotify(soundEnabled);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(musicVol);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfxVol);

        ApplyAll(soundEnabled, musicVol, sfxVol);

        _loading = false;
    }

    public void ResetToDefaults()
    {
        _loading = true;

        PlayerPrefs.SetInt(KeySoundEnabled, defaultSoundEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KeyMusicVol, defaultMusicVolume);
        PlayerPrefs.SetFloat(KeySfxVol, defaultSfxVolume);
        PlayerPrefs.Save();

        if (soundToggle != null) soundToggle.SetIsOnWithoutNotify(defaultSoundEnabled);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(defaultMusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(defaultSfxVolume);

        ApplyAll(defaultSoundEnabled, defaultMusicVolume, defaultSfxVolume);

        _loading = false;

        Debug.Log("[AudioSettings] ResetToDefaults");
    }

    private void UpdateInteractable(bool soundEnabled)
    {
        if (musicSlider != null) musicSlider.interactable = soundEnabled;
        if (sfxSlider != null) sfxSlider.interactable = soundEnabled;
    }

    public void OnSoundToggleChanged(bool enabled)
    {
        if (_loading) return;

        PlayerPrefs.SetInt(KeySoundEnabled, enabled ? 1 : 0);
        PlayerPrefs.Save();

        // При выключенном звуке не теряем значения громкости, просто mute
        ApplySoundEnabled(enabled);
        UpdateInteractable(enabled);

        Debug.Log("[AudioSettings] Sound enabled: " + enabled);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (_loading) return;

        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KeyMusicVol, value);
        PlayerPrefs.Save();

        ApplyMusicVolume(value);

        Debug.Log("[AudioSettings] Music volume: " + value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        if (_loading) return;

        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KeySfxVol, value);
        PlayerPrefs.Save();

        ApplySfxVolume(value);

        Debug.Log("[AudioSettings] SFX volume: " + value);
    }

    private void ApplyAll(bool enabled, float musicVol, float sfxVol)
    {
        ApplySoundEnabled(enabled);
        UpdateInteractable(enabled);

        // Громкости применяем всегда, даже если mute - чтобы при включении все было корректно
        ApplyMusicVolume(musicVol);
        ApplySfxVolume(sfxVol);
    }

    private void ApplySoundEnabled(bool enabled)
    {
        if (AudioManager.I == null) return;

        AudioManager.I.SetMusicMute(!enabled);
        AudioManager.I.SetSfxMute(!enabled);
    }

    private void ApplyMusicVolume(float value)
    {
        if (AudioManager.I == null) return;
        AudioManager.I.SetMusicVolume(value);
    }

    private void ApplySfxVolume(float value)
    {
        if (AudioManager.I == null) return;
        AudioManager.I.SetSfxVolume(value);
    }
}
