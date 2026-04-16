# Rubrica — Outil de gestion de grilles d'évaluation pour enseignants

Application Windows gratuite pour organiser et corriger les évaluations. Conçue pour les enseignants du collégial, elle permet de gérer les sessions, cours et évaluations, de créer des rubriques de correction, de générer des grilles par étudiant ou par équipe, et d'exporter les résultats en PDF pour le partage aux étudiants.

## Principe directeur

Proposer une solution qui permet de gagner du temps à différents stades de l'évaluation afin de diminuer la fatigue cognitive. De plus, l'enseignant demeure propriétaire de ses données en tout temps.

## Fonctionnalités

- **Gestion hiérarchique** : sessions → cours → évaluations
- **Rubriques de correction** : conception via l'éditeur intégré, format JSON
- **Listes d'étudiants** : import CSV, support des groupes et des équipes
- **Génération de grilles** : individuelle ou par équipe, avec détection des doublons
- **Éditeur de grilles** : interface de correction avec sauvegarde automatique
- **Export PDF** : génération de rapports de correction par étudiant
- **Multilingue** : français et anglais

## Téléchargement

L'application est disponible sur la page [Releases](../../releases).

**Prérequis :** Windows 10/11

## Utilisation rapide

1. Lancez `Rubrica.exe`
2. Sélectionnez ou créez un dossier racine pour vos données
3. Créez une session (ex : `Hiver 2026`), un cours (ex : `Philo`), une évaluation (ex : `Travail 1`)
4. Concevez votre rubrique via le concepteur intégré
5. Importez votre liste d'étudiants (CSV)
6. Générez les grilles et commencez la correction

## Structure des données

Les données sont stockées localement dans des fichiers JSON/CSV, organisés ainsi :

```
DossierRacine/
└── Session/
    └── Cours/
        └── Évaluation/
            ├── rubric/       ← rubric.json
            ├── roster/       ← liste étudiants CSV
            ├── submissions/  ← soumissions
            ├── grading/      ← grilles de correction JSON
            └── pdf_docs/     ← PDFs exportés
```

## Stack technique

- .NET 10 / WPF
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- PDFsharp + iText7
- xUnit + NSubstitute (tests)

## Développement

```bash
git clone <url-du-depot>
cd gestion-evaluation
dotnet build GradingTool/GradingTool.csproj
dotnet test GradingTool.Tests
```

Tâches VS Code disponibles : `build`, `publish`, `watch`.

## Contribution et retours

Les issues, suggestions et pull requests sont bienvenus. Le projet suit une architecture MVVM avec injection de dépendances.

## Licence

MIT — voir [LICENSE](LICENSE).
