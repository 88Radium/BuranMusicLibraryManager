using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Documents;
using Avalonia.Media;
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

    private const double SplitterHitWidth = 5;
    // Fluent header reserves 12px left padding, a 32px sort-icon slot, and a 1px separator
    // even when the icon is hidden. Cell text adds 12px margin each side plus our 6px padding.
    private const double SortSlotWidth = 32;
    private const double SeparatorWidth = 1;
    private const double CellTextMargin = 12;

    private Id3EditorTabViewModel? _hookedVm;
    private bool _columnRefreshQueued;
    private bool _rebuildingColumns;

    public Id3EditorTab() {
        InitializeComponent();
        DataContextChanged += (_, _) => HookColumns();
        AttachedToVisualTree += (_, _) => QueueColumnRefresh();
        SizeChanged += (_, _) => ClampInspectorWidth();
        HookColumns();
        if (this.FindControl<DataGrid>("TracksGrid") is { } tracks)
            tracks.AddHandler(InputElement.PointerPressedEvent, OnHeaderSplitterPressed, RoutingStrategies.Tunnel);
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
        if ((e.Source as Visual)?.FindAncestorOfType<DataGridColumnHeader>() != null)
            return;
        if (DataContext is Id3EditorTabViewModel vm && vm.FocusedFile is { } file)
            vm.PlayThisFileCommand.Execute(file);
    }

    private void OnHeaderSplitterPressed(object? sender, PointerPressedEventArgs e) {
        if (e.ClickCount != 2 || sender is not DataGrid grid)
            return;
        if (!e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
            return;
        if (e.Source is not Control source)
            return;
        var header = source as DataGridColumnHeader ?? source.FindAncestorOfType<DataGridColumnHeader>();
        if (header == null || header.Bounds.Width <= 0)
            return;

        var hit = DataGridColumn.GetColumnContainingElement(header);
        if (hit == null)
            return;

        var x = e.GetPosition(header).X;
        var onRight = header.Bounds.Width - x <= SplitterHitWidth;
        var onLeft  = x <= SplitterHitWidth;
        if (!onLeft && !onRight)
            return;

        var sizeThisColumn = onRight && (!onLeft || x >= header.Bounds.Width / 2);
        var target = sizeThisColumn ? hit : PreviousVisibleColumn(grid, hit);
        if (target is not { CanUserResize: true })
            return;

        target.Width = new DataGridLength(FitColumnWidth(grid, header, target), DataGridLengthUnitType.Pixel);
        e.Handled = true;
    }

    private static DataGridColumn? PreviousVisibleColumn(DataGrid grid, DataGridColumn column) =>
        grid.Columns
            .Where(c => c.IsVisible && c.DisplayIndex < column.DisplayIndex)
            .OrderByDescending(c => c.DisplayIndex)
            .FirstOrDefault();

    private static double FitColumnWidth(DataGrid grid, DataGridColumnHeader header, DataGridColumn column) {
        var headerWidth = Measure(column.Header?.ToString() ?? "", header)
                          + header.Padding.Left + header.Padding.Right + SortSlotWidth + SeparatorWidth;

        var cell = grid.GetVisualDescendants().OfType<DataGridCell>().FirstOrDefault();
        var cellPad = cell?.Padding.Left + cell?.Padding.Right ?? 12;
        var widest = WidestCell(grid, column, cell ?? (Control)header);
        var content = widest <= 0 ? 0 : widest + cellPad + CellTextMargin * 2 + SeparatorWidth;

        var optimal = Math.Max(headerWidth, content);
        var viewport = grid.Bounds.Width;
        if (viewport > headerWidth)
            optimal = Math.Min(optimal, viewport);
        optimal = Math.Max(optimal, column.MinWidth);
        if (!double.IsPositiveInfinity(column.MaxWidth))
            optimal = Math.Min(optimal, column.MaxWidth);
        return Math.Ceiling(optimal);
    }

    private static double WidestCell(DataGrid grid, DataGridColumn column, AvaloniaObject style) {
        if (grid.ItemsSource is not IEnumerable items)
            return 0;
        if (column is not DataGridBoundColumn { Binding: Binding { Path: { Length: > 0 } path } })
            return 0;

        PropertyInfo? property = null;
        var widest = 0.0;
        foreach (var item in items) {
            if (item == null)
                continue;
            property ??= item.GetType().GetProperty(path);
            var text = property?.GetValue(item)?.ToString();
            if (string.IsNullOrEmpty(text))
                continue;
            foreach (var line in text.Split('\r', '\n'))
                widest = Math.Max(widest, Measure(line, style));
        }

        return widest;
    }

    private static double Measure(string text, AvaloniaObject style) {
        if (text.Length == 0)
            return 0;
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                style.GetValue(TextElement.FontFamilyProperty),
                style.GetValue(TextElement.FontStyleProperty),
                style.GetValue(TextElement.FontWeightProperty)),
            style.GetValue(TextElement.FontSizeProperty),
            Brushes.Black);
        return formatted.Width;
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