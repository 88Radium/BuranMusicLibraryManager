namespace Buran.Types {
    public class ParsedMetadata {
        public List<string> Artists          { get; set; } = new List<string>();
        public string?      Title            { get; set; }
        public string?      Album            { get; set; }
        public string?      TrackNumber      { get; set; }
        public string?      Year             { get; set; }
        public List<string> Comments         { get; set; } = new List<string>(); // Neue Property für mehrfache Comments
        public int?         MatchedPatternId { get; set; }

        public override string ToString() {
            return
                $"Artists: {string.Join(", ", Artists)}, Title: {Title ?? "[null]"}, Album: {Album ?? "[null]"}, Track: {TrackNumber ?? "[null]"}, Comments: {string.Join("; ", Comments)}";
        }
    }
}