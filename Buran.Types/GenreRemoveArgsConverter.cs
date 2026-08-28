using System.Globalization;
using Avalonia.Data.Converters;

namespace Buran.Types;

public sealed record GenreRemoveArgs(Mp3FileObject File, string Genre);

public class GenreRemoveArgsConverter : IMultiValueConverter {
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        if (values.Count < 2) return null;
        if (values[0] is not string genre) return null;
        if (values[1] is not Mp3FileObject file) return null;
        return new GenreRemoveArgs(file, genre);
    }
}


public sealed record ArtistRemoveArgs(Mp3FileObject File, string Artist);

public class ArtistRemoveArgsConverter : IMultiValueConverter {
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        if (values.Count < 2) return null;
        if (values[0] is not string genre) return null;
        if (values[1] is not Mp3FileObject file) return null;
        return new ArtistRemoveArgs(file, genre);
    }
}

public sealed record MoodRemoveArgs(Mp3FileObject File, string Mood);

public class MoodRemoveArgsConverter : IMultiValueConverter {
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
        if (values.Count < 2) return null;
        if (values[0] is not string Mood) return null;
        if (values[1] is not Mp3FileObject file) return null;
        return new MoodRemoveArgs(file, Mood);
    }
}