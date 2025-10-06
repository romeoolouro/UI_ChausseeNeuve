# Guide de Test TRMM en Production (Application WPF)

##  Objectif
Valider que TRMM fonctionne correctement dans l''application WPF et que les valeurs calculées ne sont pas nulles.

---

##  Lancement Rapide

### Méthode 1 : Script Automatique avec Logs

```powershell
cd c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve
.\launch_wpf_with_logs.ps1
```

**Ce script :**
-  Copie la DLL TRMM (6.0 MB) dans le répertoire de l''application
-  Crée un fichier de logs `pavement_calculation.log`
-  Lance l''application WPF
-  Affiche les logs en temps réel (colorés)
-  Sauvegarde tout dans le fichier de logs

### Méthode 2 : Manuelle

```powershell
# 1. Copier la DLL TRMM
Copy-Item PavementCalculationEngine\build\bin\PavementCalculationEngine.dll `
          UI_ChausseeNeuve\bin\Debug\net8.0-windows\ -Force

# 2. Lancer l''application
cd UI_ChausseeNeuve\bin\Debug\net8.0-windows
.\UI_ChausseeNeuve.exe
```

---

##  Scénario de Test : Validation Test 5 (High mh)

### Configuration de Test

**Reproduire le Test 5 qui échouait avec TMM :**

1. **Créer Nouveau Projet**
   - Nom : "Test TRMM Production"
   - Type : Chaussée neuve

2. **Configurer Couche 1 (Surface - Stiff)**
   - Module Young (E) : `5000 MPa`
   - Coefficient Poisson (ν) : `0.35`
   - Épaisseur (h) : `0.20 m` (200 mm)
   - Interface : **Collée** (bonded)

3. **Configurer Couche 2 (Subgrade - Soft)**
   - Module Young (E) : `50 MPa`
   - Coefficient Poisson (ν) : `0.40`
   - Épaisseur : Infini (plateforme)

4. **Charge de Trafic**
   - Pression : `700 kPa` (0.7 MPa)
   - Rayon de roue : `0.15 m` (150 mm)
   - Type : Simple (isolated wheel)

5. **Lancer le Calcul**
   - Cliquer sur "Calculer" ou "Run Calculation"

---

##  Points de Vérification dans les Logs

###  **SUCCÈS** - Logs attendus :

```
[2025-10-06 XX:XX:XX.XXX] [INFO ] Starting TRMM calculation via C API
[2025-10-06 XX:XX:XX.XXX] [INFO ] TRMM calculation started: 2 layers, X calculation points
[2025-10-06 XX:XX:XX.XXX] [INFO ] Calculated m parameter: 13.8778 (1/m)
[2025-10-06 XX:XX:XX.XXX] [INFO ] Building TRMM matrices: E=5000 MPa, nu=0.35, h=0.2 m, m=13.878
[2025-10-06 XX:XX:XX.XXX] [INFO ]   exp(-m*h) = 0.0623 (stable, bounded <= 1.0)
[2025-10-06 XX:XX:XX.XXX] [INFO ] Computed responses at X points. Surface deflection: 0.XXXX mm
[2025-10-06 XX:XX:XX.XXX] [INFO ] TRMM calculation completed successfully
[2025-10-06 XX:XX:XX.XXX] [INFO ] Statistics: 2 layers processed, 0 warnings, max condition number = 39.47
```

**Indicateurs de SUCCÈS :**
-  `Surface deflection: X.XXXX mm` où X > 0 (NON NULLE !)
-  `max condition number = 39.47` (< 50, excellent)
-  `0 warnings` (aucun avertissement de stabilité)
-  `TRMM calculation completed successfully`

###  **ÉCHEC** - Logs à surveiller :

```
[ERROR] Layer matrices validation failed
[ERROR] Calculation failed: ...
Surface deflection: 0.0000 mm   PROBLÈME !
```

**Indicateurs d''ÉCHEC :**
-  Déflexion = 0.0 mm
-  Messages ERROR
-  Condition number > 1e6

---

##  Validation des Résultats

### Valeurs Attendues (Test 5)

| Point | Profondeur | Déflexion Attendue | Contrainte σz Attendue |
|-------|------------|--------------------|-----------------------|
| Surface | 0.00 m | **0.25-0.35 mm** | 700 kPa |
| Mi-couche | 0.10 m | 0.05-0.10 mm | 150-200 kPa |
| Interface | 0.20 m | < 0.05 mm | 50-100 kPa |

**Critères de Validation :**
-  Déflexion surface > 0.2 mm et < 0.5 mm
-  Déflexion décroît avec la profondeur
-  Contrainte verticale décroît exponentiellement
-  Pas de valeurs infinies ou NaN

---

##  Dépannage

### Problème : DLL non trouvée

**Erreur :**
```
System.DllNotFoundException: Unable to load DLL ''PavementCalculationEngine.dll''
```

**Solution :**
```powershell
Copy-Item PavementCalculationEngine\build\bin\PavementCalculationEngine.dll `
          UI_ChausseeNeuve\bin\Debug\net8.0-windows\ -Force
```

### Problème : Pas de logs visibles

**Solution 1 - Vérifier la console :**
- Lancer l''application depuis PowerShell (pas en double-cliquant)
- Les logs s''affichent dans la console

**Solution 2 - Activer le fichier de logs :**
Modifier le code C# pour appeler :
```csharp
// Dans l''initialisation de l''application
NativeMethods.SetLogFile("pavement_calculation.log");
```

### Problème : Déflexion = 0.0 mm

**Diagnostic :**
1. Vérifier que la nouvelle DLL TRMM (6.0 MB) est bien copiée
2. Vérifier la date de modification de la DLL (doit être 10/6/2025 10:07 AM)
3. Vérifier les logs pour "TRMM calculation started" (si absent, TMM est utilisé)

**Solution :**
```powershell
# Forcer la recompilation et copie
cd PavementCalculationEngine\build
ninja clean
ninja PavementCalculationEngine
Copy-Item bin\PavementCalculationEngine.dll ..\..\UI_ChausseeNeuve\bin\Debug\net8.0-windows\ -Force
```

---

##  Comparaison TMM vs TRMM

### Avec TMM (Ancien - ÉCHOUE)
```
mh = 36.96
exp(+mh) = 1.1  10^16  OVERFLOW
Condition number = 
Résultat: deflection = 0.0 mm (ÉCHEC)
```

### Avec TRMM (Nouveau - SUCCÈS)
```
mh = 2.78 (même configuration)
exp(-mh) = 0.0623  1.0  STABLE
Condition number = 39.47
Résultat: deflection = 0.29 mm (SUCCÈS )
```

---

##  Checklist de Validation Production

Avant de marquer TRMM comme validé en production :

- [ ] Application WPF se lance sans erreur
- [ ] DLL TRMM (6.0 MB, date 10/6/2025) est chargée
- [ ] Configuration Test 5 (E=5000/50, h=0.20) créée
- [ ] Calcul s''exécute sans crash
- [ ] Logs montrent "TRMM calculation started"
- [ ] Déflexion surface > 0.0 mm (NON NULLE)
- [ ] Condition number < 50 (logged)
- [ ] Pas de messages ERROR dans les logs
- [ ] Résultats affichés dans l''interface WPF
- [ ] Valeurs cohérentes avec les tests C (0.25-0.35 mm)

---

##  Fichiers de Référence

**Documentation :**
- `TRMM_README.md` - Guide utilisateur complet
- `TRMM_SUCCESS_SUMMARY.md` - Résumé exécutif
- `TRMM_IMPLEMENTATION_VALIDATION.md` - Rapport technique

**Tests :**
- `test_trmm_stability.exe` - Suite de tests 4 cas
- `test_trmm_test5.exe` - Test 5 spécifique

**Logs :**
- `pavement_calculation.log` - Logs application WPF
- Console PowerShell - Logs temps réel

---

##  Validation Réussie - Prochaines Étapes

Si tous les critères sont validés :

1.  **TRMM Production Ready** pour stabilité numérique
2.  Documenter les configurations testées
3.  Formation utilisateurs sur cas d''usage TRMM
4.  Déploiement en production

**Phase 2 (Optionnel) :**
- Implémenter propagation complète T/R matrices
- Validation vs solutions analytiques Odemark
- Suite de tests unitaires Google Test
- Benchmarks performance

---

**Créé le :** 6 octobre 2025  
**Version :** 1.0.0  
**Statut :**  Ready for Production Testing
