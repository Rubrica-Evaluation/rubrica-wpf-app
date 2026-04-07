using System.Text.Json.Serialization;

namespace GradingTool.Models;

public class GroupModel
{
    public string GroupCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<StudentModel> Students { get; set; } = new();

    [JsonIgnore]
    public int StudentCount => Students.Count;
}

public class RosterModel
{
    public List<GroupModel> Groups { get; set; } = new();

    [JsonIgnore]
    public bool HasGroups => Groups.Count > 0;
}