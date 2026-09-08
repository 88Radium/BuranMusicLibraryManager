using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Buran.ID3Editor.Models;
using Buran.Localization;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.ID3Editor.ViewModels;

public enum CompareFilesDecision {
    KeepBoth,
    UseCurrent,
    UseExisting
}

public partial class CompareFilesViewModel : ObservableObject {
    public CompareFilesViewModel(Mp3FileObject current, string existingPath) {
        CurrentFileName  = current.FileName;
        ExistingFileName = Path.GetFileName(existingPath);
        CurrentPath      = current.FullPath;
        ExistingPath     = existingPath;

        var currentSnap  = FileCompareSnapshot.FromMp3(current);
        var existingSnap = FileCompareSnapshot.FromPath(existingPath);
        Rows             = new ObservableCollection<FileCompareRow>(BuildRows(currentSnap, existingSnap));
        DifferenceCount  = Rows.Count(r => r.IsDifferent);

        L.WhenChanged(() => {
            OnPropertyChanged(nameof(Hint));
            OnPropertyChanged(nameof(DifferenceText));
            OnPropertyChanged(nameof(CurrentColumnHeader));
            OnPropertyChanged(nameof(ExistingColumnHeader));
        });
    }

    public event EventHandler? CloseRequested;

    public CompareFilesDecision Decision { get; private set; } = CompareFilesDecision.KeepBoth;

    public string CurrentFileName  { get; }
    public string ExistingFileName { get; }
    public string CurrentPath      { get; }
    public string ExistingPath     { get; }

    public ObservableCollection<FileCompareRow> Rows { get; }

    public int DifferenceCount { get; }

    public string Hint => L.Format("Compare.Hint", ExistingFileName);

    public string DifferenceText => DifferenceCount == 0
        ? L.Get("Compare.NoDifferences")
        : L.Format("Compare.DifferenceCount", DifferenceCount);

    public string CurrentColumnHeader  => L.Format("Compare.CurrentFile", CurrentFileName);
    public string ExistingColumnHeader => L.Format("Compare.ExistingFile", ExistingFileName);

    [RelayCommand]
    private void ChooseKeepBoth() => CloseWith(CompareFilesDecision.KeepBoth);

    [RelayCommand]
    private void ChooseUseCurrent() => CloseWith(CompareFilesDecision.UseCurrent);

    [RelayCommand]
    private void ChooseUseExisting() => CloseWith(CompareFilesDecision.UseExisting);

    private void CloseWith(CompareFilesDecision decision) {
        Decision = decision;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static IEnumerable<FileCompareRow> BuildRows(FileCompareSnapshot current, FileCompareSnapshot existing) {
        yield return Row(L.Get("Compare.Size"), FormatSize(current.SizeBytes), FormatSize(existing.SizeBytes));
        yield return Row(L.Get("Compare.Modified"), FormatDate(current.Modified), FormatDate(existing.Modified));
        yield return Row(L.Get("Compare.Duration"), FormatDuration(current.DurationSeconds, current.AudioRead), FormatDuration(existing.DurationSeconds, existing.AudioRead));
        yield return Row(L.Get("Compare.Bitrate"), FormatBitrate(current), FormatBitrate(existing));
        yield return Row(L.Get("Compare.SampleRate"), FormatSampleRate(current.SampleRate, current.AudioRead), FormatSampleRate(existing.SampleRate, existing.AudioRead));
        yield return Row(L.Get("Compare.Channels"), FormatAudioText(current.Channels, current.AudioRead), FormatAudioText(existing.Channels, existing.AudioRead));
        yield return Row(L.Get("Compare.BitDepth"), FormatBitDepth(current.BitDepth, current.AudioRead), FormatBitDepth(existing.BitDepth, existing.AudioRead));
        yield return Row(L.Get("Compare.Format"), FormatAudioText(current.Format, current.AudioRead), FormatAudioText(existing.Format, existing.AudioRead));
        yield return Row(L.Get("Id3.Title"), FormatTag(current.Title, current.AudioRead), FormatTag(existing.Title, existing.AudioRead));
        yield return Row(L.Get("Common.Artists"), FormatTag(current.Artists, current.AudioRead), FormatTag(existing.Artists, existing.AudioRead));
        yield return Row(L.Get("Id3.Album"), FormatTag(current.Album, current.AudioRead), FormatTag(existing.Album, existing.AudioRead));
        yield return Row(L.Get("Id3.ReleaseYear"), FormatTag(current.Year, current.AudioRead), FormatTag(existing.Year, existing.AudioRead));
    }

    private static FileCompareRow Row(string label, string current, string existing) => new() {
        Label         = label,
        CurrentValue  = current,
        ExistingValue = existing,
        IsDifferent   = !string.Equals(current, existing, StringComparison.CurrentCultureIgnoreCase)
    };

    private static string FormatSize(long bytes) {
        if (bytes <= 0)
            return L.Get("Common.None");
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return string.Create(CultureInfo.CurrentCulture, $"{bytes / 1024d:0.#} KB");
        return string.Create(CultureInfo.CurrentCulture, $"{bytes / (1024d * 1024d):0.##} MB");
    }

    private static string FormatDate(DateTimeOffset? value) =>
        value is null ? L.Get("Common.None") : value.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    private static string FormatDuration(double seconds, bool audioRead) {
        if (!audioRead)
            return L.Get("Compare.CouldNotRead");
        if (seconds <= 0)
            return L.Get("Common.None");
        var time = TimeSpan.FromSeconds(seconds);
        return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
    }

    private static string FormatBitrate(FileCompareSnapshot snap) {
        if (!snap.AudioRead)
            return L.Get("Compare.CouldNotRead");
        if (snap.Bitrate <= 0)
            return L.Get("Common.None");
        var rounded = Math.Round(snap.Bitrate);
        var text    = string.Create(CultureInfo.CurrentCulture, $"{rounded:0} kBit/s");
        return snap.IsVbr ? $"{text} (VBR)" : text;
    }

    private static string FormatSampleRate(double hz, bool audioRead) {
        if (!audioRead)
            return L.Get("Compare.CouldNotRead");
        if (hz <= 0)
            return L.Get("Common.None");
        var rounded = Math.Round(hz);
        return Math.Abs(rounded % 1000) < 0.001
            ? $"{rounded / 1000:0} kHz"
            : string.Create(CultureInfo.CurrentCulture, $"{hz / 1000d:0.###} kHz");
    }

    private static string FormatBitDepth(int bits, bool audioRead) {
        if (!audioRead)
            return L.Get("Compare.CouldNotRead");
        return bits > 0 ? $"{bits} bit" : L.Get("Compare.NotApplicable");
    }

    private static string FormatAudioText(string? value, bool audioRead) {
        if (!audioRead)
            return L.Get("Compare.CouldNotRead");
        return string.IsNullOrWhiteSpace(value) ? L.Get("Common.None") : value;
    }

    private static string FormatTag(string? value, bool audioRead) {
        if (!audioRead)
            return L.Get("Compare.CouldNotRead");
        return string.IsNullOrWhiteSpace(value) ? L.Get("Common.None") : value;
    }
}
