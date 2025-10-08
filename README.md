# Chaussée Neuve - Application de Dimensionnement de Chaussées

Application WPF .NET 8 pour le calcul et dimensionnement de structures de chaussées, intégrant les méthodes TRMM et PyMastic pour l'analyse des contraintes et déformations.

## 🚀 Fonctionnalités

- **Interface utilisateur moderne** : Application WPF avec workflow intuitif en 3 étapes
- **Calculs de dimensionnement** : Intégration des algorithmes TRMM et PyMastic validés
- **Modes d'utilisation** : Mode Expert et Mode Automatique pour différents niveaux d'utilisateurs
- **Gestion de projets** : Création, sauvegarde et gestion de projets de dimensionnement

## 🏗️ Architecture

### Interface Utilisateur (WPF .NET 8)
- **Fenêtre 1** : Sélection du mode (Expert/Automatique)
- **Fenêtre 2** : Informations du projet (titre, auteur, emplacement, description)
- **Fenêtre 3** : Interface principale avec barre latérale expandable et calculs

### Moteur de Calcul
- **PavementCalculationEngine** : Moteur C++ haute performance
- **PyMastic Python Bridge** : Interface validée pour calculs PyMastic (précision 0.01%)
- **TRMM Solver** : Implémentation des calculs TRMM
- **API C** : Interface pour intégration .NET

### Domaine Métier
- **Modèles de données** : Structures de chaussée, charges, paramètres matériaux
- **Validation** : Contrôles métier et cohérence des données

## 📁 Structure du Projet

```
UI_ChausseeNeuve/
├── UI_ChausseeNeuve/           # Interface utilisateur WPF
├── ChausseeNeuve.Domain/       # Modèles métier et logique domaine
├── PavementCalculationEngine/  # Moteur de calcul C++
├── TestNativeCalculation/      # Tests du moteur natif
├── UI_ChausseeNeuve.Tests/     # Tests de l'interface
├── docs/                       # Documentation technique
└── archive/                    # Historique de développement
```

## 🛠️ Installation et Configuration

### Prérequis
- Visual Studio 2022 ou plus récent
- .NET 8 SDK
- CMake 3.20+
- Python 3.8+ (pour PyMastic bridge)
- vcpkg (gestion des dépendances C++)

### Compilation

1. **Cloner le repository**
   ```bash
   git clone https://github.com/romeoolouro/UI_ChausseeNeuve.git
   cd UI_ChausseeNeuve
   ```

2. **Compiler le moteur de calcul**
   ```bash
   cd PavementCalculationEngine
   build_dll_clean.bat
   ```

3. **Compiler l'application**
   ```bash
   dotnet build UI_ChausseeNeuve.sln
   ```

## 🚦 Utilisation

### Lancement rapide
```bash
dotnet run --project UI_ChausseeNeuve
```

### Script de lancement avec logs
```powershell
.\launch_wpf_with_logs.ps1
```

## 📊 Validation et Précision

- **PyMastic Python Bridge** : Validé contre Tableau I.1 avec erreur de 0.01%
- **Calculs de contraintes** : Résultats conformes aux références techniques
- **Tests automatisés** : Suite de tests pour validation continue

## 📖 Documentation

- [Documentation Application](docs/DOCUMENTATION_APPLICATION.md) - Interface utilisateur et workflow
- [Documentation Domaine](docs/DOCUMENTATION_DOMAINE.md) - Modèles métier et logique
- [Réalisations Techniques](docs/PAVEMENT_CALCULATION_TECHNICAL_ACHIEVEMENTS.md) - Détails techniques
- [Plan de Debug C++](docs/PYMASTIC_CPP_DEBUG_PLAN.md) - Optimisations futures

## 🔧 Développement

### Tests
```bash
dotnet test
```

### Debug du moteur C++
Voir `PavementCalculationEngine/debug-scripts/` pour les outils de développement.

## 📈 Statut du Projet

✅ **Interface WPF** : Complète et fonctionnelle  
✅ **PyMastic Bridge** : Production-ready (précision validée)  
✅ **TRMM Solver** : Implémenté et testé  
✅ **Integration .NET** : API C complète  
🔄 **Optimisation C++** : Planifiée (voir plan de debug)

## 🤝 Contribution

Pour contribuer au projet, consultez l'historique de développement dans `archive/development-history/`.

## 📄 Licence

[À définir selon les besoins du projet]
