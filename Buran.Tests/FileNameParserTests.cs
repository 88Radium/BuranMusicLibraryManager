using System.Collections.ObjectModel;
using Buran.SQLite;
using Buran.Types;

namespace Buran.Tests;

public class FileNameParserTests {
    public FileNameParserTests() => CollaborationMarkers.ApplyDefaults();

    [Fact]
    public void ArtistAndTitle_KeepTheSpellingFromTheFileName() {
        var parsed = Parse("2Pac - Life Goes On.mp3", ArtistTitle(90));

        Assert.Equal("Life Goes On", parsed.Title);
        Assert.Equal(["2Pac"], parsed.Artists);
    }

    [Fact]
    public void Underscores_AreTreatedAsSpaces() {
        var parsed = Parse("2Pac_-_Life_Goes_On.mp3", ArtistTitle(90));

        Assert.Equal("Life Goes On", parsed.Title);
        Assert.Equal(["2Pac"], parsed.Artists);
    }

    [Fact]
    public void FeaturedArtist_IsASecondArtist() {
        var patterns = new ObservableCollection<DBConnector.FileNamePattern> {
            ArtistTitle(90),
            new() { Id = 2, Pattern = "{artist} feat. {artists} - {title}", Confidence = 93 }
        };

        var parsed = Parse("Eminem feat. Rihanna - Love The Way You Lie.mp3", patterns.ToArray());

        Assert.Equal("Love The Way You Lie", parsed.Title);
        Assert.Equal(["Eminem", "Rihanna"], parsed.Artists);
    }

    [Fact]
    public void YearInParentheses_IsTheReleaseYear() {
        var parsed = Parse(
            "(2002) Eminem - Lose Yourself.mp3",
            new DBConnector.FileNamePattern {
                Id = 3, Pattern = "({year}) {artist} - {title}", Confidence = 70
            });

        Assert.Equal("2002", parsed.Year);
        Assert.Equal("Lose Yourself", parsed.Title);
        Assert.Equal(["Eminem"], parsed.Artists);
    }

    [Fact]
    public void ShortTokenInsideAName_DoesNotSplitTheArtist() {
        var parsed = Parse("Soft Cell - Tainted Love.mp3", ArtistTitle(90));

        Assert.Equal(["Soft Cell"], parsed.Artists);
        Assert.Equal("Tainted Love", parsed.Title);
    }

    private static ParsedMetadata Parse(string fileName, params DBConnector.FileNamePattern[] patterns) {
        var parser = new FileNameParser();
        return parser.Parse(fileName, new ObservableCollection<DBConnector.FileNamePattern>(patterns));
    }

    private static DBConnector.FileNamePattern ArtistTitle(int confidence) =>
        new() { Id = 1, Pattern = "{artist} - {title}", Confidence = confidence };
}
