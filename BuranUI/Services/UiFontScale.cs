using System;
using System.Collections.Generic;
using Avalonia;

namespace BuranUI.Services;

public static class UiFontScale {
    public const string Small  = "small";
    public const string Medium = "medium";
    public const string Large  = "large";

    public static string Normalize(string? code) {
        if (string.Equals(code, Small, StringComparison.OrdinalIgnoreCase))
            return Small;
        if (string.Equals(code, Large, StringComparison.OrdinalIgnoreCase))
            return Large;
        return Medium;
    }

    public static void Apply(string? code) {
        if (Application.Current is not { } app)
            return;

        foreach (var pair in ResourcesFor(Normalize(code)))
            app.Resources[pair.Key] = pair.Value;
    }

    private static Dictionary<string, double> ResourcesFor(string code) => code switch {
        Small => Scale(15, 10, 10, 11, 10, 13, 22, 11, 11, 26),
        Large => Scale(22, 13, 13, 16, 14, 18, 30, 16, 16, 36),
        _     => Scale(18, 11, 11, 13, 12, 15, 24, 13, 13, 30)
    };

    private static Dictionary<string, double> Scale(
        double title, double subtitle, double heading, double body, double caption,
        double empty, double minHeight, double tab, double tree, double tabMinHeight) => new() {
        ["Font.Title"]            = title,
        ["Font.Subtitle"]         = subtitle,
        ["Font.Heading"]          = heading,
        ["Font.Body"]             = body,
        ["Font.Caption"]          = caption,
        ["Font.Empty"]            = empty,
        ["Control.MinHeight"]     = minHeight,
        ["Font.Tab"]              = tab,
        ["Font.Tree"]             = tree,
        ["Control.TabMinHeight"]  = tabMinHeight
    };
}
