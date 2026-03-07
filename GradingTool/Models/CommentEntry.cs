namespace GradingTool.Models;

using System.Text.Json.Serialization;

public enum CommentSeverity
{
    Aucun,
    Mineur,
    Majeur,
    Critique
}

public class CommentEntry
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CommentSeverity Severity { get; set; } = CommentSeverity.Aucun;
}
