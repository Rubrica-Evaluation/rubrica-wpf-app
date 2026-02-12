using System.Text.Json.Serialization;

namespace GradingTool.Models;

public class GroupModel
{
    public string GroupCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<StudentModel> Students { get; set; } = new();
    public int StudentCount => Students.Count;
}

public class RosterModel
{
    public List<StudentModel> Students { get; set; } = new();
    public List<GroupModel> Groups { get; set; } = new();
    public bool HasGroups => Groups.Count > 0;
}