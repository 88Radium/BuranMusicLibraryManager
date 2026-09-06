using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Buran.SQLite;

namespace Buran.DBEditor.Converter;

public class AlternativeArtistFilterConverter : IMultiValueConverter {
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        if (values.Count < 2) return false;
        if (values[1] is null) return true;
        if (values[0] is not int refersToId) return false;
        if (values[1] is not DatabaseTable_ArtistNames selectedArtist) return false;
        return refersToId == selectedArtist.ID;
    }
}