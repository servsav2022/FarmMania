using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplaySettingsController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Defaults")]
    [SerializeField] private int defaultWidth = 1366;
    [SerializeField] private int defaultHeight = 768;
    [SerializeField] private bool defaultFullscreen = false;

    private const string KeyResIndex = "display_res_index";
    private const string KeyFullscreen = "display_fullscreen";

    private Resolution[] _resolutions;
    private bool _loading;

    private void Awake()
    {
        SubscribeUI();
    }

    private void Start()
    {
        InitResolutionsAndDropdown();
        ReloadFromPrefs();
    }

    private void OnDestroy()
    {
        UnsubscribeUI();
    }

    private void SubscribeUI()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void UnsubscribeUI()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }

    private void InitResolutionsAndDropdown()
    {
        _resolutions = Screen.resolutions;

        var unique = new List<Resolution>();
        var options = new List<string>();

        for (int i = 0; i < _resolutions.Length; i++)
        {
            var r = _resolutions[i];

            bool exists = false;
            for (int j = 0; j < unique.Count; j++)
            {
                if (unique[j].width == r.width && unique[j].height == r.height)
                {
                    exists = true;
                    break;
                }
            }

            if (exists) continue;

            unique.Add(r);
            options.Add($"{r.width} x {r.height}");
        }

        _resolutions = unique.ToArray();

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
        }
    }

    public void ReloadFromPrefs()
    {
        if (_resolutions == null || _resolutions.Length == 0)
            InitResolutionsAndDropdown();

        _loading = true;

        int savedIndex = PlayerPrefs.GetInt(KeyResIndex, FindDefaultResolutionIndexFallbackToCurrent());
        savedIndex = Mathf.Clamp(savedIndex, 0, _resolutions.Length - 1);

        bool savedFullscreen = PlayerPrefs.GetInt(KeyFullscreen, defaultFullscreen ? 1 : 0) == 1;

        if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(savedIndex);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);

        Apply(savedIndex, savedFullscreen);

        _loading = false;
    }

    public void ResetToDefaults()
    {
        _loading = true;

        int idx = FindResolutionIndex(defaultWidth, defaultHeight);
        if (idx < 0) idx = FindCurrentResolutionIndex();

        bool fullscreen = defaultFullscreen;

        PlayerPrefs.SetInt(KeyResIndex, idx);
        PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        if (resolutionDropdown != null) resolutionDropdown.SetValueWithoutNotify(idx);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        Apply(idx, fullscreen);

        _loading = false;

        Debug.Log("[DisplaySettings] ResetToDefaults");
    }

    private int FindDefaultResolutionIndexFallbackToCurrent()
    {
        int idx = FindResolutionIndex(defaultWidth, defaultHeight);
        if (idx >= 0) return idx;
        return FindCurrentResolutionIndex();
    }

    private int FindCurrentResolutionIndex()
    {
        int currentW = Screen.width;
        int currentH = Screen.height;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].width == currentW && _resolutions[i].height == currentH)
                return i;
        }
        return 0;
    }

    private int FindResolutionIndex(int w, int h)
    {
        if (_resolutions == null) return -1;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].width == w && _resolutions[i].height == h)
                return i;
        }

        return -1;
    }

    private void OnResolutionChanged(int index)
    {
        if (_loading) return;
        if (_resolutions == null || _resolutions.Length == 0) return;

        index = Mathf.Clamp(index, 0, _resolutions.Length - 1);

        bool fullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;

        PlayerPrefs.SetInt(KeyResIndex, index);
        PlayerPrefs.Save();

        Apply(index, fullscreen);
    }

    private void OnFullscreenChanged(bool fullscreen)
    {
        if (_loading) return;
        if (_resolutions == null || _resolutions.Length == 0) return;

        int index = resolutionDropdown != null ? resolutionDropdown.value : FindCurrentResolutionIndex();
        index = Mathf.Clamp(index, 0, _resolutions.Length - 1);

        PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Apply(index, fullscreen);
    }

    private void Apply(int index, bool fullscreen)
    {
        if (_resolutions == null || _resolutions.Length == 0) return;

        var r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, fullscreen);
    }
}
