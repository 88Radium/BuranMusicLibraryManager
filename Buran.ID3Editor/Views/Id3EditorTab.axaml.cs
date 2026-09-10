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

    private Id3EditorTabViewModel? _hookedVm;
    private bool _columnRefreshQueued;
    private bool _rebuildingColumns;

    public Id3EditorTab() {
        InitializeComponent();
        DataContextChanged += (_, _) => HookColumns();
        AttachedToVisualTree += (_, _) => QueueColumnRefresh();
        SizeChanged += (_, _) => ClampInspectorWidth();
        HookColumns();
    }

    private void HookColumns() {
        if (ReferenceEquals(_hookedVm, DataContext as Id3EditorTabViewModel) && _hookedVm is not null)
            return;

        if (_hookedVm is not null) {
            _hookedVm.ColumnsChanged -= OnColumnsChanged;
            _hookedVm.PropertyChanged -= OnVmPropertyChanged;
            foreach (var column in _hookedVm.ColumnOptions)
                column.PropertyChanged -= OnColumnPropertyChanged;
            _hookedVm = null;
        }

        if (DataContext is not Id3EditorTabViewModel vm)
            return;

        _hookedVm = vm;
        vm.ColumnsChanged += OnColumnsChanged;
        vm.PropertyChanged += OnVmPropertyChanged;
        foreach (var column in vm.ColumnOptions)
            column.PropertyChanged += OnColumnPropertyChanged;
    }

    private void OnColumnsChanged(object? sender, EventArgs e) => QueueColumnRefresh();

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
            QueueColumnRefresh();
    }

    private void QueueColumnRefresh() {
        if (_columnRefreshQueued)
            return;
        _columnRefreshQueued = true;
        Dispatcher.UIThread.Post(() => {
            _columnRefreshQueued = false;
            RebuildColumns();
        }, DispatcherPriority.Background);
    }

    private void RebuildColumns() {
        if (_rebuildingColumns)
            return;
        var grid = this.FindControl<DataGrid>("TracksGrid");
        if (grid is null || DataContext is not Id3EditorTabViewModel vm)
            return;

        _rebuildingColumns = true;
        try {
            if (grid.Columns.Count == 0) {
                grid.Columns.Add(new DataGridCheckBoxColumn {
                    Binding        = new Binding(nameof(Mp3FileObject.IsSelected)),
                    Width          = new DataGridLength(36, DataGridLengthUnitType.Pixel),
                    MinWidth       = 36,
                    MaxWidth       = 36,
                    IsReadOnly     = false,
                    CanUserResize  = false,
                    CanUserReorder = false
                });
                foreach (var option in vm.ColumnOptions) {
                    grid.Columns.Add(new DataGridTextColumn {
                        Header        = option.Header,
                        Binding       = new Binding(option.Binding),
                        Width         = new DataGridLength(option.DefaultWidth, DataGridLengthUnitType.Pixel),
                        MinWidth      = option.MinWidth,
                        IsReadOnly    = true,
                        CanUserResize = true,
                        Tag           = option.Id,
                        IsVisible     = option.IsVisible
                    });
                }
                return;
            }

            var columns = grid.Columns.ToList();
            foreach (var option in vm.ColumnOptions) {
                var column = columns.FirstOrDefault(c => Equals(c.Tag, option.Id));
                if (column is null)
                    continue;
                if (column.IsVisible != option.IsVisible)
                    column.IsVisible = option.IsVisible;
                ConvertStarWidthToPixel(column, option.DefaultWidth);
            }
        }
        finally {
            _rebuildingColumns = false;
        }
    }

    private static void ConvertStarWidthToPixel(DataGridColumn column, double fallbackWidth) {
        if (column.Width.UnitType != DataGridLengthUnitType.Star)
            return;
        var pixels = column.ActualWidth > 1 ? column.ActualWidth : fallbackWidth;
        column.Width = new DataGridLength(pixels, DataGridLengthUnitType.Pixel);
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