namespace GradingTool.Services;

public interface ICourseService
{
    IEnumerable<string> GetCourses(string sessionName);
    void CreateCourse(string sessionName, string courseName);
    void DeleteCourse(string sessionName, string courseName);
    void RenameCourse(string sessionName, string oldName, string newName);
    bool HasSubdirectories(string sessionName, string courseName);
}
