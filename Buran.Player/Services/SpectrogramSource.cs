using System.Diagnostics;
using Avalonia.Media.Imaging;

namespace Buran.Player.Services;

/// <summary>
/// Builds a Spek-style spectrogram (time × frequency) once per track via ffmpeg.
/// Playback stays on libVLC; this is only the picture.
/// </summary>
public sealed class SpectrogramSource : IDisposable {
    private CancellationTokenSource? _loadCts;
    private Bitmap? _image;

    public Bitmap? Image => _image;

    public async Task<Bitmap?> LoadAsync(string path, int stopHz = 0) {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        try {
            var bytes = await Task.Run(() => RenderPng(path, stopHz, token), token);
            if (bytes is null || bytes.Length == 0)
                return Swap(null);

            await using var stream = new MemoryStream(bytes, writable: false);
            return Swap(new Bitmap(stream));
        }
        catch (OperationCanceledException) {
            return _image;
        }
        catch (Exception ex) {
            Debug.WriteLine($"Spectrogram failed: {ex.Message}");
            return Swap(null);
        }
    }

    public void Clear() => Swap(null);

    public void Dispose() {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        Swap(null);
    }

    private Bitmap? Swap(Bitmap? next) {
        var previous = _image;
        _image = next;
        previous?.Dispose();
        return next;
    }

    private static byte[]? RenderPng(string path, int stopHz, CancellationToken token) {
        var ffmpeg = File.Exists("/usr/bin/ffmpeg") ? "/usr/bin/ffmpeg" : "ffmpeg";
        var psi = new ProcessStartInfo(ffmpeg) {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };
        foreach (var arg in new[] {
                     "-nostdin", "-hide_banner", "-loglevel", "error",
                     "-i", path,
                     "-lavfi",
                     $"showspectrumpic=s=1920x512:legend=0:color=intensity:scale=log:fscale=log:gain=2:mode=combined:orientation=vertical:start=20{StopOption(stopHz)}:drange=120:limit=0",
                     "-frames:v", "1",
                     "-f", "image2pipe",
                     "-vcodec", "png",
                     "pipe:1"
                 })
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        if (process is null)
            return null;

        using var kill = token.Register(() => {
            try {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch {
                // Process already exited.
            }
        });

        var errTask = process.StandardError.ReadToEndAsync();
        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        if (!process.WaitForExit(120_000)) {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return null;
        }

        token.ThrowIfCancellationRequested();
        if (process.ExitCode != 0) {
            Debug.WriteLine($"Spectrogram ffmpeg: {errTask.GetAwaiter().GetResult()}");
            return null;
        }

        return ms.ToArray();
    }

    private static string StopOption(int stopHz) =>
        stopHz > 0 ? $":stop={stopHz}" : "";
}
