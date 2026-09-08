using System;
using System.Collections.Generic;
using Buran.Localization;
using Buran.SQLite;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.ID3Editor.Models;

public enum CatalogSuggestionKind {
    Artist,
    Genre,
    Mood
}

public enum ArtistInsertMode {
    Preferred,
    Alternative
}

public partial class CatalogSuggestionItem : ObservableObject {
    public CatalogSuggestionItem(CatalogSuggestionKind kind, string value, string source) {
        Kind   = kind;
        Value  = value;
        Source = source;
    }

    public CatalogSuggestionKind Kind   { get; }
    public string                Value  { get; }
    public string                Source { get; }

    public IReadOnlyList<DatabaseTable_ArtistNames> ExistingArtists { get; init; } =
        Array.Empty<DatabaseTable_ArtistNames>();

    public IReadOnlyList<DatabaseTable_GenreNames> ExistingGenres { get; init; } =
        Array.Empty<DatabaseTable_GenreNames>();

    public IReadOnlyList<DatabaseTable_MoodNames> ExistingMoods { get; init; } =
        Array.Empty<DatabaseTable_MoodNames>();

    public bool IsArtist => Kind == CatalogSuggestionKind.Artist;
    public bool IsGenre  => Kind == CatalogSuggestionKind.Genre;
    public bool IsMood   => Kind == CatalogSuggestionKind.Mood;

    public string KindLabel => Kind switch {
        CatalogSuggestionKind.Artist => L.Get("Catalog.KindArtist"),
        CatalogSuggestionKind.Genre  => L.Get("Catalog.KindGenre"),
        CatalogSuggestionKind.Mood   => L.Get("Catalog.KindMood"),
        _                            => Kind.ToString()
    };

    public string RadioGroupName => $"{Kind}-{Value}";

    [ObservableProperty] private bool _include = true;

    [ObservableProperty] private ArtistInsertMode _artistMode = ArtistInsertMode.Preferred;

    [ObservableProperty] private DatabaseTable_ArtistNames? _refersToArtist;

    [ObservableProperty] private DatabaseTable_GenreNames? _refersToGenre;

    [ObservableProperty] private DatabaseTable_MoodNames? _refersToMood;

    [ObservableProperty] private string _newPreferredName = "";

    [ObservableProperty] private string _newRealName = "";

    public bool HasNewPreferredName => !string.IsNullOrWhiteSpace(NewPreferredName);

    public bool HasAlternativeTarget => Kind switch {
        CatalogSuggestionKind.Artist => RefersToArtist is not null || HasNewPreferredName,
        CatalogSuggestionKind.Genre  => RefersToGenre is not null,
        CatalogSuggestionKind.Mood   => RefersToMood is not null,
        _                            => false
    };

    public bool IsPreferredName {
        get => ArtistMode == ArtistInsertMode.Preferred;
        set {
            if (value)
                ArtistMode = ArtistInsertMode.Preferred;
        }
    }

    public bool IsAlternativeName {
        get => ArtistMode == ArtistInsertMode.Alternative;
        set {
            if (value)
                ArtistMode = ArtistInsertMode.Alternative;
        }
    }

    partial void OnArtistModeChanged(ArtistInsertMode value) {
        OnPropertyChanged(nameof(IsPreferredName));
        OnPropertyChanged(nameof(IsAlternativeName));
        if (value == ArtistInsertMode.Preferred) {
            RefersToArtist   = null;
            RefersToGenre    = null;
            RefersToMood     = null;
            NewPreferredName = "";
            NewRealName      = "";
        }
    }

    partial void OnNewPreferredNameChanged(string value) {
        OnPropertyChanged(nameof(HasNewPreferredName));
        OnPropertyChanged(nameof(HasAlternativeTarget));
    }

    partial void OnRefersToArtistChanged(DatabaseTable_ArtistNames? value) {
        OnPropertyChanged(nameof(HasAlternativeTarget));
    }

    partial void OnRefersToGenreChanged(DatabaseTable_GenreNames? value) {
        OnPropertyChanged(nameof(HasAlternativeTarget));
    }

    partial void OnRefersToMoodChanged(DatabaseTable_MoodNames? value) {
        OnPropertyChanged(nameof(HasAlternativeTarget));
    }
}
