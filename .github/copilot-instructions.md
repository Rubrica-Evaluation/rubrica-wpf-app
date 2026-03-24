# Instructions — GradingTool

Application WPF de correction d'évaluations (TPs) pour enseignants.
Stack : .NET 8, WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, xUnit + NSubstitute.

---

## Architecture

```
GradingTool/
  Models/          → POCOs + modèles JSON (pas de logique)
  Services/        → logique métier + I/O fichiers (interface + implémentation)
  ViewModels/      → MVVM via CommunityToolkit.Mvvm (ObservableObject, [ObservableProperty], [RelayCommand])
  Views/           → XAML + code-behind minimal (event handlers uniquement)
  Controls/        → contrôles réutilisables
  Converters/      → IValueConverter WPF
  Helpers/         → utilitaires statiques transversaux
  Styles/          → ressources XAML globales

GradingTool.Tests/
  Services/        → tests xUnit sur la logique des services
```

**Organisation hiérarchique des données :**
`sessions root / session / cours / travail / {rubric/, roster/, submissions/, grading/, pdf_docs/}`

---

## Conventions de code

### Général
- Fichiers en C# 12, nullable enable, implicit usings enable
- Un fichier = une classe publique
- Namespaces calqués sur le dossier : `GradingTool.Services`, `GradingTool.Models`, etc.
- Langue du code : anglais (noms de classes, méthodes, propriétés)
- Langue des commentaires et messages UI : français

### Services
- Toujours créer l'interface `IMonService` en même temps que `MonService`
- Les services sont enregistrés en `Singleton` dans `App.xaml.cs`
- Les méthodes async retournent `Task` ou `Task<T>`, suffixe `Async`
- Les erreurs d'I/O sont absorbées silencieusement (retour `false`/`null`) — pas d'exceptions vers le ViewModel
- Sérialisation JSON : `JsonNamingPolicy.CamelCase` + `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` + `WriteIndented = true`

### ViewModels
- Héritent de `ObservableObject` (`partial class` obligatoire)
- Propriétés bindées : champ `_camelCase` annoté `[ObservableProperty]`
- Commandes : méthode privée annotée `[RelayCommand]`, ou `[RelayCommand(CanExecute = nameof(...))]`
- Injection via constructeur, toutes les dépendances en interfaces
- Pas de `MessageBox` ni d'accès direct aux fichiers dans un ViewModel — déléguer à `IDialogService` / service approprié
- Les propriétés calculées (non observables) sont des getters purs sans `[ObservableProperty]`

### Models
- POCOs sérialisables : `[JsonPropertyName("camelCase")]` sur chaque propriété
- `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` pour les champs optionnels
- Les modèles avec propriétés observables héritent de `ObservableObject`
- `ObservableCollection<T>` uniquement dans les modèles liés à la vue

### XAML / Views
- Code-behind limité aux event handlers qui ne peuvent pas être des commandes
- Pas de logique métier dans le code-behind
- Styles globaux dans `Styles/`

---

## Injection de dépendances

Toutes les dépendances sont enregistrées dans `App.xaml.cs → ConfigureServices()`.
Lorsqu'un nouveau service est créé, l'ajouter dans `ConfigureServices()` en `Singleton`.
Le `NavigationService` utilise une `Func<Type, object>` factory pour résoudre les ViewModels.

---

## Tests unitaires

**Projet :** `GradingTool.Tests` (net8.0-windows, UseWPF=true, xUnit + NSubstitute)

**Règles :**
- Un fichier de test par classe : `MonServiceTests.cs` dans le sous-dossier miroir (`Services/`, etc.)
- Convention de nommage : `NomMéthode_Condition_RésultatAttendu`
- Helpers privés dans la classe de test pour construire les objets de test
- Mocker les dépendances avec NSubstitute : `Substitute.For<IMonService>()`
- Ne pas tester l'I/O réel — tester uniquement la logique pure ou avec mocks
- Couvrir : cas nominal, cas limites, cas d'erreur

**Après chaque modification :**
1. `dotnet build GradingTool/GradingTool.csproj`
2. `dotnet test GradingTool.Tests`

Les deux doivent passer avant de considérer la tâche terminée.

---

## Patterns récurrents

### Ajouter un nouveau service
1. Créer `Models/MonModel.cs` si nécessaire
2. Créer `Services/IMonService.cs` (interface)
3. Créer `Services/MonService.cs` (implémentation)
4. Enregistrer dans `App.xaml.cs` : `services.AddSingleton<IMonService, MonService>();`
5. Créer `GradingTool.Tests/Services/MonServiceTests.cs`

### Ajouter une nouvelle vue
1. Créer `ViewModels/MonViewModel.cs` (partial, hérite ObservableObject)
2. Enregistrer dans `App.xaml.cs`
3. Créer `Views/MonView.xaml` + `Views/MonView.xaml.cs`
4. Lier via `DataTemplate` dans `MainWindow.xaml` si navigation nécessaire

### Données persistées
- Format : JSON, chemin via `ISessionsRootService.GetSessionsRootPath()`
- Toujours `Encoding.UTF8` pour lire/écrire
- Noms de fichiers : sanitiser via `SanitizeFileNamePart()` (GridService)

---

## Clean Code (Robert C. Martin)

### Nommage
- Les noms doivent révéler l'intention : `LoadGridFiles` et non `GetFiles`, `isTeam` et non `flag`
- Pas d'abréviations ambiguës : `student` et non `s`, `criterion` et non `c`
- Les booléens se lisent comme une affirmation : `isTeam`, `hasGroups`, `gridExists`
- Les méthodes sont des verbes : `GenerateGrid`, `SaveAsync`, `ValidateRubricFormat`
- Éviter les préfixes redondants : pas de `GetStudentName()` dans une classe `Student`

### Fonctions
- Une fonction = une seule responsabilité (SRP)
- Taille cible : ≤ 20 lignes; au-delà, extraire des méthodes privées nommées
- Maximum 3 paramètres; au-delà, regrouper dans un objet ou un record
- Pas d'effets de bord cachés — une méthode qui dit "get" ne doit rien modifier
- Pas de flags booléens en paramètre pour changer le comportement — faire deux méthodes

### Commentaires
- Le code doit se lire sans commentaires — un commentaire signale souvent un problème de nommage
- Commenter le **pourquoi**, jamais le **quoi** : le quoi se lit dans le code
- Pas de commentaires obsolètes ou de code commenté — utiliser Git pour l'historique
- Les `<summary>` XML sur les interfaces publiques sont bienvenus; pas dans les implémentations privées

### Structure
- Loi de Déméter : un objet ne parle qu'à ses dépendances directes, pas aux dépendances de ses dépendances
- Pas de nombres/chaînes magiques : extraire en constante nommée
- Les conditions complexes sont extraites dans des méthodes ou variables booléennes nommées
- Guard clauses en début de méthode plutôt qu'imbrication profonde (`if (x == null) return;`)
- Niveau d'abstraction cohérent à l'intérieur d'une méthode — ne pas mélanger logique haut niveau et détails I/O

### Classes
- SRP : une classe a une seule raison de changer
- Petite surface publique — `private` par défaut, `public` seulement si nécessaire
- Pas de classes "fourre-tout" (Utils, Helper, Manager générique)

---

## Interdictions
- Pas de logique dans le code-behind XAML
- Pas d'accès fichier dans les ViewModels
- Pas de `new MonService()` dans un ViewModel (toujours injecter)
- Pas d'opérations bloquantes sur le thread UI
