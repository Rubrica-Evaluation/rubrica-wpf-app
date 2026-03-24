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

    private const double LeftMargin = 40;
    private const double RightMargin = 40;
    private const double RowHeight = 18;
    private const double ColCritereWidth = 230;
    private const double ColPoidsWidth = 70;
    private const double ColResultatWidth = 100;
    private const double ColPointsWidth = 80;
    private const double Col2X = LeftMargin + ColCritereWidth;
    private const double Col3X = Col2X + ColPoidsWidth;
    private const double Col4X = Col3X + ColResultatWidth;

    public PdfService(IGridService gridService)
    {
        _gridService = gridService;
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
    }

    public async Task<bool> ExportPdfAsync(GridModel grid, string outputPath)
    {
        try
        {
            var document = CreateDocument(grid);
            var ctx = new RenderContext(document, CreateFonts());

            DrawDocumentHeader(ctx, grid);
            DrawSynthesisSection(ctx, grid);
            DrawFeedbackSection(ctx, grid);
            DrawPenaltyReasonsSection(ctx, grid);

            document.Save(outputPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static PdfDocument CreateDocument(GridModel grid)
    {
        var document = new PdfDocument();
        document.Info.Title = $"Grille - {grid.Meta?.Tp} - {grid.Meta?.Student?.FirstName} {grid.Meta?.Student?.LastName}";
        return document;
    }

    private static DocumentFonts CreateFonts() => new(
        Title:       new XFont("Arial", 14, XFontStyleEx.Bold),
        Section:     new XFont("Arial", 12, XFontStyleEx.Bold),
        Regular:     new XFont("Arial", 11, XFontStyleEx.Regular),
        BoldRegular: new XFont("Arial", 11, XFontStyleEx.Bold),
        TableHeader: new XFont("Arial", 10, XFontStyleEx.Bold),
        Table:       new XFont("Arial", 10, XFontStyleEx.Regular)
    );

    private void DrawDocumentHeader(RenderContext ctx, GridModel grid)
    {
        ctx.Gfx.DrawString($"Grille d'évaluation — {grid.Meta?.Tp}", ctx.Fonts.Title, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 20), XStringFormats.TopLeft);
        ctx.Y += 30;

        if (grid.Meta?.Student != null)
            DrawStudentInfo(ctx, grid.Meta.Student);

        ctx.Y += 15;
    }

    private void DrawStudentInfo(RenderContext ctx, StudentModel student)
    {
        ctx.Gfx.DrawString($"Étudiant : {student.FirstName} {student.LastName}", ctx.Fonts.Regular, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 16), XStringFormats.TopLeft);
        ctx.Y += 18;
        ctx.Gfx.DrawString($"DA : {student.Da}", ctx.Fonts.Regular, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 16), XStringFormats.TopLeft);
        ctx.Y += 18;
        ctx.Gfx.DrawString($"Groupe : {student.Group}", ctx.Fonts.Regular, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 16), XStringFormats.TopLeft);
        ctx.Y += 18;
    }

    private void DrawSynthesisSection(RenderContext ctx, GridModel grid)
    {
        ctx.Gfx.DrawString("Synthèse", ctx.Fonts.Section, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 18), XStringFormats.TopLeft);
        ctx.Y += 25;

        DrawSynthesisTableHeader(ctx);
        DrawCriteriaRows(ctx, grid);
        DrawPenaltyRows(ctx, grid);
        DrawTotalRow(ctx, grid);
    }

    private void DrawSynthesisTableHeader(RenderContext ctx)
    {
        DrawTableCell(ctx.Gfx, LeftMargin, ctx.Y, ColCritereWidth,  RowHeight, "Critère",   ctx.Fonts.TableHeader, true);
        DrawTableCell(ctx.Gfx, Col2X,      ctx.Y, ColPoidsWidth,    RowHeight, "Poids (%)", ctx.Fonts.TableHeader, true);
        DrawTableCell(ctx.Gfx, Col3X,      ctx.Y, ColResultatWidth, RowHeight, "Résultat",  ctx.Fonts.TableHeader, true);
        DrawTableCell(ctx.Gfx, Col4X,      ctx.Y, ColPointsWidth,   RowHeight, "Points",    ctx.Fonts.TableHeader, true);
        ctx.Y += RowHeight;
    }

    private void DrawCriteriaRows(RenderContext ctx, GridModel grid)
    {
        foreach (var criterion in grid.Criteria)
        {
            var wrappedLabel  = WrapText(ctx.Gfx, criterion.Label,        ctx.Fonts.Table, ColCritereWidth  - 8);
            var wrappedResult = WrapText(ctx.Gfx, criterion.Result ?? "—", ctx.Fonts.Table, ColResultatWidth - 8);
            double cellHeight = Math.Max(Math.Max(wrappedLabel.Count, wrappedResult.Count) * 14 + 4, RowHeight);

            DrawMultilineTableCell(ctx.Gfx, LeftMargin, ctx.Y, ColCritereWidth,  cellHeight, wrappedLabel,  ctx.Fonts.Table, false);
            DrawTableCell(ctx.Gfx, Col2X, ctx.Y, ColPoidsWidth,    cellHeight, criterion.Weight.ToString(),              ctx.Fonts.Table, false);
            DrawMultilineTableCell(ctx.Gfx, Col3X,      ctx.Y, ColResultatWidth, cellHeight, wrappedResult, ctx.Fonts.Table, false);
            DrawTableCell(ctx.Gfx, Col4X, ctx.Y, ColPointsWidth,   cellHeight, criterion.Points?.ToString("F2") ?? "0.00", ctx.Fonts.Table, false);
            ctx.Y += cellHeight;
        }
    }

    private void DrawPenaltyRows(RenderContext ctx, GridModel grid)
    {
        foreach (var penalty in grid.Penalties)
        {
            var wrappedLabel = WrapText(ctx.Gfx, penalty.Label, ctx.Fonts.Table, ColCritereWidth - 8);
            double cellHeight = Math.Max(wrappedLabel.Count * 14 + 4, RowHeight);

            DrawMultilineTableCell(ctx.Gfx, LeftMargin, ctx.Y, ColCritereWidth,  cellHeight, wrappedLabel, ctx.Fonts.Table, false);
            DrawTableCell(ctx.Gfx, Col2X, ctx.Y, ColPoidsWidth,    cellHeight, "—",                             ctx.Fonts.Table, false);
            DrawTableCell(ctx.Gfx, Col3X, ctx.Y, ColResultatWidth, cellHeight, penalty.Count.ToString(),        ctx.Fonts.Table, false);
            DrawTableCell(ctx.Gfx, Col4X, ctx.Y, ColPointsWidth,   cellHeight, penalty.ComputedPenalty.ToString("F2"), ctx.Fonts.Table, false);
            ctx.Y += cellHeight;
        }
    }

    private void DrawTotalRow(RenderContext ctx, GridModel grid)
    {
        string totalText = grid.Computed?.Total?.ToString("F2") ?? "0.00";
        DrawTableCell(ctx.Gfx, LeftMargin, ctx.Y, ColCritereWidth,  RowHeight, "TOTAL",   ctx.Fonts.TableHeader, true);
        DrawTableCell(ctx.Gfx, Col2X,      ctx.Y, ColPoidsWidth,    RowHeight, "100",     ctx.Fonts.TableHeader, true);
        DrawTableCell(ctx.Gfx, Col3X,      ctx.Y, ColResultatWidth, RowHeight, "",        ctx.Fonts.TableHeader, true);
        DrawTableCell(ctx.Gfx, Col4X,      ctx.Y, ColPointsWidth,   RowHeight, totalText, ctx.Fonts.TableHeader, true);
        ctx.Y += RowHeight + 15;
    }

    private void DrawFeedbackSection(RenderContext ctx, GridModel grid)
    {
        var criteriaWithFeedback = grid.Criteria
            .Where(c => c.Feedback != null && c.Feedback.Count > 0)
            .ToList();

        if (criteriaWithFeedback.Count == 0)
            return;

        ctx.Gfx.DrawString("Rétroaction par critère", ctx.Fonts.Section, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 18), XStringFormats.TopLeft);
        ctx.Y += 25;

        foreach (var criterion in criteriaWithFeedback)
            DrawCriterionFeedback(ctx, criterion);
    }

    private void DrawCriterionFeedback(RenderContext ctx, CriterionModel criterion)
    {
        var scale = criterion.Scale.FirstOrDefault(s => s.Qualitative == criterion.Result);
        var resultText = scale != null
            ? $"{scale.Qualitative} — {scale.Label}"
            : criterion.Result ?? "—";

        ctx.Gfx.DrawString($"{criterion.Label} — Résultat : {resultText}", ctx.Fonts.BoldRegular, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 20), XStringFormats.TopLeft);
        ctx.Y += 22;

        DrawFeedbackItems(ctx, criterion.Feedback!);

        ctx.Y += 4;
        ctx.EnsureSpace(40);
    }

    private void DrawFeedbackItems(RenderContext ctx, IEnumerable<CommentEntry> feedbackItems)
    {
        foreach (var feedback in feedbackItems)
        {
            var lines = WrapText(ctx.Gfx, feedback.Text, ctx.Fonts.Table, ctx.ContentWidth - 20);
            ctx.Gfx.DrawString("•", ctx.Fonts.Table, XBrushes.Black,
                new XRect(LeftMargin, ctx.Y, 10, 14), XStringFormats.TopLeft);

            foreach (var line in lines)
            {
                ctx.Gfx.DrawString(line, ctx.Fonts.Table, XBrushes.Black,
                    new XRect(LeftMargin + 15, ctx.Y, ctx.ContentWidth - 20, 14), XStringFormats.TopLeft);
                ctx.Y += 14;
            }
            ctx.Y += 2;
        }
    }

    private void DrawPenaltyReasonsSection(RenderContext ctx, GridModel grid)
    {
        var penaltiesWithReason = grid.Penalties.Where(p => !string.IsNullOrWhiteSpace(p.Reason)).ToList();
        if (penaltiesWithReason.Count == 0) return;

        ctx.EnsureSpace(100);
        ctx.Y += 15;
        ctx.Gfx.DrawString("Raisons des pénalités", ctx.Fonts.Section, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 18), XStringFormats.TopLeft);
        ctx.Y += 25;

        foreach (var penalty in penaltiesWithReason)
            DrawPenaltyReason(ctx, penalty);
    }

    private void DrawPenaltyReason(RenderContext ctx, PenaltyItemModel penalty)
    {
        ctx.Gfx.DrawString($"{penalty.Label} — Nombre : {penalty.Count} fois -{-penalty.ComputedPenalty:F1} pts", ctx.Fonts.BoldRegular, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, ctx.ContentWidth, 20), XStringFormats.TopLeft);
        ctx.Y += 22;

        var lines = WrapText(ctx.Gfx, penalty.Reason, ctx.Fonts.Table, ctx.ContentWidth - 15);
        ctx.Gfx.DrawString("•", ctx.Fonts.Table, XBrushes.Black,
            new XRect(LeftMargin, ctx.Y, 10, 14), XStringFormats.TopLeft);

        foreach (var line in lines)
        {
            ctx.Gfx.DrawString(line, ctx.Fonts.Table, XBrushes.Black,
                new XRect(LeftMargin + 15, ctx.Y, ctx.ContentWidth - 20, 14), XStringFormats.TopLeft);
            ctx.Y += 14;
        }

        ctx.Y += 6;
        ctx.EnsureSpace(40);
    }

    private static void DrawTableCell(XGraphics gfx, double x, double y, double width, double height, string text, XFont font, bool isHeader)
    {
        gfx.DrawRectangle(isHeader ? XPens.Black : new XPen(XColor.FromArgb(200, 200, 200)), x, y, width, height);
        if (isHeader)
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), x, y, width, height);
        gfx.DrawString(text, font, XBrushes.Black, new XRect(x + 4, y + 2, width - 8, height - 4), XStringFormats.TopLeft);
    }

    private static void DrawMultilineTableCell(XGraphics gfx, double x, double y, double width, double height, List<string> lines, XFont font, bool isHeader)
    {
        gfx.DrawRectangle(isHeader ? XPens.Black : new XPen(XColor.FromArgb(200, 200, 200)), x, y, width, height);
        if (isHeader)
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)), x, y, width, height);

        double lineY = y + 2;
        foreach (var line in lines)
        {
            gfx.DrawString(line, font, XBrushes.Black, new XRect(x + 4, lineY, width - 8, 14), XStringFormats.TopLeft);
            lineY += 14;
        }
    }

    private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var lines = new List<string>();
        var paragraphs = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var paragraph in paragraphs)
        {
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
                lines.Add(currentLine);

            if (string.IsNullOrWhiteSpace(paragraph) && lines.Count > 0 && !string.IsNullOrEmpty(lines[^1]))
                lines.Add("");
        }

        return lines;
    }

    public async Task<bool> ExportGroupPdfsAsync(string groupGradingPath, string groupPdfPath, bool overwrite = false)
    {
        try
        {
            Directory.CreateDirectory(groupPdfPath);

            var jsonFiles = Directory.GetFiles(groupGradingPath, "*.json");
            if (jsonFiles.Length == 0) return false;

            var pdfFiles = await GeneratePdfsFromJsonFiles(jsonFiles, groupPdfPath, overwrite);
            if (pdfFiles.Count == 0) return false;

            CreateZipArchive(pdfFiles, groupPdfPath);
            OpenFolder(groupPdfPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<string>> GeneratePdfsFromJsonFiles(string[] jsonFiles, string outputDirectory, bool overwrite)
    {
        var pdfFiles = new List<string>();
        foreach (var jsonFile in jsonFiles)
        {
            var pdfPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(jsonFile) + ".pdf");

            if (File.Exists(pdfPath) && !overwrite)
            {
                pdfFiles.Add(pdfPath);
                continue;
            }

            var grid = await _gridService.LoadGridAsync(jsonFile);
            if (grid == null) continue;

            if (await ExportPdfAsync(grid, pdfPath))
                pdfFiles.Add(pdfPath);
        }
        return pdfFiles;
    }

    private static void CreateZipArchive(List<string> pdfFiles, string outputDirectory)
    {
        var zipPath = Path.Combine(outputDirectory, "travaux.zip");
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        using var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var pdfFile in pdfFiles)
            zipArchive.CreateEntryFromFile(pdfFile, Path.GetFileName(pdfFile));
    }

    private static void OpenFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = folderPath,
            UseShellExecute = true
        });
    }

    private sealed class RenderContext
    {
        public PdfDocument Document { get; }
        public PdfPage Page { get; private set; }
        public XGraphics Gfx { get; private set; }
        public double Y { get; set; }
        public double ContentWidth { get; }
        public DocumentFonts Fonts { get; }

        public RenderContext(PdfDocument document, DocumentFonts fonts)
        {
            Document = document;
            Fonts = fonts;
            Page = document.AddPage();
            Gfx = XGraphics.FromPdfPage(Page);
            ContentWidth = Page.Width - PdfService.LeftMargin - PdfService.RightMargin;
            Y = PdfService.LeftMargin;
        }

        public void EnsureSpace(double requiredHeight)
        {
            if (Y > Page.Height - requiredHeight)
                AddPage();
        }

        private void AddPage()
        {
            Page = Document.AddPage();
            Gfx = XGraphics.FromPdfPage(Page);
            Y = PdfService.LeftMargin;
        }
    }

    private record DocumentFonts(
        XFont Title,
        XFont Section,
        XFont Regular,
        XFont BoldRegular,
        XFont TableHeader,
        XFont Table
    );
}