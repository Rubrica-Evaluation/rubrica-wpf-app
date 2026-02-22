namespace GradingTool.Services;

public class CommentService : ICommentService
{
    private readonly Dictionary<string, List<string>> _commentsByCriteria;

    public CommentService()
    {
        // Initialiser avec des commentaires mockés
        _commentsByCriteria = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "Structure des tables",
                new List<string>
                {
                    "Code clair et facile à comprendre",
                    "Code relativement clair",
                    "Code peu clair, manque de structure",
                    "Code difficile à comprendre, très désorganisé"
                }
            },
            {
                "Contraintes",
                new List<string>
                {
                    "Solution complètement correcte",
                    "Solution correcte avec quelques erreurs mineures",
                    "Solution partiellement correcte",
                    "Solution incorrecte ou incomplète"
                }
            },
            {
                "Relations (PK / FK)",
                new List<string>
                {
                    "Toutes les exigences sont satisfaites",
                    "La plupart des exigences sont satisfaites",
                    "Plusieurs exigences sont manquantes",
                    "Beaucoup d'exigences sont manquantes"
                }
            },
            {
                "Fichiers SQL remis",
                new List<string>
                {
                    "Documentation complète et claire",
                    "Documentation adéquate",
                    "Documentation insuffisante",
                    "Pas de documentation ou très minimal"
                }
            },
            {
                "Efficacité",
                new List<string>
                {
                    "Solution très efficace et optimisée",
                    "Solution efficace",
                    "Solution fonctionnelle mais à améliorer",
                    "Solution inefficace ou très lente"
                }
            }
        };
    }

    public List<string> GetCommentsForCriterion(string criterionLabel)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel))
        {
            return new List<string>();
        }

        if (_commentsByCriteria.TryGetValue(criterionLabel, out var comments))
        {
            return new List<string>(comments); // Retourner une copie
        }

        // Retourner une liste vide si le critère n'existe pas
        return new List<string>();
    }

    public void AddCommentForCriterion(string criterionLabel, string comment)
    {
        if (string.IsNullOrWhiteSpace(criterionLabel) || string.IsNullOrWhiteSpace(comment))
        {
            return;
        }

        if (!_commentsByCriteria.ContainsKey(criterionLabel))
        {
            _commentsByCriteria[criterionLabel] = new List<string>();
        }

        // Ajouter seulement si le commentaire n'existe pas déjà
        if (!_commentsByCriteria[criterionLabel].Contains(comment))
        {
            _commentsByCriteria[criterionLabel].Add(comment);
        }
    }
}
