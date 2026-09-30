namespace Tectonic.Web.Services;

// Per-circuit UI settings shared by the layout and the Settings, Rules, Appearance and Email pages.
// Not persisted: the API has no settings endpoint yet.
public class UiState
{
    public string UserName { get; set; } = "John Doe";
    public string Email { get; set; } = "john.doe@example.com";
    public bool Prive { get; set; } = true;
    public Lang Language { get; private set; } = Lang.En;
    public bool NotificationsEnabled { get; set; } = true;

    // Regular alerts go into one summary email at 18:00; critical ones still arrive instantly.
    public bool DailyDigest { get; set; }

    // Only email about expenses marked critical (see Recurring expenses page).
    public bool CriticalOnly { get; private set; }

    // Email reminder style (Appearance > Email reminders).
    public string EmailTone { get; private set; } = "Formal";
    public string EmailDesign { get; private set; } = "Classic";
    public string EmailTextSize { get; private set; } = "Regular";
    public bool EmailEmoji { get; private set; }
    public bool DarkMode { get; private set; }

    public bool Personalize { get; private set; } = true;
    public string ThemeId { get; private set; } = "";
    public string Tone { get; private set; } = "Factual";

    public Dictionary<string, bool> RulesEnabled { get; } =
        RuleCatalog.All.ToDictionary(r => r.Id, r => r.EnabledByDefault);

    // The chosen theme only applies while Personalize is on; otherwise the app uses KBC blue.
    public ThemeOption? ActiveTheme =>
        Personalize ? AppearanceCatalog.Themes.FirstOrDefault(t => t.Id == ThemeId) : null;

    public string Accent => ActiveTheme?.Accent ?? AppearanceCatalog.DefaultAccent;

    public event Action? Changed;

    public void SetLanguage(Lang value) => Set(() => Language = value);

    public void SetDarkMode(bool value) => Set(() => DarkMode = value);

    public void SetRuleEnabled(string id, bool enabled) => Set(() => RulesEnabled[id] = enabled);

    public void SetPersonalize(bool value) => Set(() => Personalize = value);

    public void SetTheme(string id) => Set(() => ThemeId = ThemeId == id ? "" : id);

    public void SetTone(string tone) => Set(() => Tone = tone);

    public void SetCriticalOnly(bool value) => Set(() => CriticalOnly = value);

    public void SetEmailTone(string value) => Set(() => EmailTone = value);

    public void SetEmailDesign(string value) => Set(() => EmailDesign = value);

    public void SetEmailTextSize(string value) => Set(() => EmailTextSize = value);

    public void SetEmailEmoji(bool value) => Set(() => EmailEmoji = value);

    private void Set(Action change)
    {
        change();
        Changed?.Invoke();
    }
}
