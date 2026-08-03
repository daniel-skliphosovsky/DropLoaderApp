using System.Globalization;
using System.Resources;

namespace MediaHub;

/// <summary>
/// Thin wrapper over the AppResources ResourceManager. Strings are resolved
/// against the current UI culture at call time, so a language switch takes
/// effect immediately for any string fetched after the switch.
/// </summary>
public static class Loc
{
    /// <summary>
    /// Preferences key holding the user's chosen language code ("ru"/"en").
    /// </summary>
    public const string LanguagePreferenceKey = "mediahub.language";

    private static readonly ResourceManager Resources =
        new("MediaHub.Resources.Strings.AppResources", typeof(Loc).Assembly);

    /// <summary>Two-letter code of the active UI language.</summary>
    public static string CurrentCode =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    /// <summary>
    /// Activates the given language ("ru"/"en") for the UI thread and future
    /// threads, since the downloaders run on the thread pool.
    /// </summary>
    public static void SetLanguage(string code)
    {
        CultureInfo culture = new CultureInfo(code);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static string Get(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Get(string key, params object?[] args)
    {
        string format = Get(key);
        return args.Length == 0 ? format : string.Format(CultureInfo.CurrentUICulture, format, args);
    }

    // Popup strings are fetched through x:Static, which is evaluated when each
    // popup instance is created, so they always reflect the current language
    // without any change notifications.
    public static string PopupStop => Get(LocKeys.PopupStop);
    public static string PopupStopHint => Get(LocKeys.PopupStopHint);
    public static string PopupInformation => Get(LocKeys.InfoButton);
    public static string PopupLoading => Get(LocKeys.PopupLoading);
    public static string PopupClose => Get(LocKeys.PopupClose);
    public static string PopupCloseHint => Get(LocKeys.PopupCloseHint);
}
