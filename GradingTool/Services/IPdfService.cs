using GradingTool.Models;

namespace GradingTool.Services;

public interface IPdfService
{
    Task<bool> ExportPdfAsync(GridModel grid, string outputPath);
    Task<bool> ExportGroupPdfsAsync(string groupGradingPath, string groupPdfPath);
}