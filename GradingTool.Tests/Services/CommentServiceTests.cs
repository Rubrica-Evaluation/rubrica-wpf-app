using System.IO;
using GradingTool.Models;
using GradingTool.Services;

namespace GradingTool.Tests.Services;

public class CommentServiceTests
{
    private static CommentService CreateSut() => new();

    private static CommentEntry Entry(string text, CommentSeverity severity = CommentSeverity.Aucun) =>
        new() { Text = text, Severity = severity };

    // -----------------------------------------------------------------------
    // GetCommentsForCriterion
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCommentsForCriterion_LabelNullOuVide_RetourneListeVide(string? label)
    {
        var sut = CreateSut();
        Assert.Empty(sut.GetCommentsForCriterion(label!));
    }

    [Fact]
    public void GetCommentsForCriterion_LabelInconnu_RetourneListeVide()
    {
        var sut = CreateSut();
        Assert.Empty(sut.GetCommentsForCriterion("Critère inexistant"));
    }

    [Fact]
    public void GetCommentsForCriterion_LabelConnu_RetourneCommentaires()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("bon travail", CommentSeverity.Majeur));

        var result = sut.GetCommentsForCriterion("Critère 1");

        Assert.Single(result);
        Assert.Equal("bon travail", result[0].Text);
        Assert.Equal(CommentSeverity.Majeur, result[0].Severity);
    }

    [Fact]
    public void GetCommentsForCriterion_RechercheInsensibleCasse()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère A", Entry("texte"));

        Assert.Single(sut.GetCommentsForCriterion("critère a"));
    }

    [Fact]
    public void GetCommentsForCriterion_RetourneCopie_PasReferenceInterne()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("original"));

        sut.GetCommentsForCriterion("Critère 1").Clear();

        Assert.Single(sut.GetCommentsForCriterion("Critère 1"));
    }

    // -----------------------------------------------------------------------
    // AddCommentForCriterion
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCommentForCriterion_LabelNullOuVide_NeFaitRien(string? label)
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion(label!, Entry("texte"));
        Assert.Empty(sut.GetCommentsForCriterion(""));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCommentForCriterion_TexteNullOuVide_NeFaitRien(string? text)
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry(text ?? ""));
        Assert.Empty(sut.GetCommentsForCriterion("Critère 1"));
    }

    [Fact]
    public void AddCommentForCriterion_NouveauCommentaire_AjouteALaListe()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("commentaire"));
        Assert.Single(sut.GetCommentsForCriterion("Critère 1"));
    }

    [Fact]
    public void AddCommentForCriterion_DoublonExact_NonAjoute()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("doublon"));
        sut.AddCommentForCriterion("Critère 1", Entry("doublon"));
        Assert.Single(sut.GetCommentsForCriterion("Critère 1"));
    }

    [Fact]
    public void AddCommentForCriterion_DoublonInsensibleCasse_NonAjoute()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("Doublon"));
        sut.AddCommentForCriterion("Critère 1", Entry("doublon"));
        Assert.Single(sut.GetCommentsForCriterion("Critère 1"));
    }

    [Fact]
    public void AddCommentForCriterion_DeuxCriteresDifferents_Independants()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère A", Entry("c1"));
        sut.AddCommentForCriterion("Critère B", Entry("c2"));

        Assert.Single(sut.GetCommentsForCriterion("Critère A"));
        Assert.Single(sut.GetCommentsForCriterion("Critère B"));
    }

    // -----------------------------------------------------------------------
    // UpdateCommentForCriterion
    // -----------------------------------------------------------------------

    [Fact]
    public void UpdateCommentForCriterion_LabelInconnu_NeFaitRien()
    {
        var sut = CreateSut();
        var ex = Record.Exception(() =>
            sut.UpdateCommentForCriterion("Critère inconnu", "ancien", Entry("nouveau")));
        Assert.Null(ex);
    }

    [Fact]
    public void UpdateCommentForCriterion_OldTextInconnu_NeFaitRien()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("original"));

        sut.UpdateCommentForCriterion("Critère 1", "texte inexistant", Entry("nouveau"));

        Assert.Equal("original", sut.GetCommentsForCriterion("Critère 1")[0].Text);
    }

    [Fact]
    public void UpdateCommentForCriterion_CasNominal_MetsAJourTextEtSeverite()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("ancien texte", CommentSeverity.Aucun));

        sut.UpdateCommentForCriterion("Critère 1", "ancien texte", Entry("nouveau texte", CommentSeverity.Majeur));

        var result = sut.GetCommentsForCriterion("Critère 1");
        Assert.Single(result);
        Assert.Equal("nouveau texte", result[0].Text);
        Assert.Equal(CommentSeverity.Majeur, result[0].Severity);
    }

    [Fact]
    public void UpdateCommentForCriterion_MatchInsensibleCasse()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("Texte Original"));

        sut.UpdateCommentForCriterion("Critère 1", "texte original", Entry("nouveau"));

        Assert.Equal("nouveau", sut.GetCommentsForCriterion("Critère 1")[0].Text);
    }

    // -----------------------------------------------------------------------
    // RemoveCommentForCriterion
    // -----------------------------------------------------------------------

    [Fact]
    public void RemoveCommentForCriterion_LabelInconnu_NeFaitRien()
    {
        var sut = CreateSut();
        var ex = Record.Exception(() =>
            sut.RemoveCommentForCriterion("Critère inconnu", "texte"));
        Assert.Null(ex);
    }

    [Fact]
    public void RemoveCommentForCriterion_TexteInconnu_NeFaitRien()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("original"));

        sut.RemoveCommentForCriterion("Critère 1", "texte inexistant");

        Assert.Single(sut.GetCommentsForCriterion("Critère 1"));
    }

    [Fact]
    public void RemoveCommentForCriterion_CasNominal_RetireLeCommentaire()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("à supprimer"));
        sut.AddCommentForCriterion("Critère 1", Entry("à conserver"));

        sut.RemoveCommentForCriterion("Critère 1", "à supprimer");

        var result = sut.GetCommentsForCriterion("Critère 1");
        Assert.Single(result);
        Assert.Equal("à conserver", result[0].Text);
    }

    [Fact]
    public void RemoveCommentForCriterion_MatchInsensibleCasse()
    {
        var sut = CreateSut();
        sut.AddCommentForCriterion("Critère 1", Entry("Texte"));

        sut.RemoveCommentForCriterion("Critère 1", "texte");

        Assert.Empty(sut.GetCommentsForCriterion("Critère 1"));
    }

    // -----------------------------------------------------------------------
    // SaveCommentsAsync
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveCommentsAsync_CheminNullOuVide_NeFaitRien(string? path)
    {
        var sut = CreateSut();
        var ex = await Record.ExceptionAsync(() => sut.SaveCommentsAsync(path!));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SaveCommentsAsync_CheminInexistant_NeFaitRien()
    {
        var sut = CreateSut();
        var ex = await Record.ExceptionAsync(() => sut.SaveCommentsAsync(@"C:\chemin\inexistant\xyz"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SaveCommentsAsync_CurrentPathDifferent_NAEcritAucunFichier()
    {
        var sut = CreateSut();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            // _currentGradingPath est null → le guard bloque l'écriture
            await sut.SaveCommentsAsync(dir);
            Assert.False(File.Exists(Path.Combine(dir, "reusable_comments.json")));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // -----------------------------------------------------------------------
    // LoadCommentsAsync
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadCommentsAsync_CheminNullOuVide_NeFaitRien(string? path)
    {
        var sut = CreateSut();
        var ex = await Record.ExceptionAsync(() => sut.LoadCommentsAsync(path!));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LoadCommentsAsync_CheminInexistant_NeFaitRien()
    {
        var sut = CreateSut();
        var ex = await Record.ExceptionAsync(() => sut.LoadCommentsAsync(@"C:\chemin\inexistant\xyz"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LoadCommentsAsync_FichierAbsent_VideLeCacheEtPermetSauvegarde()
    {
        var sut = CreateSut();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            sut.AddCommentForCriterion("Critère 1", Entry("avant"));

            await sut.LoadCommentsAsync(dir);

            Assert.Empty(sut.GetCommentsForCriterion("Critère 1"));
            // currentGradingPath mis à jour → la sauvegarde doit fonctionner
            await sut.SaveCommentsAsync(dir);
            Assert.True(File.Exists(Path.Combine(dir, "reusable_comments.json")));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadCommentsAsync_FichierValide_RemplitLeCache()
    {
        var sut = CreateSut();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var json = """{"Critère 1":[{"text":"commentaire","severity":"Majeur"}]}""";
            await File.WriteAllTextAsync(Path.Combine(dir, "reusable_comments.json"), json);

            await sut.LoadCommentsAsync(dir);

            var result = sut.GetCommentsForCriterion("Critère 1");
            Assert.Single(result);
            Assert.Equal("commentaire", result[0].Text);
            Assert.Equal(CommentSeverity.Majeur, result[0].Severity);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadCommentsAsync_FichierJsonNull_VideLeCacheEtPermetSauvegarde()
    {
        // Régression : loaded == null ne doit pas bloquer les sauvegardes ultérieures
        var sut = CreateSut();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "reusable_comments.json"), "null");
            sut.AddCommentForCriterion("Critère 1", Entry("avant"));

            await sut.LoadCommentsAsync(dir);

            Assert.Empty(sut.GetCommentsForCriterion("Critère 1"));
            // currentGradingPath doit être mis à jour même si loaded est null
            await sut.SaveCommentsAsync(dir);
            Assert.True(File.Exists(Path.Combine(dir, "reusable_comments.json")));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task LoadCommentsAsync_FichierMalforme_ConserveCacheExistant()
    {
        var sut = CreateSut();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "reusable_comments.json"), "{ json invalide }}");
            sut.AddCommentForCriterion("Critère 1", Entry("conservé"));

            await sut.LoadCommentsAsync(dir);

            Assert.Single(sut.GetCommentsForCriterion("Critère 1"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    // -----------------------------------------------------------------------
    // Round-trip Save → Load
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveLoad_AllerRetour_ProduiseMemeContenu()
    {
        var sut = CreateSut();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            await sut.LoadCommentsAsync(dir); // initialise _currentGradingPath
            sut.AddCommentForCriterion("Critère 1", Entry("texte A", CommentSeverity.Mineur));
            sut.AddCommentForCriterion("Critère 2", Entry("texte B", CommentSeverity.Critique));
            await sut.SaveCommentsAsync(dir);

            var sut2 = CreateSut();
            await sut2.LoadCommentsAsync(dir);

            var c1 = sut2.GetCommentsForCriterion("Critère 1");
            var c2 = sut2.GetCommentsForCriterion("Critère 2");
            Assert.Single(c1);
            Assert.Equal("texte A", c1[0].Text);
            Assert.Equal(CommentSeverity.Mineur, c1[0].Severity);
            Assert.Single(c2);
            Assert.Equal("texte B", c2[0].Text);
            Assert.Equal(CommentSeverity.Critique, c2[0].Severity);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
