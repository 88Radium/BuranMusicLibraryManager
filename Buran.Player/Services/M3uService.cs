using System.Text;

namespace Buran.Player.Services;

public static class M3uService {
    public static void Write(string filePath, IEnumerable<(string Path, string Title, int DurationSeconds)> tracks) {
        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        foreach (var track in tracks) {
            sb.Append("#EXTINF:");
            sb.Append(Math.Max(0, track.DurationSeconds));
            sb.Append(',');
            sb.AppendLine(track.Title.Replace('\n', ' ').Replace('\r', ' '));
            sb.AppendLine(track.Path);
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static List<string> Read(string filePath) {
        var folder = Path.GetDirectoryName(filePath) ?? "";
        var paths  = new List<string>();
        foreach (var raw in File.ReadAllLines(filePath)) {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                continue;

            var resolved = Path.IsPathRooted(line)
                ? line
                : Path.GetFullPath(Path.Combine(folder, line));
            paths.Add(resolved);
        }

        return paths;
    }
}
