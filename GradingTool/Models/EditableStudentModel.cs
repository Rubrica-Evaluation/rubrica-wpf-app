using CommunityToolkit.Mvvm.ComponentModel;

namespace GradingTool.Models;

public partial class EditableStudentModel : ObservableObject
{
    [ObservableProperty]
    private string _da = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private int _team;

    public static EditableStudentModel FromStudent(StudentModel student) => new()
    {
        Da = student.Da,
        FirstName = student.FirstName,
        LastName = student.LastName,
        Team = student.Team
    };

    public StudentModel ToStudent() => new()
    {
        Da = Da,
        FirstName = FirstName,
        LastName = LastName,
        Team = Team
    };
}
