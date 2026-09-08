using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Buran.ID3Editor.Types;
using Buran.Localization;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.ID3Editor.ViewModels;

public partial class BulkCommentsViewModel : ObservableObject {
    private readonly List<Mp3FileObject> _files;

    public BulkCommentsViewModel(IEnumerable<Mp3FileObject> files) {
        _files        = files.ToList();
        SelectedFiles = new ObservableCollection<Mp3FileObject>(_files);
        PreviewItems  = new ObservableCollection<TagPreviewItem>();
        LoadFromSelection();
        L.WhenChanged(() => {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(Hint));
            UpdatePreview();
        });
    }

    public event EventHandler? CloseRequested;

    public ObservableCollection<Mp3FileObject>  SelectedFiles { get; }
    public ObservableCollection<TagPreviewItem> PreviewItems  { get; }

    public string WindowTitle => L.Get("Bulk.CommentsTitle");
    public string Hint        => L.Get("Bulk.CommentsHint");

    [ObservableProperty] private string _newComment = "";

    public bool CanApply => _files.Count > 0 && HasDelta();

    partial void OnNewCommentChanged(string value) {
        UpdatePreview();
        OnPropertyChanged(nameof(CanApply));
        ApplyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ClearComment() {
        NewComment = "";
    }

    [RelayCommand]
    private void LoadFromSelection() {
        var comments = _files
            .Select(f => f.Id3Comment ?? "")
            .Distinct(StringComparer.Ordinal)
            .ToList();
        NewComment = comments.Count == 1 ? comments[0] : "";
        UpdatePreview();
    }

    [RelayCommand]
    private void Apply() {
        if (!CanApply)
            return;

        var next = NewComment ?? "";
        foreach (var file in _files)
            file.Id3Comment = next;

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private bool HasDelta() {
        var next = NewComment ?? "";
        return _files.Any(f => !string.Equals(f.Id3Comment ?? "", next, StringComparison.Ordinal));
    }

    private void UpdatePreview() {
        PreviewItems.Clear();
        var next = NewComment ?? "";
        foreach (var file in _files) {
            var old = file.Id3Comment ?? "";
            PreviewItems.Add(new TagPreviewItem {
                FileName = file.FileName,
                OldTags  = string.IsNullOrEmpty(old) ? L.Get("Common.None") : old,
                NewTags  = string.IsNullOrEmpty(next) ? L.Get("Common.None") : next
            });
        }
    }
}
