# TestNativeCalculation

Projet de test console pour valider les fonctionnalités du moteur de calcul natif C++.

## Objectif

Ce projet permet de tester directement les fonctions du moteur `PavementCalculationEngine` sans passer par l'interface WPF, facilitant ainsi :
- Le debug des algorithmes de calcul
- La validation des résultats
- Les tests de performance
- L'intégration des nouvelles fonctionnalités

## Usage

```bash
dotnet run --project TestNativeCalculation
```

## Dépendances

- Référence vers `ChausseeNeuve.Domain` pour les modèles
- Intégration avec `PavementCalculationEngine.dll`