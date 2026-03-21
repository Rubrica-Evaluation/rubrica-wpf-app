using GradingTool.Models;
using GradingTool.Services;
using NSubstitute;
using System.IO;

namespace GradingTool.Tests.Services;

public class RubricServiceTests
{
    [Fact]
    public void CreateEmptyRubric_WorkNameProvided_InitializesDraft()
    {
        var sessionsRootService = Substitute.For<ISessionsRootService>();
        var sut = new RubricService(sessionsRootService);

        var rubric = sut.CreateEmptyRubric("TP synthèse");

        Assert.Equal("TP synthèse", rubric.Meta.Tp);
        Assert.NotNull(rubric.Meta.Student);
        Assert.Empty(rubric.Criteria);
        Assert.Empty(rubric.Penalties);
        Assert.Null(rubric.Computed.Total);
    }

    [Fact]
    public void ValidateRubricFormat_MissingPenalty_ReturnsFalse()
    {
        var sessionsRootService = Substitute.For<ISessionsRootService>();
        var sut = new RubricService(sessionsRootService);
        var rubric = BuildValidRubric();
        rubric.Penalties.Clear();

        var isValid = sut.ValidateRubricFormat(rubric, out var errorMessage);

        Assert.False(isValid);
        Assert.Contains("pénalités", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaveRubric_InvalidRubric_ReturnsFalse()
    {
        var sessionsRootService = Substitute.For<ISessionsRootService>();
        sessionsRootService.GetSessionsRootPath().Returns(Path.GetTempPath());
        var sut = new RubricService(sessionsRootService);
        var rubric = sut.CreateEmptyRubric("TP1");

        var saved = sut.SaveRubric("Hiver 2026", "BD1", "TP1", rubric);

        Assert.False(saved);
    }

    private static RubricModel BuildValidRubric()
    {
        return new RubricModel
        {
            Meta = new RubricMeta
            {
                Tp = "TP1",
                Student = new StudentModel()
            },
            Penalties =
            [
                new PenaltyItemModel
                {
                    Label = "Retard",
                    Count = 0,
                    Factor = -10,
                    Reason = string.Empty,
                    Min = -30
                }
            ],
            Criteria =
            [
                new CriterionModel
                {
                    Label = "Analyse",
                    Weight = 100,
                    Scale =
                    [
                        new ScaleItemModel
                        {
                            Qualitative = "A",
                            Label = "Excellent",
                            Points = 100
                        }
                    ]
                }
            ],
            Computed = new ComputedModel()
        };
    }
}