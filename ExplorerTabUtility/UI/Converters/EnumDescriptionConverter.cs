using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using ExplorerTabUtility.Managers;

namespace ExplorerTabUtility.UI.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    /// <summary>
    /// Converts an enum value for display: first the localized key
    /// "Enum.{Type}.{Value}" (or "...{Value}.Description" when <paramref name="parameter"/>
    /// is "Description"), then the <see cref="DescriptionAttribute"/>, then the raw name.
    /// Non-enum values pass through unchanged (ToString()).
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return DependencyProperty.UnsetValue;

        if (value is Enum enumValue)
        {
            var isDescription = parameter?.ToString() == "Description";
            var key = $"Enum.{enumValue.GetType().Name}.{enumValue}{ (isDescription ? ".Description" : string.Empty) }";

            var localized = LocalizationManager.GetString(key);
            if (localized != key)
                return localized;
        }

        var valueStr = value.ToString()!;
        var fieldInfo = value.GetType().GetField(valueStr);
        if (fieldInfo == null) return valueStr;

        var descriptionAttribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        return descriptionAttribute?.Description ?? valueStr;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}