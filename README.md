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
- **PavementCalculationEngine** : Moteur C++ avec intégration Python
- **PyMastic Python Bridge** : Interface subprocess vers [PyMastic](https://github.com/Mostafa-Nakhaei/PyMastic) (précision validée 0.01%)
  - **Actuellement en production** : Seul algorithme validé et déployé
- **Développements futurs** :
  - TRMM Solver C++ : Erreurs de précision à résoudre
  - PyMastic C++ port : Erreurs de précision à corriger
  - Voir `docs/PYMASTIC_CPP_DEBUG_PLAN.md` pour les plans d'optimisation

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
- Python 3.8+ (pour PyMastic bridge - **REQUIS pour la production**)
- CMake 3.20+ (pour compilation moteur C++)

### Compilation

1. **Cloner le repository**
   ```bash
   git clone https://github.com/romeoolouro/UI_ChausseeNeuve.git
   cd UI_ChausseeNeuve
   ```

2. **Installer PyMastic Python** (REQUIS)
   ```bash
   cd PavementCalculationEngine/extern
   git clone https://github.com/Mostafa-Nakhaei/PyMastic.git
   cd PyMastic
   pip install -r requirements.txt
   ```

3. **Compiler le moteur de calcul**
   ```bash
   cd PavementCalculationEngine
   build_dll_clean.bat
   ```

4. **Compiler l'application**
   ```bash
   dotnet build UI_ChausseeNeuve.sln -c Release
   ```

### Configuration Production

- ✅ Console désactivée : `OutputType` = `WinExe`
- ✅ Tests exclus : Non inclus dans `UI_ChausseeNeuve.sln`
- ✅ Build Release : Optimisé pour déploiement

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

- **PyMastic Python Bridge** : 
  - Utilise le repos officiel [PyMastic de Mostafa Nakhaei](https://github.com/Mostafa-Nakhaei/PyMastic)
  - Validé contre Tableau I.1 avec erreur de 0.01% (711.6 μɛ vs 711.5±4 μɛ attendu)
  - **Seul algorithme actuellement en production**
- **Développements expérimentaux** :
  - TRMM Solver C++ : Erreurs de précision non résolues
  - PyMastic C++ : Port avec erreurs de précision significatives (>1500×)
  - Plans de debug documentés dans `docs/PYMASTIC_CPP_DEBUG_PLAN.md`

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
✅ **PyMastic Python Bridge** : **EN PRODUCTION** - Seul algorithme validé (0.01% erreur)  
❌ **TRMM Solver C++** : Erreurs de précision - Développement futur  
❌ **PyMastic C++ Port** : Erreurs de précision - Développement futur  
✅ **Integration .NET** : API C complète avec subprocess Python  
🔄 **Optimisation C++** : Planifiée (voir `docs/PYMASTIC_CPP_DEBUG_PLAN.md`)

## 🤝 Contribution

Pour contribuer au projet, consultez l'historique de développement dans `archive/development-history/`.

## 📄 Licence

[À définir selon les besoins du projet]
