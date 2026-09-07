using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.DBEditor.Models;

public enum TagKind {
    Genre,
    Mood
}

public partial class AssignableTag : ObservableObject {
    public TagKind Kind { get; }
    public int     Id   { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool   _isAssigned;
    [ObservableProperty] private bool   _canAssign;
    [ObservableProperty] private bool   _isFilterActive;

    private readonly Action<AssignableTag, bool>? _onAssignedChanged;
    private          bool                         _suppressAssigned;

    public AssignableTag(TagKind kind, int id, string name, bool isAssigned, Action<AssignableTag, bool>? onAssignedChanged) {
        Kind                 = kind;
        Id                   = id;
        _name                = name;
        _isAssigned          = isAssigned;
        _onAssignedChanged   = onAssignedChanged;
    }

    public void SetAssignedSilent(bool value) {
        _suppressAssigned = true;
        IsAssigned        = value;
        _suppressAssigned = false;
    }

    partial void OnIsAssignedChanged(bool value) {
        if (!_suppressAssigned)
            _onAssignedChanged?.Invoke(this, value);
    }
}
