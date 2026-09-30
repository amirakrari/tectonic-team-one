namespace Tectonic.Web.Services;

// Gradient = page background for the whole app (and the swatch). Accent = color for links, switches,
// active nav and buttons, taken from the gradient and dark enough for white text.
public record ThemeOption(string Id, string Name, string Gradient, string Accent);

public static class AppearanceCatalog
{
    public const string DefaultAccent = "#0097DB"; // KBC blue

    // Gradients from uiGradients (https://uigradients.com, MIT license). Each starts dark at the
    // top-left, where page headings sit, so white headings stay readable.
    public static readonly IReadOnlyList<ThemeOption> Themes =
    [
        new("witching-hour", "Witching Hour", "linear-gradient(135deg, #240b36 0%, #c31432 100%)", "#C31432"),
        new("vice-city", "Vice City", "linear-gradient(135deg, #3494e6 0%, #ec6ead 100%)", "#2F7FD0"),
        new("twitch", "Twitch", "linear-gradient(135deg, #2a0845 0%, #6441a5 100%)", "#6441A5"),
        new("harvey", "Harvey", "linear-gradient(135deg, #1f4037 0%, #99f2c8 100%)", "#2A6B55"),
        new("kyoto", "Kyoto", "linear-gradient(135deg, #c21500 0%, #ffc500 100%)", "#C21500"),
        new("frost", "Frost", "linear-gradient(135deg, #000428 0%, #004e92 100%)", "#004E92"),
    ];

    public static readonly IReadOnlyList<string> Tones = ["Young", "Professional", "Joyful", "Factual"];
}
