namespace GradingTool.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string language);
    string this[string key] { get; }
    event Action? LanguageChanged;
}
