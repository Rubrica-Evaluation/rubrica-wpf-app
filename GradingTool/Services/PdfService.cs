using GradingTool.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Fonts;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace GradingTool.Services;

public class PdfService : IPdfService
{
    private readonly IGridService _gridService;

    public PdfService(IGridService gridService)
    {
        _gridService = gridService;
        // Enable Windows fonts for better font support
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
    }

    public async Task<bool> ExportPdfAsync(GridModel grid, string outputPath)
    {
        try
        {
            var document = new PdfDocument();
            document.Info.Title = $"Grille - {grid.Meta?.Tp} - {grid.Meta?.Student?.FirstName} {grid.Meta?.Student?.LastName}";

            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var titleFont = new XFont("Arial", 14, XFontStyleEx.Bold);
            var sectionFont = new XFont("Arial", 12, XFontStyleEx.Bold);
            var regularFont = new XFont("Arial", 11, XFontStyleEx.Regular);
            var boldRegularFont = new XFont("Arial", 11, XFontStyleEx.Bold);
            var tableHeaderFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var tableFont = new XFont("Arial", 10, XFontStyleEx.Regular);

            double y = 40;
            double pageWidth = page.Width;
            double leftMargin = 40;
            double rightMargin = 40;
            double contentWidth = pageWidth - leftMargin - rightMargin;

            // Title
            string title = $"Grille d'évaluation — {grid.Meta?.Tp}";
            gfx.DrawString(title, titleFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 20), XStringFormats.TopLeft);
            y += 30;

            // Student info
            if (grid.Meta?.Student != null)
            {
                gfx.DrawString($"Étudiant : {grid.Meta.Student.FirstName} {grid.Meta.Student.LastName}", regularFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 16), XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString($"DA : {grid.Meta.Student.Da}", regularFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 16), XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString($"Groupe : {grid.Meta.Student.Group}", regularFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 16), XStringFormats.TopLeft);
                y += 18;
            }

            y += 15;

            // "Synthèse" section with table
            gfx.DrawString("Synthèse", sectionFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 18), XStringFormats.TopLeft);
            y += 25;

            // Table headers
            double col1Width = 230; // Critère
            double col2Width = 70;  // Poids (%)
            double col3Width = 70;  // Résultat
            double col4Width = 80;  // Points

            double tableX = leftMargin;
            double rowHeight = 18;

            // Draw header row with border
            DrawTableCell(gfx, tableX, y, col1Width, rowHeight, "Critère", tableHeaderFont, true);
            DrawTableCell(gfx, tableX + col1Width, y, col2Width, rowHeight, "Poids (%)", tableHeaderFont, true);
            DrawTableCell(gfx, tableX + col1Width + col2Width, y, col3Width, rowHeight, "Résultat", tableHeaderFont, true);
            DrawTableCell(gfx, tableX + col1Width + col2Width + col3Width, y, col4Width, rowHeight, "Points", tableHeaderFont, true);
            y += rowHeight;

            // Draw criteria rows
            foreach (var criterion in grid.Criteria)
            {
                string resultText = criterion.Result ?? "—";
                string pointsText = criterion.Points?.ToString("F2") ?? "0.00";

                // Handle wrapped text for criterion labels
                var wrappedLabel = WrapText(gfx, criterion.Label, tableFont, col1Width - 8);
                double cellHeight = Math.Max(wrappedLabel.Count * 14 + 4, rowHeight);

                // Draw row with variable height
                DrawMultilineTableCell(gfx, tableX, y, col1Width, cellHeight, wrappedLabel, tableFont, false);
                DrawTableCell(gfx, tableX + col1Width, y, col2Width, cellHeight, criterion.Weight.ToString(), tableFont, false);
                DrawTableCell(gfx, tableX + col1Width + col2Width, y, col3Width, cellHeight, resultText, tableFont, false);
                DrawTableCell(gfx, tableX + col1Width + col2Width + col3Width, y, col4Width, cellHeight, pointsText, tableFont, false);
                y += cellHeight;
            }

            // Draw penalty rows
            if (grid.Penalties.Any())
            {
                foreach (var penalty in grid.Penalties)
                {
                    string resultText = penalty.Count.ToString();
                    string pointsText = penalty.ComputedPenalty.ToString("F2");

                    // Handle wrapped text for penalty labels
                    var wrappedLabel = WrapText(gfx, penalty.Label, tableFont, col1Width - 8);
                    double cellHeight = Math.Max(wrappedLabel.Count * 14 + 4, rowHeight);

                    // Draw row
                    DrawMultilineTableCell(gfx, tableX, y, col1Width, cellHeight, wrappedLabel, tableFont, false);
                    DrawTableCell(gfx, tableX + col1Width, y, col2Width, cellHeight, "—", tableFont, false);
                    DrawTableCell(gfx, tableX + col1Width + col2Width, y, col3Width, cellHeight, resultText, tableFont, false);
                    DrawTableCell(gfx, tableX + col1Width + col2Width + col3Width, y, col4Width, cellHeight, pointsText, tableFont, false);
                    y += cellHeight;
                }
            }

            // Draw total row
            string totalText = grid.Computed?.Total?.ToString("F2") ?? "0.00";
            DrawTableCell(gfx, tableX, y, col1Width, rowHeight, "TOTAL", tableHeaderFont, true);
            DrawTableCell(gfx, tableX + col1Width, y, col2Width, rowHeight, "100", tableHeaderFont, true);
            DrawTableCell(gfx, tableX + col1Width + col2Width, y, col3Width, rowHeight, "", tableHeaderFont, true);
            DrawTableCell(gfx, tableX + col1Width + col2Width + col3Width, y, col4Width, rowHeight, totalText, tableHeaderFont, true);
            y += rowHeight + 15;

            // "Rétroaction par critère" section
            gfx.DrawString("Rétroaction par critère", sectionFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 18), XStringFormats.TopLeft);
            y += 25;

            // Draw feedback for each criterion
            foreach (var criterion in grid.Criteria)
            {
                string labelText = $"{criterion.Label} — Résultat: {criterion.Result ?? "—"}";
                
                // Draw criterion label in bold
                gfx.DrawString(labelText, boldRegularFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 20), XStringFormats.TopLeft);
                y += 18;
                
                y += 4;
                
                // Draw feedback - each comment on its own line with proper spacing
                if (criterion.Feedback != null && criterion.Feedback.Count > 0)
                {
                    foreach (var feedback in criterion.Feedback)
                    {
                        // Wrap text for long feedback items
                        var wrappedFeedback = WrapText(gfx, feedback.Text, tableFont, contentWidth - 20);
                        
                        // Draw bullet point
                        gfx.DrawString("•", tableFont, XBrushes.Black, new XRect(leftMargin, y, 10, 14), XStringFormats.TopLeft);
                        
                        // Draw feedback lines with indentation
                        double feedbackX = leftMargin + 15;
                        foreach (var line in wrappedFeedback)
                        {
                            gfx.DrawString(line, tableFont, XBrushes.Black, new XRect(feedbackX, y, contentWidth - 20, 14), XStringFormats.TopLeft);
                            y += 14;
                        }
                        
                        y += 2; // Extra spacing between items
                    }
                }
                else
                {
                    gfx.DrawString("—", tableFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 16), XStringFormats.TopLeft);
                    y += 18;
                }
                
                y += 4;

                // Check if we need a new page
                if (y > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }
            }

            // "Raisons des pénalités" section
            if (grid.Penalties.Any(p => !string.IsNullOrWhiteSpace(p.Reason)))
            {
                // Check if we need a new page
                if (y > page.Height - 100)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }

                y += 15;
                gfx.DrawString("Raisons des pénalités", sectionFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 18), XStringFormats.TopLeft);
                y += 25;

                foreach (var penalty in grid.Penalties.Where(p => !string.IsNullOrWhiteSpace(p.Reason)))
                {
                    string labelText = $"{penalty.Label} — Nombre: {penalty.Count}";
                    
                    // Draw penalty label in bold
                    gfx.DrawString(labelText, boldRegularFont, XBrushes.Black, new XRect(leftMargin, y, contentWidth, 20), XStringFormats.TopLeft);
                    y += 18;
                    
                    y += 4;
                    
                    // Draw reason with proper text wrapping
                    var wrappedReason = WrapText(gfx, penalty.Reason, tableFont, contentWidth - 15);
                    
                    // Draw bullet point
                    gfx.DrawString("•", tableFont, XBrushes.Black, new XRect(leftMargin, y, 10, 14), XStringFormats.TopLeft);
                    
                    // Draw reason lines with indentation
                    double reasonX = leftMargin + 15;
                    foreach (var line in wrappedReason)
                    {
                        gfx.DrawString(line, tableFont, XBrushes.Black, new XRect(reasonX, y, contentWidth - 20, 14), XStringFormats.TopLeft);
                        y += 14;
                    }
                    
                    y += 6; // Extra spacing between items

                    // Check if we need a new page
                    if (y > page.Height - 40)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 40;
                    }
                }
            }

            document.Save(outputPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void DrawTableCell(XGraphics gfx, double x, double y, double width, double height, string text, XFont font, bool isHeader)
    {
        // Draw border
        gfx.DrawRectangle(isHeader ? XPens.Black : new XPen(XColor.FromArgb(200, 200, 200)), x, y, width, height);

        // Draw background for header
        if (isHeader)
        {
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), x, y, width, height);
        }

        // Draw text with padding
        var rect = new XRect(x + 4, y + 2, width - 8, height - 4);
        gfx.DrawString(text, font, XBrushes.Black, rect, XStringFormats.TopLeft);
    }

    private void DrawMultilineTableCell(XGraphics gfx, double x, double y, double width, double height, List<string> lines, XFont font, bool isHeader)
    {
        // Draw border
        gfx.DrawRectangle(isHeader ? XPens.Black : new XPen(XColor.FromArgb(200, 200, 200)), x, y, width, height);

        // Draw background for header
        if (isHeader)
        {
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), x, y, width, height);
        }

        // Draw text lines with padding
        double lineY = y + 2;
        foreach (var line in lines)
        {
            var rect = new XRect(x + 4, lineY, width - 8, 14);
            gfx.DrawString(line, font, XBrushes.Black, rect, XStringFormats.TopLeft);
            lineY += 14;
        }
    }

    private List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var lines = new List<string>();
        
        // D'abord diviser par les retours de ligne explicites (\r\n ou \n)
        var paragraphs = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        foreach (var paragraph in paragraphs)
        {
            // Pour chaque paragraphe, appliquer le wrapping
            var words = paragraph.Split(' ');
            var currentLine = "";
            
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                var size = gfx.MeasureString(testLine, font);

                if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
            
            // Si le paragraphe était vide (deux retours de ligne à la suite), ajouter une ligne vide
            if (string.IsNullOrWhiteSpace(paragraph) && lines.Count > 0 && !string.IsNullOrEmpty(lines[lines.Count - 1]))
            {
                lines.Add("");
            }
        }

        return lines;
    }

    public async Task<bool> ExportGroupPdfsAsync(string groupGradingPath, string groupPdfPath)
    {
        try
        {
            // Ensure pdf_docs directory exists
            Directory.CreateDirectory(groupPdfPath);

            // Get all JSON files in grading/group/
            var jsonFiles = Directory.GetFiles(groupGradingPath, "*.json");
            if (jsonFiles.Length == 0)
            {
                return false;
            }

            var pdfFiles = new List<string>();

            foreach (var jsonFile in jsonFiles)
            {
                var grid = await _gridService.LoadGridAsync(jsonFile);
                if (grid != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(jsonFile) + ".pdf";
                    var pdfPath = Path.Combine(groupPdfPath, fileName);
                    var success = await ExportPdfAsync(grid, pdfPath);
                    if (success)
                    {
                        pdfFiles.Add(pdfPath);
                    }
                }
            }

            if (pdfFiles.Count == 0)
            {
                return false;
            }

            // Create zip
            var zipPath = Path.Combine(groupPdfPath, "travaux.zip");
            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var pdfFile in pdfFiles)
                {
                    zipArchive.CreateEntryFromFile(pdfFile, Path.GetFileName(pdfFile));
                }
            }

            // Open the folder
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = groupPdfPath,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}