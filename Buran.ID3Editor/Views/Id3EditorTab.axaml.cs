using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Buran.ID3Editor.Models;
using Buran.ID3Editor.ViewModels;
using Buran.Types;

namespace Buran.ID3Editor.Views;

public partial class Id3EditorTab : UserControl {
    private const double MinInspectorWidth = 200;
    private const double MinListWidth      = 160;

    private bool   _splitDragging;
    private double _splitStartWidth;
    private double _splitStartX;

    public Id3EditorTab() {
        InitializeComponent();
        DataContextChanged += (_, _) => HookColumns();
        AttachedToVisualTree += (_, _) => RebuildColumns();
        SizeChanged += (_, _) => ClampInspectorWidth();
    }

    private void HookColumns() {
        if (DataContext is not Id3EditorTabViewModel vm)
            return;
        vm.ColumnsChanged += (_, _) => RebuildColumns();
        vm.PropertyChanged += OnVmPropertyChanged;
        foreach (var column in vm.ColumnOptions)
            column.PropertyChanged += OnColumnPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Id3EditorTabViewModel.IsEditMode))
            Dispatcher.UIThread.Post(ClampInspectorWidth, DispatcherPriority.Render);
    }

    private void ClampInspectorWidth() {
        if (DataContext is not Id3EditorTabViewModel vm || !vm.IsEditMode)
            return;
        var grid = this.FindControl<Grid>("EditorSplitGrid");
        if (grid is null || grid.Bounds.Width <= 0)
            return;
        var max = Math.Max(MinInspectorWidth, grid.Bounds.Width - MinListWidth - 6);
        if (vm.InspectorPaneWidth > max)
            vm.InspectorPaneWidth = max;
    }

    private void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(TrackColumnOption.IsVisible))
            RebuildColumns();
    }

    private void RebuildColumns() {
        var grid = this.FindControl<DataGrid>("TracksGrid");
        if (grid is null || DataContext is not Id3EditorTabViewModel vm)
            return;

        grid.Columns.Clear();
        grid.Columns.Add(new DataGridCheckBoxColumn {
            Binding    = new Binding(nameof(Mp3FileObject.IsSelected)),
            Width      = new DataGridLength(36),
            IsReadOnly = false,
            CanUserReorder = false
        });
        foreach (var option in vm.ColumnOptions.Where(c => c.IsVisible)) {
            grid.Columns.Add(new DataGridTextColumn {
                Header         = option.Header,
                Binding        = new Binding(option.Binding),
                Width          = new DataGridLength(1, DataGridLengthUnitType.Star),
                IsReadOnly     = true,
                Tag            = option.Id
            });
        }
    }

    private void Tracks_DoubleTapped(object? sender, TappedEventArgs e) {
        if (DataContext is Id3EditorTabViewModel vm && vm.FocusedFile is { } file)
            vm.PlayThisFileCommand.Execute(file);
    }

    private void InspectorSplitter_PointerPressed(object? sender, PointerPressedEventArgs e) {
        if (DataContext is not Id3EditorTabViewModel vm)
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        var grid = this.FindControl<Grid>("EditorSplitGrid");
        _splitDragging   = true;
        _splitStartWidth = vm.InspectorPaneWidth;
        _splitStartX     = e.GetPosition((Visual?)grid ?? this).X;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void InspectorSplitter_PointerMoved(object? sender, PointerEventArgs e) {
        if (!_splitDragging || DataContext is not Id3EditorTabViewModel vm)
            return;
        var grid  = this.FindControl<Grid>("EditorSplitGrid");
        var host  = (Visual?)grid ?? this;
        var delta = e.GetPosition(host).X - _splitStartX;
        var max   = Math.Max(MinInspectorWidth, host.Bounds.Width - MinListWidth - 6);
        vm.InspectorPaneWidth = Math.Clamp(_splitStartWidth - delta, MinInspectorWidth, max);
    }

    private void InspectorSplitter_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        EndSplitDrag(e.Pointer);
    }

    private void InspectorSplitter_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) {
        _splitDragging = false;
    }

    private void EndSplitDrag(IPointer pointer) {
        if (!_splitDragging)
            return;
        _splitDragging = false;
        pointer.Capture(null);
    }

    private void Tracks_ContextRequested(object? sender, ContextRequestedEventArgs e) {
        var source = e.Source as Control;
        var row    = source?.FindAncestorOfType<DataGridRow>();
        if (row?.DataContext is Mp3FileObject file && DataContext is Id3EditorTabViewModel vm)
            vm.FocusedFile = file;
    }

    private void CatalogAddBox_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter || sender is not AutoCompleteBox { DataContext: Mp3FileObject file } box)
            return;
        if (DataContext is not Id3EditorTabViewModel vm)
            return;
        if (box.IsDropDownOpen)
            return;

        switch (box.Tag as string) {
            case "Artist":
                vm.AddSingleArtistFromID3Tags(file);
                break;
            case "Genre":
                vm.AddSingleGenreFromID3Tags(file);
                break;
            case "Mood":
                vm.AddSingleMoodFromID3Tags(file);
                break;
        }

        e.Handled = true;
    }
}