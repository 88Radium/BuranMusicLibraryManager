using System.Text;

namespace Buran.Localization;

public enum HelpBlockKind {
    Title,
    Heading,
    Subheading,
    Paragraph,
    Bullet
}

public sealed class HelpBlock {
    public HelpBlockKind Kind { get; init; }
    public string        Text { get; init; } = "";

    public bool IsTitle      => Kind == HelpBlockKind.Title;
    public bool IsHeading    => Kind == HelpBlockKind.Heading;
    public bool IsSubheading => Kind == HelpBlockKind.Subheading;
    public bool IsParagraph  => Kind == HelpBlockKind.Paragraph;
    public bool IsBullet     => Kind == HelpBlockKind.Bullet;
}

public static class HelpDocument {
    public static IReadOnlyList<HelpBlock> Load() {
        var code = LocalizationService.Instance.ResolvedCode;
        var markdown = ReadMarkdown(code);
        if (string.IsNullOrWhiteSpace(markdown) &&
            !string.Equals(code, LocalizationService.FallbackCode, StringComparison.OrdinalIgnoreCase))
            markdown = ReadMarkdown(LocalizationService.FallbackCode);

        return Parse(markdown ?? "");
    }

    public static IReadOnlyList<HelpBlock> Parse(string markdown) {
        var blocks     = new List<HelpBlock>();
        var paragraph  = new StringBuilder();

        void FlushParagraph() {
            var text = paragraph.ToString().Trim();
            paragraph.Clear();
            if (text.Length > 0)
                blocks.Add(new HelpBlock { Kind = HelpBlockKind.Paragraph, Text = text });
        }

        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n')) {
            var line = raw.TrimEnd();
            if (line.StartsWith("### ", StringComparison.Ordinal)) {
                FlushParagraph();
                blocks.Add(new HelpBlock { Kind = HelpBlockKind.Subheading, Text = line[4..].Trim() });
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal)) {
                FlushParagraph();
                blocks.Add(new HelpBlock { Kind = HelpBlockKind.Heading, Text = line[3..].Trim() });
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal)) {
                FlushParagraph();
                blocks.Add(new HelpBlock { Kind = HelpBlockKind.Title, Text = line[2..].Trim() });
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal)) {
                FlushParagraph();
                blocks.Add(new HelpBlock { Kind = HelpBlockKind.Bullet, Text = "• " + line[2..].Trim() });
            }
            else if (string.IsNullOrWhiteSpace(line)) {
                FlushParagraph();
            }
            else {
                if (paragraph.Length > 0)
                    paragraph.Append(' ');
                paragraph.Append(line.Trim());
            }
        }

        FlushParagraph();
        return blocks;
    }

    private static string? ReadMarkdown(string culture) {
        var assembly = typeof(HelpDocument).Assembly;
        var name     = $"Buran.Localization.Help.{culture}.md";
        var stream   = assembly.GetManifestResourceStream(name);
        if (stream is null) {
            var suffix = $".Help.{culture}.md";
            var match  = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                stream = assembly.GetManifestResourceStream(match);
        }

        if (stream is null)
            return null;

        using (stream)
        using (var reader = new StreamReader(stream))
            return reader.ReadToEnd();
    }
}
