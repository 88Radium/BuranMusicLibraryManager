namespace Buran.ID3Editor.Models;

public sealed class FileCompareRow {
    public required string Label         { get; init; }
    public required string CurrentValue  { get; init; }
    public required string ExistingValue { get; init; }
    public bool            IsDifferent   { get; init; }
}
