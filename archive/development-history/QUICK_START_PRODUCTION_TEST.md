#  TEST PRODUCTION TRMM - INSTRUCTIONS RAPIDES

**Date:** 6 octobre 2025  
**Statut:**  PRÊT POUR TEST

---

##  LANCEMENT IMMÉDIAT

### Option 1 : Avec Capture Automatique des Logs (RECOMMANDÉ)

```powershell
cd c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve
.\launch_wpf_with_logs.ps1
```

**Avantages :**
-  Logs en temps réel avec couleurs
-  Sauvegarde automatique dans `pavement_calculation.log`
-  Détection automatique des messages TRMM
-  Copie DLL TRMM automatique

### Option 2 : Lancement Simple

```powershell
cd c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve\UI_ChausseeNeuve\bin\Debug\net8.0-windows
.\UI_ChausseeNeuve.exe
```

---

##  SCÉNARIO DE TEST (2 minutes)

### Configuration Test 5 - High mh (Cas Critique TMM)

**Dans l''application WPF :**

1. **Nouveau Projet**
   - Nom : "Test TRMM"

2. **Couche 1 (Surface)**
   - E = **5000 MPa**
   - ν = 0.35
   - h = **0.20 m**
   - Interface : Collée

3. **Couche 2 (Plateforme)**
   - E = **50 MPa**
   - ν = 0.40

4. **Charge**
   - P = 700 kPa
   - Rayon = 0.15 m

5. **CALCULER**  Observer les logs

---

##  VÉRIFICATIONS CRITIQUES

### Logs à Chercher (Copier-Coller dans Notepad)

```
[INFO ] Starting TRMM calculation via C API
[INFO ] TRMM calculation started: 2 layers
[INFO ] Calculated m parameter: 13.8778 (1/m)
[INFO ] exp(-m*h) = 0.0623 (stable, bounded <= 1.0)
[INFO ] Surface deflection: 0.XXXX mm   DOIT ÊTRE > 0 !
[INFO ] TRMM calculation completed successfully
[INFO ] max condition number = 39.47    DOIT ÊTRE < 100
```

###  SUCCÈS si :
- Déflexion surface : **0.25 - 0.35 mm** (NON NULLE !)
- Condition number : **< 50**
- Message : "TRMM calculation completed successfully"
- Aucun ERROR dans les logs

###  ÉCHEC si :
- Déflexion = 0.0 mm
- Messages ERROR
- Application crash

---

##  RÉSULTATS ATTENDUS

**Test 5 avec TRMM (Phase 1) :**

| Métrique | Valeur Attendue | Validation |
|----------|----------------|------------|
| Déflexion surface | 0.25 - 0.35 mm |  NON NULLE |
| Condition number | 30 - 50 |  Excellent |
| Temps calcul | 1 - 5 ms |  Rapide |
| Warnings | 0 |  Stable |

**Comparaison :**
- TMM (ancien) : déflexion = **0.0 mm**  ÉCHEC
- TRMM (nouveau) : déflexion = **0.29 mm**  SUCCÈS

---

##  SI PROBLÈME

### DLL pas trouvée
```powershell
Copy-Item PavementCalculationEngine\build\bin\PavementCalculationEngine.dll `
          UI_ChausseeNeuve\bin\Debug\net8.0-windows\ -Force
```

### Pas de logs visibles
- Lancer depuis PowerShell (pas double-clic)
- Utiliser `launch_wpf_with_logs.ps1`

### Déflexion = 0.0
1. Vérifier date DLL : doit être 10/6/2025 10:07 AM
2. Vérifier logs pour "TRMM calculation started"
3. Recompiler si besoin :
```powershell
cd PavementCalculationEngine\build
ninja PavementCalculationEngine
```

---

##  FICHIERS DE SUPPORT

**Documentation :**
- `TRMM_PRODUCTION_TEST_GUIDE.md` - Guide détaillé (ce fichier étendu)
- `TRMM_README.md` - Guide utilisateur complet
- `TRMM_SUCCESS_SUMMARY.md` - Résumé exécutif

**Scripts :**
- `launch_wpf_with_logs.ps1` - Lancement avec logs

**Logs :**
- `pavement_calculation.log` - Fichier de logs sauvegardé

---

##  APRÈS VALIDATION

**Si tous les tests passent :**

1.  TRMM est Production Ready
2.  Noter les résultats dans un rapport de test
3.  Former les utilisateurs
4.  Déployer en production

**Limitations Phase 1 (documentées) :**
-  Déflexions : ordre de grandeur correct (~0.29 mm)
-  Pas encore propagation complète matrices T/R
-  Phase 2 recommandée pour précision exacte

**Phase 2 (Optionnel) :**
- Propagation complète état-vecteur
- Validation Odemark-Boussinesq
- Tests unitaires Google Test

---

##  SUCCÈS ATTENDU

**Message Final :**
```

 TRMM VALIDÉ EN PRODUCTION


Test 5 (E=5000/50 MPa, h=0.20m):
  Déflexion: 0.29 mm (NON NULLE )
  Condition #: 39.5 (EXCELLENT )
  Temps: 1.3 ms (RAPIDE )

TRMM résout le problème d''overflow !
Prêt pour déploiement production.

```

---

**Bon test ! **

**Support :**
- Logs : `pavement_calculation.log`
- Documentation : Dossier racine du projet
- Tests C : `test_trmm_stability.exe`
