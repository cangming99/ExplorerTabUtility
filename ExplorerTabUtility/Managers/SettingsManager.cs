using System;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ExplorerTabUtility.Models;
using ExplorerTabUtility.Helpers;

namespace ExplorerTabUtility.Managers;

public static class SettingsManager
{
    public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;

    /// <summary>
    /// Path to the settings file. Overridable (used by tests); defaults to the per-user application data folder.
    /// </summary>
    internal static string SettingsFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Constants.AppName,
        Constants.SettingsFileName);

    private static AppSettings? _settings;

    private static AppSettings Settings
    {
        get
        {
            if (_settings == null)
                LoadSettings();

            return _settings!;
        }
    }

    /// <summary>Clears the cached settings so the next access reloads from disk. Test-only.</summary>
    internal static void ResetCacheForTests() => _settings = null;

    private static void LoadSettings()
    {
        var settings = new AppSettings();

        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory!);

            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            settings = new AppSettings();
        }

        _settings = settings;
    }

    private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = "")
    {
        StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
    }

    public static bool IsMouseHookActive
    {
        get => Settings.MouseHook;
        set
        {
            Settings.MouseHook = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool IsKeyboardHookActive
    {
        get => Settings.KeyboardHook;
        set
        {
            Settings.KeyboardHook = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool IsWindowHookActive
    {
        get => Settings.WindowHook;
        set
        {
            Settings.WindowHook = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool ReuseTabs
    {
        get => Settings.ReuseTabs;
        set
        {
            Settings.ReuseTabs = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static string HotKeyProfiles
    {
        get => Settings.HotKeyProfiles;
        set
        {
            Settings.HotKeyProfiles = value;
            SaveSettings();
        }
    }

    public static Size FormSize
    {
        get => Settings.FormSize;
        set
        {
            Settings.FormSize = value;
            SaveSettings();
        }
    }

    public static bool SaveProfilesOnExit
    {
        get => Settings.SaveProfilesOnExit;
        set
        {
            Settings.SaveProfilesOnExit = value;
            SaveSettings();
        }
    }

    public static bool IsFirstRun
    {
        get => Settings.IsFirstRun;
        set
        {
            Settings.IsFirstRun = value;
            SaveSettings();
        }
    }

    /// <summary>
    /// The active language: "auto" (follow the operating system display language) or a canonical
    /// culture code such as "en" or "zh-Hans". Persisted in the settings file and applied through
    /// <see cref="LocalizationManager"/>.
    /// </summary>
    public static bool IsEnabled
    {
        get => Settings.IsEnabled;
        set
        {
            Settings.IsEnabled = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static string Language
    {
        get => Settings.Language;
        set
        {
            Settings.Language = value;
            SaveSettings();
            NotifyStaticPropertyChanged();
        }
    }

    public static bool IsTrayIconHidden
    {
        get => Settings.IsTrayIconHidden;
        set
        {
            Settings.IsTrayIconHidden = value;
            SaveSettings();
        }
    }

    public static bool HaveThemeIssue
    {
        get => Settings.HaveThemeIssue;
        set
        {
            Settings.HaveThemeIssue = value;
            SaveSettings();
        }
    }

    public static bool AutoUpdate
    {
        get => Settings.AutoUpdate;
        set
        {
            Settings.AutoUpdate = value;
            SaveSettings();
        }
    }

    public static bool SaveClosedHistory
    {
        get => Settings.SaveClosedWindows;
        set
        {
            Settings.SaveClosedWindows = value;
            SaveSettings();
        }
    }

    public static bool RestorePreviousWindows
    {
        get => Settings.RestorePreviousWindows;
        set
        {
            Settings.RestorePreviousWindows = value;
            SaveSettings();
        }
    }

    public static WindowRecord[]? ClosedWindows
    {
        get => Settings.ClosedWindows;
        set
        {
            Settings.ClosedWindows = value;
            SaveSettings();
        }
    }


    public static void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}

internal class AppSettings
{
    public bool MouseHook { get; set; }
    public bool KeyboardHook { get; set; } = true;
    public bool WindowHook { get; set; } = true;
    public bool ReuseTabs { get; set; } = true;
    public Size FormSize { get; set; } = new(852, 402);
    public bool SaveProfilesOnExit { get; set; } = true;
    public bool IsFirstRun { get; set; } = true;
    public bool IsTrayIconHidden { get; set; }
    public bool HaveThemeIssue { get; set; }
    public bool AutoUpdate { get; set; }
    public string HotKeyProfiles { get; set; } = Constants.DefaultHotKeyProfiles;
    public string Language { get; set; } = LocalizationManager.AutoLanguage;
    public bool IsEnabled { get; set; } = true;
    public bool SaveClosedWindows { get; set; }
    public bool RestorePreviousWindows { get; set; }
    public WindowRecord[]? ClosedWindows { get; set; }
}