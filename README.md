# GradingTool - Outil d'Évaluation

Application WPF développée en .NET 8 pour faciliter la gestion et l'évaluation des travaux étudiants. Permet de créer des sessions, cours, travaux, gérer les rubriques d'évaluation, les listes d'étudiants, générer des grilles de notation et exporter des PDFs.

## Fonctionnalités

- **Gestion des Sessions** : Organisez vos évaluations par sessions (ex: 2025-Winter, 2025-Summer)
- **Gestion des Cours** : Créez et gérez plusieurs cours par session
- **Gestion des Travaux** : Définissez les travaux à évaluer pour chaque cours
- **Rubriques d'Évaluation** : Importez et gérez les critères d'évaluation (fichiers JSON)
- **Listes d'Étudiants** : Importez les rosters d'étudiants (fichiers CSV)
- **Génération de Grilles** : Créez automatiquement des grilles de notation pour les étudiants individuels ou équipes
- **Éditeur de Grilles** : Interface intuitive pour noter les travaux
- **Export PDF** : Générez des rapports PDF des évaluations
- **Sauvegarde Automatique** : Les modifications sont sauvegardées automatiquement

## Prérequis

- **.NET 8.0** ou supérieur
- **Windows** (application WPF)

## Installation et Démarrage

1. **Cloner le dépôt** :
   ```bash
   git clone <url-du-depot>
   cd gestion-evaluation
   ```

2. **Restaurer les dépendances** :
   ```bash
   dotnet restore
   ```

3. **Construire l'application** :
   ```bash
   dotnet build
   ```

4. **Lancer l'application** :
   ```bash
   dotnet run --project GradingTool/GradingTool.csproj
   ```

Ou utilisez les tâches VS Code :
- `Ctrl+Shift+P` > `Tasks: Run Task` > `watch` pour lancer en mode développement

## Structure du Projet

```
gestion-evaluation/
├── GradingTool/                 # Application WPF principale
│   ├── App.xaml                 # Point d'entrée de l'application
│   ├── MainWindow.xaml          # Fenêtre principale
│   ├── ViewModels/              # Modèles de vue (MVVM)
│   │   ├── MainViewModel.cs
│   │   ├── WorkspaceViewModel.cs
│   │   └── GridEditorViewModel.cs
│   ├── Views/                   # Vues XAML
│   ├── Models/                  # Modèles de données
│   ├── Services/                # Services métier
│   ├── Converters/              # Convertisseurs XAML
│   ├── Controls/                # Contrôles personnalisés
│   └── Styles/                  # Styles et ressources
├── evaluation.sln               # Solution Visual Studio
├── .gitignore                   # Fichiers à ignorer
└── README.md                    # Ce fichier
```

## Utilisation

1. **Configuration Initiale** :
   - Lancez l'application
   - Sélectionnez un dossier racine pour stocker vos sessions d'évaluation

2. **Créer une Session** :
   - Cliquez sur "Nouvelle Session"
   - Entrez un nom (ex: "2025-Winter")

3. **Ajouter un Cours** :
   - Sélectionnez une session
   - Cliquez sur "Nouveau Cours"
   - Entrez le code du cours (ex: "42006_AFX")

4. **Définir un Travail** :
   - Sélectionnez un cours
   - Cliquez sur "Nouveau Travail"
   - Configurez les paramètres du travail

5. **Importer les Données** :
   - Ajoutez une rubrique d'évaluation (fichier JSON)
   - Importez la liste des étudiants (fichier CSV)

6. **Générer les Grilles** :
   - Sélectionnez un groupe d'étudiants
   - Choisissez le mode de génération (individuel ou équipe)
   - Générez les grilles de notation

7. **Évaluer les Travaux** :
   - Ouvrez l'éditeur de grilles
   - Notez selon les critères définis
   - Sauvegardez automatiquement

8. **Exporter les Résultats** :
   - Générez des rapports PDF
   - Exportez les données en CSV

## Structure des Données

L'application organise les données dans une structure hiérarchique :

```
Dossier Racine/
├── Session1/
│   ├── Cours1/
│   │   ├── Travail1/
│   │   │   ├── rubric/          # Fichier rubric.json
│   │   │   ├── roster/          # Fichiers CSV étudiants
│   │   │   ├── submissions/     # Soumissions étudiants
│   │   │   ├── grading/         # Grilles JSON générées
│   │   │   └── pdf_docs/        # PDFs exportés
│   │   └── Travail2/
│   └── Cours2/
└── Session2/
```

## Technologies Utilisées

- **.NET 8.0** - Framework de développement
- **WPF** - Framework d'interface utilisateur
- **CommunityToolkit.Mvvm** - Patterns MVVM
- **PdfSharp** - Génération de PDFs
- **CsvHelper** - Manipulation de fichiers CSV
- **Newtonsoft.Json** - Gestion du JSON

## Développement

### Tâches Disponibles

- `build` : Construit l'application
- `publish` : Publie l'application pour déploiement
- `watch` : Lance l'application en mode surveillance (recompilation automatique)

### Contribution

1. Fork le projet
2. Créez une branche pour votre fonctionnalité
3. Commitez vos changements
4. Poussez vers la branche
5. Ouvrez une Pull Request

## Support

Pour des questions ou problèmes, ouvrez une issue sur le dépôt GitHub.