using System.Globalization;
using System.Windows;

namespace GradingTool.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IConfigurationService _configurationService;

    public string CurrentLanguage { get; private set; } = "fr";

    public event Action? LanguageChanged;

    public string this[string key]
    {
        get
        {
            if (Application.Current?.Resources[key] is string value)
                return value;
            return key;
        }
    }

    public LocalizationService(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        var savedLanguage = configurationService.LoadLanguage() ?? DetectSystemLanguage();
        _configurationService.SaveLanguage(savedLanguage);
        ApplyLanguage(savedLanguage);
    }

    private static string DetectSystemLanguage()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return twoLetter == "fr" ? "fr" : "en";
    }

    public void SetLanguage(string language)
    {
        if (language == CurrentLanguage)
            return;

        _configurationService.SaveLanguage(language);
        ApplyLanguage(language);
        LanguageChanged?.Invoke();
    }

    private void ApplyLanguage(string language)
    {
        CurrentLanguage = language;

        var dicts = Application.Current.Resources.MergedDictionaries;

        var existing = dicts.FirstOrDefault(d =>
            d.Source != null && d.Source.OriginalString.Contains("/Resources/Strings."));

        if (existing != null)
            dicts.Remove(existing);

        var uri = new Uri(
            $"pack://application:,,,/GradingTool;component/Resources/Strings.{language}.xaml",
            UriKind.Absolute);

        dicts.Add(new ResourceDictionary { Source = uri });
    }
}
