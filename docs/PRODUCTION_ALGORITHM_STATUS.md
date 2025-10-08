# État de Production - Algorithmes de Calcul

**Date** : Octobre 2025  
**Version** : Production 1.0

## ✅ Algorithme en Production

### PyMastic Python Bridge

**Statut** : **ACTIF - PRODUCTION**

- **Source** : [PyMastic par Mostafa Nakhaei](https://github.com/Mostafa-Nakhaei/PyMastic)
- **Méthode d'intégration** : Subprocess Python appelé depuis C++ via `PyMasticPythonBridge`
- **Validation** : Tableau I.1 - Erreur 0.01% (711.6 μɛ vs 711.5±4 μɛ attendu)
- **Fichiers clés** :
  - `PavementCalculationEngine/pymastic_bridge.py` - Interface Python
  - `PavementCalculationEngine/src/PyMasticPythonBridge.cpp` - Wrapper C++
  - `PavementCalculationEngine/src/PavementAPI.cpp` - API exposée à .NET

**Pourquoi PyMastic Python ?**
- Seul algorithme avec validation mathématique prouvée
- Précision conforme aux références techniques (Tableau I.1)
- Repos officiel maintenu et documenté

## ❌ Implémentations Expérimentales (NON PRODUCTION)

### 1. TRMM Solver C++

**Statut** : **INACTIF - Erreurs de précision**

- **Fichiers** : `PavementCalculationEngine/src/TRMMSolver.cpp`
- **Problème** : Erreurs de précision non résolues
- **Action future** : Debug planifié (voir `PYMASTIC_CPP_DEBUG_PLAN.md`)

### 2. PyMastic C++ Port

**Statut** : **INACTIF - Erreurs de précision majeures**

- **Fichiers** : `PavementCalculationEngine/src/PyMasticSolver.cpp`
- **Problème** : Erreur de précision >1500× (1,123,227 μɛ vs 711.6 μɛ attendu)
- **Causes identifiées** :
  - Problèmes de conversion d'unités (kPa ↔ psi, m ↔ inches)
  - Erreurs dans les fonctions de Bessel
  - Problèmes d'intégration numérique
- **Action future** : Debug systématique planifié (voir `PYMASTIC_CPP_DEBUG_PLAN.md`)

### 3. Autres tentatives

- **Timm Solver** : Implémentation antérieure abandonnée
- Tests multiples documentés dans `archive/development-history/`

## 🔄 Stratégie de Développement

### Court terme (Production)
- ✅ Utilisation exclusive de PyMastic Python Bridge
- ✅ Performance acceptable via subprocess
- ✅ Fiabilité et précision garanties

### Long terme (Optimisation)
- 🔄 Debug des implémentations C++ pour performance native
- 🔄 Élimination de la dépendance Python (si besoin)
- 🔄 Voir `PYMASTIC_CPP_DEBUG_PLAN.md` pour stratégie détaillée

## 📋 Configuration Actuelle

### Dépendances Production
- **Python 3.8+** : REQUIS
- **PyMastic Python** : REQUIS (extern/PyMastic/)
- **.NET 8** : REQUIS
- **PavementCalculationEngine.dll** : REQUIS

### Configuration Build
- Console : Désactivée (`WinExe`)
- Tests : Non inclus dans solution principale
- Mode : Release optimisé

## 🎯 Pour les Développeurs Futurs

**IMPORTANT** : Ne pas modifier ou remplacer PyMastic Python Bridge sans :
1. Validation mathématique complète (Tableau I.1 minimum)
2. Tests de régression exhaustifs
3. Documentation des résultats de validation

Les implémentations C++ sont disponibles pour référence et développement futur, mais **NE DOIVENT PAS** être activées en production sans validation préalable.

## 📚 Références

- [PyMastic Original](https://github.com/Mostafa-Nakhaei/PyMastic)
- `PYMASTIC_CPP_DEBUG_PLAN.md` - Plan de debug C++
- `archive/development-history/` - Historique des tentatives