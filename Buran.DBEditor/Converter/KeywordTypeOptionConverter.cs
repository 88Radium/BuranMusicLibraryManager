using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Buran.DBEditor.Models;
using Buran.DBEditor.ViewModels;
using Buran.Types;

namespace Buran.DBEditor.Converter;

public sealed class KeywordTypeOptionConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        var code = value as string;
        return DBEditorTabViewModel.KeywordTypes.FirstOrDefault(t =>
                   string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase))
               ?? DBEditorTabViewModel.KeywordTypes[0];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is KeywordTypeOption option ? option.Code : CollaborationMarkers.TypeCollaboration;
}
