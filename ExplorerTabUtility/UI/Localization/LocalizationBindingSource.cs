using System.ComponentModel;
using ExplorerTabUtility.Managers;

namespace ExplorerTabUtility.UI.Localization;

/// <summary>
/// WPF binding source that exposes the localization dictionary through an indexer.
/// Raises <see cref="PropertyChanged"/> for "Item[]" whenever the active language changes,
/// so every binding created by <see cref="LocExtension"/> refreshes automatically without
/// a restart.
/// </summary>
public sealed class LocalizationBindingSource : INotifyPropertyChanged
{
    public static LocalizationBindingSource Instance { get; } = new();

    private const string IndexerName = "Item[]";

    private LocalizationBindingSource()
    {
        LocalizationManager.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(IndexerName));
    }

    /// <summary>Gets the localized string for <paramref name="key"/> in the active language.</summary>
    public string this[string key] => LocalizationManager.GetString(key);

    public event PropertyChangedEventHandler? PropertyChanged;
}