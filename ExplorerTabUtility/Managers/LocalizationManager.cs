using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ExplorerTabUtility.Managers;

/// <summary>
/// Provides application localization: resolves the active language, holds the per-language
/// string resources (embedded JSON), and notifies subscribers when the language changes.
///
/// Deliberately decoupled from WPF: raises plain <see cref="EventHandler"/> events and never
/// touches the UI thread or Dispatcher; UI code subscribes on the UI thread. This keeps the
/// class testable without a window or dispatcher.
///
/// Resolution chain: an explicit language choice wins; otherwise the operating system display
/// language is used; anything unsupported falls back to English. A missing key falls back to
/// English, then to the key itself, so the UI never shows blank text.
/// </summary>
public static class LocalizationManager
{
    /// <summary>Setting value meaning "follow the operating system display language".</summary>
    public const string AutoLanguage = "auto";

    /// <summary>Canonical code of the fallback language.</summary>
    public const string EnglishLanguage = "en";

    /// <summary>Canonical code of the delivered Simplified Chinese language.</summary>
    public const string SimplifiedChineseLanguage = "zh-Hans";

    private const string ResourcePrefix = "ExplorerTabUtility.Resources.";
    private const string ResourceSuffix = ".json";

    private static readonly object SyncRoot = new();
    private static readonly string[] SimplifiedChineseCultures = ["zh", "zh-hans", "zh-cn", "zh-sg", "zh-my", "zh-hans-cn"];

    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Resources = new(StringComparer.OrdinalIgnoreCase)
    {
        [EnglishLanguage] = LoadResource(EnglishLanguage) ?? new Dictionary<string, string>(),
        [SimplifiedChineseLanguage] = LoadResource(SimplifiedChineseLanguage) ?? new Dictionary<string, string>()
    };

    private static string _language = EnglishLanguage;

    /// <summary>Raised whenever the active language actually changes (not on no-op requests).</summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>Canonical code of the currently active language (e.g. "en", "zh-Hans").</summary>
    public static string CurrentLanguage
    {
        get
        {
            lock (SyncRoot)
                return _language;
        }
    }

    /// <summary>The cultures that this language maps to Simplified Chinese.</summary>
    private static bool IsSimplifiedChinese(string normalized) =>
        normalized == "zh"
        || normalized.StartsWith("zh-hans", StringComparison.Ordinal)
        || Array.IndexOf(SimplifiedChineseCultures, normalized) >= 0
        || (normalized.StartsWith("zh-", StringComparison.Ordinal)
            && normalized.IndexOf("tw", StringComparison.Ordinal) < 0
            && normalized.IndexOf("hk", StringComparison.Ordinal) < 0
            && normalized.IndexOf("mo", StringComparison.Ordinal) < 0
            && normalized.IndexOf("hant", StringComparison.Ordinal) < 0);

    /// <summary>All languages delivered with the application.</summary>
    public static IReadOnlyList<string> SupportedLanguages { get; } = [EnglishLanguage, SimplifiedChineseLanguage];

    /// <summary>The culture of the active language, used for string formatting.</summary>
    public static CultureInfo CurrentCulture => GetCulture(CurrentLanguage);

    /// <summary>
    /// Maps an explicit culture/code to the canonical supported language. Unsupported or blank
    /// values resolve to <see cref="EnglishLanguage"/>. Does not resolve "auto" - callers decide
    /// whether the request means "follow the system" first.
    /// </summary>
    public static string ResolveLanguage(string? cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
            return EnglishLanguage;

        var normalized = cultureCode.Trim().ToLowerInvariant();
        return IsSimplifiedChinese(normalized) ? SimplifiedChineseLanguage : EnglishLanguage;
    }

    /// <summary>Resolves the canonical language from the operating system display language.</summary>
    public static string ResolveSystemLanguage() => ResolveLanguage(CultureInfo.CurrentUICulture.Name);

    /// <summary>
    /// Applies the requested language: "auto" (or blank) follows the system display language and
    /// is never persisted; an explicit code is normalized to the supported language and persisted
    /// to settings. Returns true when the active language actually changed.
    /// </summary>
    public static bool SetLanguage(string? language)
    {
        var isAuto = string.IsNullOrWhiteSpace(language)
            || string.Equals(language, AutoLanguage, StringComparison.OrdinalIgnoreCase);

        return isAuto
            ? Apply(ResolveSystemLanguage())
            : ApplyAndPersist(ResolveLanguage(language));
    }

    /// <summary>
    /// Applies the language persisted in settings (without writing back). Call once at startup,
    /// before any UI is shown, so the saved language takes effect from the first frame.
    /// </summary>
    internal static void Initialize()
    {
        var setting = SettingsManager.Language;
        var resolved = string.IsNullOrWhiteSpace(setting)
            || string.Equals(setting, AutoLanguage, StringComparison.OrdinalIgnoreCase)
                ? ResolveSystemLanguage()
                : ResolveLanguage(setting);

        Apply(resolved);
    }

    /// <summary>Looks up a localized string; falls back to English, then to the key itself.</summary>
    public static string GetString(string key)
    {
        lock (SyncRoot)
        {
            if (TryGetResource(key, out var value))
                return value;
        }

        Debug.WriteLine($"Localization: missing key '{key}' for '{CurrentLanguage}'.");
        return key;
    }

    /// <summary>
    /// Looks up a localized string and formats it with the given arguments using the current
    /// language's culture. Falls back the same way as <see cref="GetString(string)"/>.
    /// </summary>
    public static string GetString(string key, params object?[] args)
    {
        var value = GetString(key);
        return args is { Length: > 0 } ? string.Format(CurrentCulture, value, args) : value;
    }

    private static bool ApplyAndPersist(string language)
    {
        SettingsManager.Language = language;
        return Apply(language);
    }

    private static bool Apply(string language)
    {
        lock (SyncRoot)
        {
            if (string.Equals(_language, language, StringComparison.OrdinalIgnoreCase))
                return false;

            _language = language;
        }

        LanguageChanged?.Invoke(null, EventArgs.Empty);
        return true;
    }

    private static bool TryGetResource(string key, out string value)
    {
        value = string.Empty;

        if (Resources.TryGetValue(_language, out var current) && current.TryGetValue(key, out var localized))
        {
            value = localized;
            return true;
        }

        if (Resources.TryGetValue(EnglishLanguage, out var english) && english.TryGetValue(key, out var fallback))
        {
            value = fallback;
            return true;
        }

        return false;
    }

    private static CultureInfo GetCulture(string language)
    {
        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static IReadOnlyDictionary<string, string>? LoadResource(string language)
    {
        try
        {
            var assembly = typeof(LocalizationManager).Assembly;
            var resourceName = ResourcePrefix + language + ResourceSuffix;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.WriteLine($"Localization: embedded resource '{resourceName}' not found.");
                return null;
            }

            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Localization: failed to load resources for '{language}': {ex.Message}");
            return null;
        }
    }
}