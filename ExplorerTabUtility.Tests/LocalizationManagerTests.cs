using System;
using System.Globalization;
using System.IO;
using ExplorerTabUtility.Managers;
using Xunit;

namespace ExplorerTabUtility.Tests;

/// <summary>
/// Covers the localization engine and its linkage to persisted settings (ticket 01).
/// All state is static, so every test stays in this single class/collection: xUnit runs the
/// tests of one class sequentially, which keeps the shared StaticState deterministic.
/// </summary>
public sealed class LocalizationManagerTests : IDisposable
{
    private readonly string _tempDir;

    public LocalizationManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ExplorerTabUtility.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        // Point settings at an isolated temp file before the type is first touched, then start
        // every test from a known language.
        SettingsManager.SettingsFilePath = Path.Combine(_tempDir, "settings.json");
        SettingsManager.ResetCacheForTests();
        LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage);
    }

    public void Dispose()
    {
        SettingsManager.ResetCacheForTests();
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Ignored - temp cleanup must never fail a test.
        }
    }

    private static IDisposable UseUiCulture(string name)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(name);
        return new RestoreCulture(previous);
    }

    private sealed class RestoreCulture(CultureInfo culture) : IDisposable
    {
        public void Dispose() => CultureInfo.CurrentUICulture = culture;
    }

    [Theory]
    [InlineData("en", LocalizationManager.EnglishLanguage)]
    [InlineData("en-US", LocalizationManager.EnglishLanguage)]
    [InlineData("EN", LocalizationManager.EnglishLanguage)]
    [InlineData("zh", LocalizationManager.SimplifiedChineseLanguage)]
    [InlineData("zh-Hans", LocalizationManager.SimplifiedChineseLanguage)]
    [InlineData("zh-CN", LocalizationManager.SimplifiedChineseLanguage)]
    [InlineData("zh-SG", LocalizationManager.SimplifiedChineseLanguage)]
    [InlineData("ZH-hans", LocalizationManager.SimplifiedChineseLanguage)]
    [InlineData("zh-HK", LocalizationManager.EnglishLanguage)]
    [InlineData("zh-TW", LocalizationManager.EnglishLanguage)]
    [InlineData("zh-Hant", LocalizationManager.EnglishLanguage)]
    [InlineData("fr-FR", LocalizationManager.EnglishLanguage)]
    [InlineData("", LocalizationManager.EnglishLanguage)]
    public void ResolveLanguage_MapsCulturesToSupportedLanguage(string culture, string expected)
        => Assert.Equal(expected, LocalizationManager.ResolveLanguage(culture));

    [Fact]
    public void ResolveLanguage_NullOrWhitespace_FallsBackToEnglish()
    {
        Assert.Equal(LocalizationManager.EnglishLanguage, LocalizationManager.ResolveLanguage(null));
        Assert.Equal(LocalizationManager.EnglishLanguage, LocalizationManager.ResolveLanguage("   "));
    }

    [Fact]
    public void ResolveSystemLanguage_FollowsCurrentUiCulture()
    {
        using (UseUiCulture("zh-CN"))
            Assert.Equal(LocalizationManager.SimplifiedChineseLanguage, LocalizationManager.ResolveSystemLanguage());

        using (UseUiCulture("en-US"))
            Assert.Equal(LocalizationManager.EnglishLanguage, LocalizationManager.ResolveSystemLanguage());
    }

    [Fact]
    public void GetString_ReturnsValueOfCurrentLanguage()
    {
        LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage);
        Assert.Equal("OK", LocalizationManager.GetString("Common.OK"));

        LocalizationManager.SetLanguage(LocalizationManager.SimplifiedChineseLanguage);
        Assert.Equal("确定", LocalizationManager.GetString("Common.OK"));
    }

    [Fact]
    public void GetString_FormatsPlaceholders()
    {
        LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage);
        Assert.Equal("You can show the app again by pressing Ctrl+E.",
            LocalizationManager.GetString("App.VisibilityHotkeyHint", "Ctrl+E"));

        LocalizationManager.SetLanguage(LocalizationManager.SimplifiedChineseLanguage);
        var chinese = LocalizationManager.GetString("App.VisibilityHotkeyHint", "Ctrl+E");
        Assert.Contains("Ctrl+E", chinese);
        Assert.DoesNotContain("{0}", chinese);
    }

    [Fact]
    public void GetString_MissingKey_ReturnsKeyWithoutThrowing()
    {
        LocalizationManager.SetLanguage(LocalizationManager.SimplifiedChineseLanguage);
        Assert.Equal("No.Such.Key", LocalizationManager.GetString("No.Such.Key"));
        Assert.Equal("No.Such.Key", LocalizationManager.GetString("No.Such.Key", "arg"));
    }

    [Fact]
    public void LanguageChanged_IsRaisedOnlyWhenLanguageActuallyChanges()
    {
        LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage);
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        LocalizationManager.LanguageChanged += handler;
        try
        {
            LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage); // no-op
            Assert.Equal(0, raised);

            LocalizationManager.SetLanguage(LocalizationManager.SimplifiedChineseLanguage);
            Assert.Equal(1, raised);

            LocalizationManager.SetLanguage(LocalizationManager.SimplifiedChineseLanguage); // no-op
            Assert.Equal(1, raised);

            LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage);
            Assert.Equal(2, raised);
        }
        finally
        {
            LocalizationManager.LanguageChanged -= handler;
        }
    }

    [Fact]
    public void SetLanguage_PersistsExplicitChoiceAndAppliesItOnStartup()
    {
        SettingsManager.ResetCacheForTests();

        // No settings file yet: the Language field falls back to its default ("auto").
        Assert.Equal(LocalizationManager.AutoLanguage, SettingsManager.Language);

        LocalizationManager.SetLanguage(LocalizationManager.SimplifiedChineseLanguage);
        Assert.Equal(LocalizationManager.SimplifiedChineseLanguage, SettingsManager.Language);

        var persisted = File.ReadAllText(SettingsManager.SettingsFilePath);
        Assert.Contains(LocalizationManager.SimplifiedChineseLanguage, persisted);

        // A fresh process (cache cleared) must start in the persisted language.
        SettingsManager.ResetCacheForTests();
        LocalizationManager.Initialize();
        Assert.Equal(LocalizationManager.SimplifiedChineseLanguage, LocalizationManager.CurrentLanguage);
    }

    [Fact]
    public void SetLanguage_Auto_FollowsSystemWithoutPersisting()
    {
        LocalizationManager.SetLanguage(LocalizationManager.EnglishLanguage); // persisted choice: "en"

        using (UseUiCulture("zh-CN"))
        {
            LocalizationManager.SetLanguage(LocalizationManager.AutoLanguage);
            Assert.Equal(LocalizationManager.SimplifiedChineseLanguage, LocalizationManager.CurrentLanguage);
        }

        // "auto" resolves against the system but is never written into the settings.
        Assert.Equal(LocalizationManager.EnglishLanguage, SettingsManager.Language);
    }

    [Fact]
    public void LegacySettings_WithoutLanguageField_DefaultsToAuto()
    {
        File.WriteAllText(SettingsManager.SettingsFilePath, """{ "MouseHook": true }""");
        SettingsManager.ResetCacheForTests();

        Assert.Equal(LocalizationManager.AutoLanguage, SettingsManager.Language);
    }

    [Fact]
    public void SupportedLanguages_ContainEnglishAndSimplifiedChinese()
        => Assert.Equal(2, LocalizationManager.SupportedLanguages.Count);
}