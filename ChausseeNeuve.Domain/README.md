# ChausseeNeuve.Domain

Ce projet contient les modèles métier et la logique du domaine pour l'application de dimensionnement de chaussées.

## Composants

### Modèles Principaux
- `PavementStructure.cs` - Structure complète de la chaussée
- `Layer.cs` - Modèle de couche de chaussée
- `Project.cs` - Modèle de projet de dimensionnement
- `ChargeReference.cs` - Références de charges

### Modèles Techniques
- `Models/ValeurAdmissibleCouche.cs` - Valeurs admissibles par couche
- `Models/GNTParameters.cs` - Paramètres GNT (Grave Non Traitée)
- `ValeurAdmissibleCoucheDto.cs` - DTO pour transfert de données

### Énumérations
- `Enums.cs` - Énumérations métier (types de matériaux, modes de calcul, etc.)

## Usage

Ce projet est référencé par l'interface utilisateur WPF et fournit les modèles de données utilisés dans toute l'application.