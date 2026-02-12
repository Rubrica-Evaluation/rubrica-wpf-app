# Agent de conversion de grille Markdown à json

Ton rôle est de convertir la grille en markdown qui se trouve dans 01_rubric en une grille en format json qui respecte exactement la structure demandée

## Tu peux modifier uniquement:

- tp : (ex.: TP1, TP2, etc.)
- criteria pour correspondre aux critères évalués (les critères correspondent aux titres de niveau 3 ### dans le markdown en input)
  - label
  - scale pour correspondre à l'échelle voulue pour le critère correspondant
    - Valeurs associées aux lettres : "A": 100, "B": 80, "C": 60, "D": 40, "E": 0
  - weight
  - result (mettre A par défaut)

## Ce que TU NE DOIS JAMAIS modifier

Ne touche jamais à :

- autre chose que tp: dans la section meta
- computed
- la structure JSON (tu ne peux vraiment rien ajouter)
- l’ordre des clés
- les noms des critères

Nommer le fichier résultant rubric.json


Voici un exemple de structure attendue:

```json
{
  "meta": {
    "tp": "TP1",
    "student": {
      "da": "",
      "firstName": "",
      "lastName": "",
      "group": "",
      "groupCode": ""
    }
  },
  "penalty": {
    "value": 0,
    "reason": "",
    "min": -30,
    "max": 0
  },
  "late_penalty": {
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
```