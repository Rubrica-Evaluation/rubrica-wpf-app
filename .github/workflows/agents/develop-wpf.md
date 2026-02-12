# .github/workflows/agents/wpf-grading-backlog.md
# Agent Copilot — Grading Tool WPF (développement itératif par user stories)

## Rôle
Tu es un agent GitHub Copilot senior chargé de développer progressivement une application **WPF .NET (Windows)** pour gérer des sessions/cours/travaux et corriger des grilles d’évaluation au format JSON.

Tu travailles **story par story**. Tu ne passes à la story suivante **que quand les critères d’acceptation sont satisfaits**.

---

## Contrainte de méthode (obligatoire)

### À chaque story, tu dois :
1) **Créer ou modifier uniquement le minimum de fichiers** nécessaires.
2) Ajouter/mettre à jour un fichier `docs/progress.md` avec :
   - la story en cours
   - ce qui a été fait
   - comment tester
3) Fournir un **plan de test manuel** (3–6 étapes) réalisable en 2 minutes.
4) Maintenir l’app **compilable** en tout temps.
5) Éviter toute “grosse refonte” tant que ce n’est pas nécessaire.

### Architecture (obligatoire)
- WPF + MVVM strict.
- Zéro logique métier dans le code-behind (sauf wiring minimal).
- Services séparés pour I/O et calcul.
- Sérialisation JSON via `System.Text.Json`.
- Le format JSON des grilles est **immuable** (tu le respectes).
- Tri de la liste des grilles par **nom de fichier** (locale fr + tri numérique si possible).

### Stack recommandée
- .NET 8
- `CommunityToolkit.Mvvm` (commands + observable)
- `Microsoft.Extensions.DependencyInjection` (DI simple)

---

## Contexte des données (structure projet actuelle en python)

Dossier racine:
sessions/
  Hiver 2026/
    BD1/
      TP2/
        grading/
        pdf_docs/
        roster/
        rubric/
        submissions/
tools/ (scripts existants)

Exigence: dans l’app, l’utilisateur sélectionne un **dossier racine `sessions/`**.

---

## Format des grilles JSON (à respecter)

Exemple (provenant de BD1):
{
  "meta": {
    "tp": "TP1",
    "student": {
      "da": "",
      "firstName": "",
      "lastName": "",
      "group": "",
      "groupCode": "",
      "team": 0
    }
  },
  "penalty": {
    "value": 0,
    "reason": "",
    "min": -30,
    "max": 0
  },
  "late_penalty": {
    "number_days_late": 0,
    "value": 0,
    "reason": "",
    "min": -30,
    "max": 0
  },
  "language_penalty": {
    "value": 0,
    "number_errors": 0,
    "min": -20,
    "max": 0
  },
  "criteria": {
    "structure": {
      "label": "Structure des tables",
      "scale": { "A": 100, "B": 80, "C": 60, "D": 40, "E": 0 },
      "weight": 60,
      "result": "",
      "feedback": "",
      "points": null
    },
    "types": {
      "label": "Types de données",
      "scale": { "A": 100, "C": 60, "E": 0 },
      "weight": 40,
      "result": "",
      "feedback": "",
      "points": null
    }
  },
  "computed": {
    "total": null
  }
}

Règles:
- result ∈ {A,B,C,D,E,""}
- points par critère = weight * (scale[result]/100)
- total = somme(points) + penalty.value (clamp min/max)
- total ne descend pas sous 0
- Ne pas écraser les champs inconnus; préserver `penalty.reason` si présent.

---

## UX cible (aperçu)
Fenêtre principale:
- Sélection dossier `sessions/`
- Navigation (à gauche): session → cours → travail
- Dans un travail: charger rubric.json + roster CSV (à droite)
- Générer grilles JSON (options équipes)
- Lister grilles d’un groupe (tri par filename)
- Ouvrir éditeur WPF pour corriger
- Export summary.csv (DA,total) pour groupe
- Export PDF + lien “Ouvrir dossier PDF”

---

# BACKLOG — User Stories (ordre obligatoire)

## Epic 0 — Base
### US0.1 — Fenêtre vide
En tant que prof, je veux lancer l’app WPF et voir une fenêtre principale.
Acceptation:
- Build + run
- Fenêtre “Accueil” visible
- couleurs bien contrastées (light mode)
- texte assez gros
- boutons légèrement arrondis

### US0.2 — Sélection dossier racine sessions
Je veux créer/sélectionner un dossier racine `Eval-App/sessions/` et le mémoriser.
Acceptation:
- Si le dossier racine n'est pas détecté, bouton pour le créer:
  - Choisir dossier (folder picker)
  - Chemin stocké (App.config)
- Affiché dans UI à gauche

---

## Epic 1 — Navigation
### US1.1 — Lister sessions
Lister dossiers sous `sessions/` (ex. “Hiver 2026”), tri alpha.
Acceptation:
- bouton nouvelle session
- possible de nommer la session
- crée le dossier correspondant
- Liste = dossiers réels
- Tri OK

### US1.2 — Lister cours
Cliquer session → liste cours (ex. “BD1”), tri alpha.
Acceptation:
- Navigation hiérarchique OK

### US1.3 — Lister évaluations
Cliquer cours → liste évaluations (ex. “TP1/TP2”), tri alpha.
Acceptation:
- Possible de créer / modifier / supprimer une évaluation
- Sélection évaluation affiche panneau “Détails” (chemins des sous-dossiers)
- à la création d'une nouvelle évaluation, créer automatiquement rubric, roster, submissions, grading, pdf_docs
- 

---

## Epic 2 — Charger assets TP
### US2.1 — Vérifier/Créer structure TP
Sur sélection TP, vérifier présence: rubric/ roster/ submissions/ grading/ pdf_docs/
Acceptation:
- Messagebox d'avertissement demandant si veut créer la structure manquante.

### US2.2 — Charger rubric.json
Charger un fichier rubric JSON (template) depuis `rubric/`.
Acceptation:
- Doit afficher si la rubrique est trouvée ou pas
- Bouton pour charger une nouvelle rubrique
- Doit valider si la rubrique correspond au format attendu (ne pas permettre de charger sinon)
- Bouton pour télécharger template avec format attendu
- Erreurs JSON gérées (message)

### US2.3 — Charger liste d'étudiants CSV et détecter groupes
Lire `roster/*.csv` et détecter groupCode via `gr0000X` dans le nom.
Acceptation:
- Liste groupes: “Gr. N (gr0000N)”
- Si aucun match: message clair
- Un bouton permet de charger un nouveau groupe
- Bouton pour télécharger template avec format attendu

---

## Epic 3 — Génération grilles
### US3.1 — Générer 1 grille (test)
Générer 1 JSON de grille à partir du rubric et d’un étudiant.
Acceptation:
- JSON écrit dans `grading/gr0000X/`
- meta remplie

### US3.2 — Générer tout le groupe
Générer toutes les grilles du groupe sélectionné.
Acceptation:
- N fichiers attendus
- Fichiers bien nommés et encodés (reprendre la logique du test pour une grille)
- Si le répertoire grading n'est pas vide, Messagebox: Option “écraser” ou “skip existants”
- Toast de success / échec

### US3.3 — Option équipes (4e colonne)
Si 4e colonne du roster = team id (numérique ou texte), proposer:
- “Une grille par étudiant”
- “Une grille par équipe”
Acceptation:
- Regroupement correct même si team id = 2, 2.0, "2"
- Nom de fichier stable (liste des membres)
- Si équipe, préfixer les fichiers T1, T2, T3, etc.

---

## Epic 4 — Éditeur
### US4.1 — Ouvrir grille JSON dans l’éditeur
Sélectionner un JSON → ouvrir éditeur.
Acceptation:
- Affiche meta + critères + pénalité
- la section de pénalités est une zone repliable
- chaque critère est une section repliable
- modifier les résultats (A–E) à l'aide de boutons radios
- modifier les pénalités
- voir les points et le total se recalculer automatiquement
- enregistrer la grille (manuellement + autosave)

### US4.2 — Modifier result et calcul live
Changer A–E → points et total se recalculent instantanément.
Acceptation:
- Formules exactes + clamp penalty + total min 0
- Message de dernière sauvegarde

### US4.3 — Modifier feedback
Éditer feedback par critère.
Acceptation:
- Sauvegarde dans JSON avec bouton enregistrer.

### US4.4 — Pénalité reason + warning
Ajouter Raison -> `penalty.reason` et warning si penalty < 0 et reason vide.
Acceptation:
- warning visible
- reason sauvegardée

### US4.5 — Save + autosave en quittant
Bouton Save et autosave lors de sortie/changement de sélection (configurable).
Acceptation:
- Dirty tracking
- Autosave avec message ou confirmation

---

## Epic 5 — Opérations groupe
### US5.1 — Liste grilles triée par nom de fichier
Liste grilles du groupe triée par filename (tri numérique).
Acceptation:
- Fichier_2 avant Fichier_10

### US5.2 — Export summary.csv (DA,total)
Exporter summary.csv pour groupe sélectionné.
Acceptation:
- CSV: header da,total
- tri par DA
- totaux calculés et arrondis

---

## Epic 6 — PDF
### US6.1 — Export PDF 1 grille
Exporter un PDF pour la grille affichée.
Acceptation:
- PDF dans `pdf_docs/gr0000X/`
- contenu lisible

### US6.2 — Export PDF groupe + ouvrir dossier
Exporter tous les PDF du groupe + bouton “Ouvrir dossier PDF”.
Acceptation:
- Batch OK
- Ouvre l’explorateur Windows sur le dossier
- bouton d'exportation en .zip nommé 'travaux.zip'

---

# Mode opératoire (comment tu avances)

## Début
Commence immédiatement par **US0.1**.
- Propose la structure de solution minimale (1 projet WPF suffit au début).
- Implémente la story avec le moins de code possible.
- Mets à jour docs/progress.md.

## À chaque fin de story
- Confirme story complétée
- Donne étapes de test
- Annonce la story suivante que tu vas faire

Ne saute jamais de story.
Ne fais pas d’optimisation prématurée.


### Idées...
- Lorsqu'on produit le sommaire ou le pdf, vérifier si des champs de rétroaction pour les critères sont vides ("Les étudiants suivants ont des champs de rétroaction non remplis, voulez-vous tout de même poursuivre?"). Message d'avertissement si c'est le cas.
- Ajouter des tris et filtres pour la liste des étudiants dans l'éditeur
- Bouton "Appliquer à l'équipe" sur les champs
- Vidéo tutoriel créée à l'aide de notebooklm
- Conversion d'une grille à échelle descriptive en json à l'aide d'intégration avec IA (option payante)
- Dans l'éditeur, rétroaction sous forme de commentaire qu'on pourrait réutiliser (option payante?).
- Créateur de rubrique (critères et pénalités)
- Possibilité d'ajouter un step dans une grille précise (qualitatif, label, points, order qui permet de savoir entre quels deux niveaux insérer ce step).
