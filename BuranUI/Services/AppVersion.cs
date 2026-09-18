using System.Reflection;

namespace BuranUI.Services;

public static class AppVersion {
    public static string Display {
        get {
            var raw = typeof(AppVersion).Assembly
                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion
                      ?? typeof(AppVersion).Assembly.GetName().Version?.ToString(3)
                      ?? "0.0";
            var plus = raw.IndexOf('+');
            return plus >= 0 ? raw[..plus] : raw;
        }
    }
}
