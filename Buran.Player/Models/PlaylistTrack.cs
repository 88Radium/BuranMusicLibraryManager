using System.Diagnostics;
using System.Globalization;
using ATL;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.Player.Models;

public partial class PlaylistTrack : ObservableObject {
    public PlaylistTrack(string path) {
        Path     = path;
        FileName = System.IO.Path.GetFileName(path);
        Exists   = File.Exists(path);
        if (!Exists)
            return;

        try {
            var track = new Track(path);
            Title           = string.IsNullOrWhiteSpace(track.Title) ? FileName : track.Title;
            Artist          = track.Artist ?? "";
            DurationSeconds = track.Duration;
            SampleRate      = track.SampleRate;
        }
        catch {
            Title = FileName;
        }
    }

    public string Path            { get; }
    public string FileName        { get; }
    public string Title           { get; } = "";
    public string Artist          { get; } = "";
    public int    DurationSeconds { get; }
    public double SampleRate      { get; private set; }
    public bool   Exists          { get; }

    public double NyquistHz => SampleRate > 0 ? SampleRate / 2.0 : 0;

    public void EnsureSampleRate() {
        if (SampleRate > 0 || !Exists)
            return;
        SampleRate = ProbeSampleRate(Path);
    }

    public string DisplayTitle {
        get {
            var title = string.IsNullOrWhiteSpace(Artist) ? Title : $"{Artist} – {Title}";
            return Exists ? title : title + " (missing)";
        }
    }

    public string DurationText {
        get {
            var seconds = Math.Max(0, DurationSeconds);
            return TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
        }
    }

    private static double ProbeSampleRate(string path) {
        try {
            var probe = File.Exists("/usr/bin/ffprobe") ? "/usr/bin/ffprobe" : "ffprobe";
            var psi = new ProcessStartInfo(probe) {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };
            foreach (var arg in new[] {
                         "-v", "error",
                         "-select_streams", "a:0",
                         "-show_entries", "stream=sample_rate",
                         "-of", "csv=p=0",
                         path
                     })
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process is null)
                return 0;
            var text = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000)) {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return 0;
            }

            return double.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hz)
                ? hz
                : 0;
        }
        catch {
            return 0;
        }
    }
}
