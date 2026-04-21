using System;
using UnityEngine;

public static class GameSettings
{
    public enum PlayMode
    {
        Custom = 0,
        Classic = 1,
        Chill = 2,
        Turbo = 3,
        Nightmare = 4,
        Practice = 5,
        Zen = 6
    }

    const string Prefix = "PacMan.Settings.";

    static readonly string[] playModeNames =
    {
        "Custom",
        "Classic",
        "Chill",
        "Turbo",
        "Nightmare",
        "Practice",
        "Zen"
    };

    static bool loaded;

    public static event Action SettingsChanged;

    public static string[] PlayModeNames => playModeNames;
    public static PlayMode Mode { get; private set; } = PlayMode.Classic;
    public static float MasterVolume { get; private set; } = 1f;
    public static float GameSpeedMultiplier { get; private set; } = 1f;
    public static float PacmanSpeedMultiplier { get; private set; } = 1f;
    public static float GhostSpeedMultiplier { get; private set; } = 1f;
    public static float FrightDurationMultiplier { get; private set; } = 1f;
    public static int StartingLives { get; private set; } = 4;
    public static bool Invincible { get; private set; }
    public static bool TouchControls { get; private set; }
    public static bool ShowFps { get; private set; }
    public static bool ReduceFlashing { get; private set; }
    public static bool ColorblindPalette { get; private set; }
    public static bool VSync { get; private set; } = true;
    public static bool Fullscreen { get; private set; }
    public static int TargetFrameRate { get; private set; } = 60;
    public static int QualityLevel { get; private set; } = 2;
    public static int WindowPreset { get; private set; }
    public static float UiScale { get; private set; } = 1f;
    public static Vector2 TouchDirection { get; private set; }

    static string Key(string name) => Prefix + name;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        Mode = (PlayMode)PlayerPrefs.GetInt(Key("Mode"), (int)PlayMode.Classic);
        MasterVolume = PlayerPrefs.GetFloat(Key("MasterVolume"), 1f);
        GameSpeedMultiplier = PlayerPrefs.GetFloat(Key("GameSpeedMultiplier"), 1f);
        PacmanSpeedMultiplier = PlayerPrefs.GetFloat(Key("PacmanSpeedMultiplier"), 1f);
        GhostSpeedMultiplier = PlayerPrefs.GetFloat(Key("GhostSpeedMultiplier"), 1f);
        FrightDurationMultiplier = PlayerPrefs.GetFloat(Key("FrightDurationMultiplier"), 1f);
        StartingLives = PlayerPrefs.GetInt(Key("StartingLives"), 4);
        Invincible = PlayerPrefs.GetInt(Key("Invincible"), 0) == 1;
        TouchControls = PlayerPrefs.GetInt(Key("TouchControls"), Application.isMobilePlatform ? 1 : 0) == 1;
        ShowFps = PlayerPrefs.GetInt(Key("ShowFps"), 0) == 1;
        ReduceFlashing = PlayerPrefs.GetInt(Key("ReduceFlashing"), 0) == 1;
        ColorblindPalette = PlayerPrefs.GetInt(Key("ColorblindPalette"), 0) == 1;
        VSync = PlayerPrefs.GetInt(Key("VSync"), 1) == 1;
        Fullscreen = PlayerPrefs.GetInt(Key("Fullscreen"), Screen.fullScreen ? 1 : 0) == 1;
        TargetFrameRate = PlayerPrefs.GetInt(Key("TargetFrameRate"), 60);
        QualityLevel = PlayerPrefs.GetInt(Key("QualityLevel"), Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, 5));
        WindowPreset = PlayerPrefs.GetInt(Key("WindowPreset"), 0);
        UiScale = PlayerPrefs.GetFloat(Key("UiScale"), 1f);

        ClampValues();
        ApplySystemSettings(forceTimeScale: true);
    }

    public static void ResetAll()
    {
        Mode = PlayMode.Classic;
        MasterVolume = 1f;
        ApplyModeDefaults(PlayMode.Classic);
        TouchControls = Application.isMobilePlatform;
        ShowFps = false;
        ReduceFlashing = false;
        ColorblindPalette = false;
        VSync = true;
        Fullscreen = Screen.fullScreen;
        TargetFrameRate = 60;
        QualityLevel = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, 5);
        WindowPreset = 0;
        UiScale = 1f;
        SaveAndApply();
    }

    public static void ApplyMode(PlayMode mode)
    {
        Mode = mode;
        if (mode == PlayMode.Custom)
        {
            SaveAndApply();
            return;
        }

        ApplyModeDefaults(mode);
        SaveAndApply();
    }

    static void ApplyModeDefaults(PlayMode mode)
    {
        switch (mode)
        {
            case PlayMode.Chill:
                GameSpeedMultiplier = 0.9f;
                PacmanSpeedMultiplier = 1.08f;
                GhostSpeedMultiplier = 0.85f;
                FrightDurationMultiplier = 1.35f;
                StartingLives = 5;
                Invincible = false;
                break;

            case PlayMode.Turbo:
                GameSpeedMultiplier = 1.25f;
                PacmanSpeedMultiplier = 1.12f;
                GhostSpeedMultiplier = 1.1f;
                FrightDurationMultiplier = 0.9f;
                StartingLives = 4;
                Invincible = false;
                break;

            case PlayMode.Nightmare:
                GameSpeedMultiplier = 1.05f;
                PacmanSpeedMultiplier = 0.95f;
                GhostSpeedMultiplier = 1.2f;
                FrightDurationMultiplier = 0.65f;
                StartingLives = 3;
                Invincible = false;
                break;

            case PlayMode.Practice:
                GameSpeedMultiplier = 0.8f;
                PacmanSpeedMultiplier = 1.1f;
                GhostSpeedMultiplier = 0.75f;
                FrightDurationMultiplier = 2f;
                StartingLives = 6;
                Invincible = true;
                break;

            case PlayMode.Zen:
                GameSpeedMultiplier = 0.7f;
                PacmanSpeedMultiplier = 1f;
                GhostSpeedMultiplier = 0.65f;
                FrightDurationMultiplier = 2.5f;
                StartingLives = 6;
                Invincible = false;
                break;

            default:
                GameSpeedMultiplier = 1f;
                PacmanSpeedMultiplier = 1f;
                GhostSpeedMultiplier = 1f;
                FrightDurationMultiplier = 1f;
                StartingLives = 4;
                Invincible = false;
                break;
        }
    }

    public static string CurrentModeName()
    {
        int index = Mathf.Clamp((int)Mode, 0, playModeNames.Length - 1);
        return playModeNames[index];
    }

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        SaveAndApply();
    }

    public static void SetGameSpeed(float value)
    {
        MarkCustom();
        GameSpeedMultiplier = Mathf.Clamp(value, 0.5f, 2f);
        SaveAndApply();
    }

    public static void SetPacmanSpeed(float value)
    {
        MarkCustom();
        PacmanSpeedMultiplier = Mathf.Clamp(value, 0.5f, 2f);
        SaveAndApply();
    }

    public static void SetGhostSpeed(float value)
    {
        MarkCustom();
        GhostSpeedMultiplier = Mathf.Clamp(value, 0.5f, 2f);
        SaveAndApply();
    }

    public static void SetFrightDuration(float value)
    {
        MarkCustom();
        FrightDurationMultiplier = Mathf.Clamp(value, 0f, 2.5f);
        SaveAndApply();
    }

    public static void SetStartingLives(int value)
    {
        MarkCustom();
        StartingLives = Mathf.Clamp(value, 1, 6);
        SaveAndApply();
    }

    public static void SetInvincible(bool value)
    {
        MarkCustom();
        Invincible = value;
        SaveAndApply();
    }

    public static void SetTouchControls(bool value)
    {
        TouchControls = value;
        SaveAndApply();
    }

    public static void SetShowFps(bool value)
    {
        ShowFps = value;
        SaveAndApply();
    }

    public static void SetReduceFlashing(bool value)
    {
        ReduceFlashing = value;
        SaveAndApply();
    }

    public static void SetColorblindPalette(bool value)
    {
        ColorblindPalette = value;
        SaveAndApply();
    }

    public static void SetVSync(bool value)
    {
        VSync = value;
        SaveAndApply();
    }

    public static void SetFullscreen(bool value)
    {
        Fullscreen = value;
        SaveAndApply();
    }

    public static void SetTargetFrameRate(int value)
    {
        TargetFrameRate = value;
        SaveAndApply();
    }

    public static void SetQualityLevel(int value)
    {
        QualityLevel = Mathf.Max(0, value);
        SaveAndApply();
    }

    public static void SetWindowPreset(int value)
    {
        WindowPreset = Mathf.Clamp(value, 0, 3);
        SaveAndApply();
    }

    public static void SetUiScale(float value)
    {
        UiScale = Mathf.Clamp(value, 0.75f, 1.4f);
        SaveAndApply();
    }

    public static void SetTouchDirection(Vector2 direction)
    {
        TouchDirection = Vector2.ClampMagnitude(direction, 1f);
    }

    static void MarkCustom()
    {
        if (Mode != PlayMode.Custom) Mode = PlayMode.Custom;
    }

    static void ClampValues()
    {
        MasterVolume = Mathf.Clamp01(MasterVolume);
        GameSpeedMultiplier = Mathf.Clamp(GameSpeedMultiplier, 0.5f, 2f);
        PacmanSpeedMultiplier = Mathf.Clamp(PacmanSpeedMultiplier, 0.5f, 2f);
        GhostSpeedMultiplier = Mathf.Clamp(GhostSpeedMultiplier, 0.5f, 2f);
        FrightDurationMultiplier = Mathf.Clamp(FrightDurationMultiplier, 0f, 2.5f);
        StartingLives = Mathf.Clamp(StartingLives, 1, 6);
        UiScale = Mathf.Clamp(UiScale, 0.75f, 1.4f);
    }

    static void SaveAndApply()
    {
        ClampValues();
        Save();
        ApplySystemSettings();
        SettingsChanged?.Invoke();
    }

    static void Save()
    {
        PlayerPrefs.SetInt(Key("Mode"), (int)Mode);
        PlayerPrefs.SetFloat(Key("MasterVolume"), MasterVolume);
        PlayerPrefs.SetFloat(Key("GameSpeedMultiplier"), GameSpeedMultiplier);
        PlayerPrefs.SetFloat(Key("PacmanSpeedMultiplier"), PacmanSpeedMultiplier);
        PlayerPrefs.SetFloat(Key("GhostSpeedMultiplier"), GhostSpeedMultiplier);
        PlayerPrefs.SetFloat(Key("FrightDurationMultiplier"), FrightDurationMultiplier);
        PlayerPrefs.SetInt(Key("StartingLives"), StartingLives);
        PlayerPrefs.SetInt(Key("Invincible"), Invincible ? 1 : 0);
        PlayerPrefs.SetInt(Key("TouchControls"), TouchControls ? 1 : 0);
        PlayerPrefs.SetInt(Key("ShowFps"), ShowFps ? 1 : 0);
        PlayerPrefs.SetInt(Key("ReduceFlashing"), ReduceFlashing ? 1 : 0);
        PlayerPrefs.SetInt(Key("ColorblindPalette"), ColorblindPalette ? 1 : 0);
        PlayerPrefs.SetInt(Key("VSync"), VSync ? 1 : 0);
        PlayerPrefs.SetInt(Key("Fullscreen"), Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(Key("TargetFrameRate"), TargetFrameRate);
        PlayerPrefs.SetInt(Key("QualityLevel"), QualityLevel);
        PlayerPrefs.SetInt(Key("WindowPreset"), WindowPreset);
        PlayerPrefs.SetFloat(Key("UiScale"), UiScale);
        PlayerPrefs.Save();
    }

    public static void ApplySystemSettings(bool forceTimeScale = false)
    {
        AudioListener.volume = MasterVolume;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : TargetFrameRate;

        if (QualitySettings.names.Length > 0)
        {
            int qualityIndex = Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1);
            if (QualitySettings.GetQualityLevel() != qualityIndex)
                QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
        }

        if (Screen.fullScreen != Fullscreen)
            Screen.fullScreen = Fullscreen;

        ApplyWindowPreset();

        if (forceTimeScale || Time.timeScale > 0f)
            Time.timeScale = GameSpeedMultiplier;
    }

    static void ApplyWindowPreset()
    {
        if (WindowPreset == 0 || Application.isMobilePlatform) return;

        int width = 1280;
        int height = 720;

        if (WindowPreset == 2)
        {
            width = 1600;
            height = 900;
        }
        else if (WindowPreset == 3)
        {
            width = 1920;
            height = 1080;
        }

        if (Screen.width != width || Screen.height != height || Screen.fullScreen != Fullscreen)
            Screen.SetResolution(width, height, Fullscreen);
    }
}
