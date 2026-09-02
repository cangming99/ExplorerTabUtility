using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ExplorerTabUtility.UI.Localization;

/// <summary>
/// Localizes a XAML property with live language switching, e.g.
/// <c>Text="{loc:Loc MainWindow.Title}"</c> or <c>ToolTip="{loc:Loc Key=Common.OK}"</c>.
///
/// Works on any dependency property that accepts a binding (Text, Content, Header,
/// ToolTip, ...). The returned binding targets <see cref="LocalizationBindingSource"/>,
/// which refreshes all active usages whenever the language changes - no restart needed.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    /// <summary>The localization key to resolve.</summary>
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return DependencyProperty.UnsetValue;

        return new Binding
        {
            Source = LocalizationBindingSource.Instance,
            Path = new PropertyPath($"[{Key}]"),
            Mode = BindingMode.OneWay
        };
    }
}