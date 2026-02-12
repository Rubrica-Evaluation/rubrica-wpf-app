using System.Text.Json.Serialization;

namespace GradingTool.Models;

public class GridModel
{
    [JsonPropertyName("meta")]
    public GridMeta Meta { get; set; } = new();

    [JsonPropertyName("penalties")]
    public List<PenaltyItemModel> Penalties { get; set; } = new();

    [JsonPropertyName("criteria")]
    public List<CriterionModel> Criteria { get; set; } = new();

    [JsonPropertyName("computed")]
    public ComputedModel Computed { get; set; } = new();
}

public class GridMeta : RubricMeta
{
    [JsonPropertyName("members")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GridMemberModel>? Members { get; set; }
}

public class GridMemberModel
{
    [JsonPropertyName("da")]
    public string Da { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    public string DisplayName
    {
        get
        {
            var fullName = $"{FirstName} {LastName}".Trim();
            if (fullName.Length > 25 && !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName} {LastName[0]}.";
            }
            return fullName;
        }
    }
}