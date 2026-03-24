using GradingTool.Models;
using GradingTool.Services;

namespace GradingTool.Tests.Services;

public class GridServiceTests
{
    private readonly GridService _sut = new();

    // Échelle de référence : A(100), B(80), C(60), D(40), E(0)
    private static List<ScaleItemModel> Scale5() =>
    [
        new() { Qualitative = "A", Points = 100 },
        new() { Qualitative = "B", Points = 80 },
        new() { Qualitative = "C", Points = 60 },
        new() { Qualitative = "D", Points = 40 },
        new() { Qualitative = "E", Points = 0 }
    ];

    private static CommentEntry Mineur(string text = "x") =>
        new() { Text = text, Severity = CommentSeverity.Mineur };

    private static CommentEntry Majeur(string text = "x") =>
        new() { Text = text, Severity = CommentSeverity.Majeur };

    private static CommentEntry Critique(string text = "x") =>
        new() { Text = text, Severity = CommentSeverity.Critique };

    private static CommentEntry Aucun(string text = "x") =>
        new() { Text = text, Severity = CommentSeverity.Aucun };

    // -----------------------------------------------------------------------
    // Cas limites : retour null
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_NoFeedback_ReturnsNull()
    {
        var result = _sut.GetResultRecommendation([], Scale5());
        Assert.Null(result);
    }

    [Fact]
    public void GetResultRecommendation_OnlyBlankFeedback_ReturnsNull()
    {
        var feedback = new[] { new CommentEntry { Text = "  ", Severity = CommentSeverity.Mineur } };
        var result = _sut.GetResultRecommendation(feedback, Scale5());
        Assert.Null(result);
    }

    [Fact]
    public void GetResultRecommendation_ScaleTooShort_ReturnsNull()
    {
        var scale = new[] { new ScaleItemModel { Qualitative = "A", Points = 100 } };
        var result = _sut.GetResultRecommendation([Mineur()], scale);
        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // Aucune erreur de sévérité → A (aucun drop)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_OnlyAucunSeverity_ReturnsTopGrade()
    {
        var result = _sut.GetResultRecommendation([Aucun()], Scale5());
        Assert.Equal("A", result);
    }

    // -----------------------------------------------------------------------
    // 1 Mineur → drop de 1 → B
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_OneMineur_DropsOne()
    {
        var result = _sut.GetResultRecommendation([Mineur()], Scale5());
        Assert.Equal("B", result);
    }

    // -----------------------------------------------------------------------
    // 2 Mineurs → nMineur > 2 est faux → drop de 1 → B (même que 1 mineur)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_TwoMineurs_DropOne()
    {
        var result = _sut.GetResultRecommendation([Mineur(), Mineur()], Scale5());
        Assert.Equal("B", result);
    }

    // -----------------------------------------------------------------------
    // 3+ Mineurs → nMineur > 2 devient vrai → drop de 2 → C
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_ThreeMineurs_DropTwo()
    {
        var result = _sut.GetResultRecommendation([Mineur(), Mineur(), Mineur()], Scale5());
        Assert.Equal("C", result);
    }

    // -----------------------------------------------------------------------
    // 1 Majeur → drop de 2 → C
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_OneMajeur_DropTwo()
    {
        var result = _sut.GetResultRecommendation([Majeur()], Scale5());
        Assert.Equal("C", result);
    }

    // -----------------------------------------------------------------------
    // 2 Majeurs → drop de 3 → D
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_TwoMajeurs_DropThree()
    {
        var result = _sut.GetResultRecommendation([Majeur(), Majeur()], Scale5());
        Assert.Equal("D", result);
    }

    // -----------------------------------------------------------------------
    // 1 Critique → drop = maxDrop = count-2 = 3 → scaleList[3] = D
    // Note : le dernier niveau (E) est intentionnellement hors d'atteinte par l'algo
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_OneCritique_DropsToMaxDrop()
    {
        var result = _sut.GetResultRecommendation([Critique()], Scale5());
        Assert.Equal("D", result);
    }

    // -----------------------------------------------------------------------
    // Critique prend toujours le dessus, même avec d'autres sévérités
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_CritiquePlusMajeur_DropsToMaxDrop()
    {
        var result = _sut.GetResultRecommendation([Critique(), Majeur(), Mineur()], Scale5());
        Assert.Equal("D", result);
    }

    // -----------------------------------------------------------------------
    // Le drop ne peut pas dépasser maxDrop (scaleCount - 2)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_DropCappedAtMaxDrop()
    {
        // Échelle courte : A(100), B(0) → maxDrop = 0
        var shortScale = new[]
        {
            new ScaleItemModel { Qualitative = "A", Points = 100 },
            new ScaleItemModel { Qualitative = "B", Points = 0 }
        };

        // Même avec un Critique, le résultat ne peut pas descendre plus bas que B
        var result = _sut.GetResultRecommendation([Critique()], shortScale);
        Assert.Equal("A", result);
    }

    // -----------------------------------------------------------------------
    // Les entrées vides/blanches ne comptent pas comme feedback réel
    // -----------------------------------------------------------------------

    [Fact]
    public void GetResultRecommendation_MixedBlankAndRealFeedback_OnlyCountsReal()
    {
        var feedback = new[]
        {
            new CommentEntry { Text = "", Severity = CommentSeverity.Critique },
            Mineur()
        };

        // Seul le Mineur compte → drop de 1 → B
        var result = _sut.GetResultRecommendation(feedback, Scale5());
        Assert.Equal("B", result);
    }
}
