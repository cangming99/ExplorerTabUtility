using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ExplorerTabUtility.Managers;
using Xunit;

namespace ExplorerTabUtility.Tests;

/// <summary>
/// Guards the embedded resource files: every language must define exactly the same key set
/// (the English file is the baseline), must parse, and must not contain empty values.
/// A missing translation is caught here before it can silently fall back in the UI.
/// This class reads embedded resources only, so it is safe to run in parallel with the
/// (static-stateful) LocalizationManagerTests.
/// </summary>
public sealed class LocalizationResourcesTests
{
    [Fact]
    public void ResourceFiles_HaveIdenticalKeySets()
    {
        var english = LoadEntries(LocalizationManager.EnglishLanguage);
        var chinese = LoadEntries(LocalizationManager.SimplifiedChineseLanguage);

        Assert.NotEmpty(english);
        Assert.Equal(new SortedSet<string>(english.Keys, StringComparer.Ordinal),
            new SortedSet<string>(chinese.Keys, StringComparer.Ordinal));
    }

    [Fact]
    public void ResourceFiles_ValuesAreNonEmpty()
    {
        foreach (var language in new[] { LocalizationManager.EnglishLanguage, LocalizationManager.SimplifiedChineseLanguage })
        {
            foreach (var (key, value) in LoadEntries(language))
                Assert.False(string.IsNullOrWhiteSpace(value), $"{language}: value for '{key}' is empty.");
        }
    }

    internal static Dictionary<string, string> LoadEntries(string language)
    {
        var assembly = typeof(LocalizationManager).Assembly;
        var resourceName = $"ExplorerTabUtility.Resources.{language}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!);
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
        Assert.NotNull(entries);

        return entries!;
    }
}