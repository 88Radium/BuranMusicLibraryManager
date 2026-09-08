using System;
using System.Globalization;
using System.IO;
using ATL;
using Buran.Types;

namespace Buran.ID3Editor.Models;

public sealed record FileCompareSnapshot {
    public string          FileName         { get; init; } = "";
    public string          FullPath         { get; init; } = "";
    public long            SizeBytes        { get; init; }
    public DateTimeOffset? Modified         { get; init; }
    public double          DurationSeconds  { get; init; }
    public double          Bitrate          { get; init; }
    public bool            IsVbr            { get; init; }
    public double          SampleRate       { get; init; }
    public string          Channels         { get; init; } = "";
    public int             BitDepth         { get; init; } = -1;
    public string          Format           { get; init; } = "";
    public string          Title            { get; init; } = "";
    public string          Artists          { get; init; } = "";
    public string          Album            { get; init; } = "";
    public string          Year             { get; init; } = "";
    public bool            AudioRead        { get; init; }

    public static FileCompareSnapshot FromMp3(Mp3FileObject file) {
        var snapshot = FromFileInfo(file.FullPath);
        if (file.Mp3File is null)
            return snapshot;

        return WithAudio(snapshot, file.Mp3File, file.Id3Title, string.Join("; ", file.Id3ArtistCollection), file.Id3Album);
    }

    public static FileCompareSnapshot FromPath(string path) {
        var snapshot = FromFileInfo(path);
        try {
            var track = new Track(path);
            return WithAudio(snapshot, track, track.Title ?? "", track.Artist ?? "", track.Album ?? "");
        } catch {
            return snapshot;
        }
    }

    private static FileCompareSnapshot FromFileInfo(string path) {
        var info = new FileInfo(path);
        return new FileCompareSnapshot {
            FileName  = Path.GetFileName(path),
            FullPath  = path,
            SizeBytes = info.Exists ? info.Length : 0,
            Modified  = info.Exists ? info.LastWriteTime : null
        };
    }

    private static FileCompareSnapshot WithAudio(
        FileCompareSnapshot file,
        Track track,
        string title,
        string artists,
        string album) {
        var year = Mp3FileObject.ReadReleaseYear(track)?.ToString(CultureInfo.InvariantCulture) ?? "";

        return file with {
            DurationSeconds = track.Duration,
            Bitrate         = track.Bitrate,
            IsVbr           = track.IsVBR,
            SampleRate      = track.SampleRate,
            Channels        = track.ChannelsArrangement?.ToString() ?? "",
            BitDepth        = track.BitDepth,
            Format          = track.AudioFormat?.ToString() ?? "",
            Title           = title,
            Artists         = artists,
            Album           = album,
            Year            = year,
            AudioRead       = true
        };
    }
}
