#  PHASE 2 - PROPAGATION COMPLÈTE MATRICES T/R

**Status Phase 1**:  COMPLÉTÉ ET VALIDÉ  
**Date Phase 1**: 06/10/2025  
**Phase 2 Estimée**: 2-3 semaines  

---

##  OBJECTIF PHASE 2

Passer de **formule analytique simplifiée** à **propagation exacte via matrices T/R** pour obtenir une **précision quantitative parfaite**.

### Différence Phase 1 vs Phase 2

| Aspect | Phase 1 (Actuel) | Phase 2 (Target) |
|--------|------------------|------------------|
| **Stabilité numérique** |  Parfaite (exp(-mh) seulement) |  Maintenue |
| **Précision déflexions** |  Ordre de grandeur correct |  Exacte (< 0.1% erreur) |
| **Précision contraintes** |  Approximative |  Exacte |
| **Méthode** | Formule simplifiée | Propagation complète état-vecteur |
| **Validation** | Tests empiriques | Solutions fermées + MATLAB |
| **Cas d'usage** | Études préliminaires | Dimensionnement final |

---

##  IMPLÉMENTATION PHASE 2

### 1. Modification TRMMSolver.cpp - ComputeResponses()

**Ligne actuelle (TRMMSolver.cpp:200-215):**

```cpp
void TRMMSolver::ComputeResponses(
    const PavementInputC& input,
    const std::vector<LayerMatrices>& layers,
    PavementOutputC& output
) {
    // PHASE 1: FORMULE SIMPLIFIÉE
    for (int i = 0; i < input.nz; i++) {
        double z = input.z_coords[i];
        
        // Formule approximative Boussinesq modifiée
        double E_eff = /* ... calcul module équivalent ... */;
        double deflection = (pressure * radius / E_eff) * some_factor(z);
        
        output.deflections[i] = deflection;
        // Contraintes/déformations dérivées de formule...
    }
}
```

**Phase 2 - Propagation Complète:**

```cpp
void TRMMSolver::ComputeResponses(
    const PavementInputC& input,
    const std::vector<LayerMatrices>& layers,
    PavementOutputC& output
) {
    // PHASE 2: PROPAGATION EXACTE MATRICES T/R
    
    // 1. Vecteur d'état initial (charge surface)
    Eigen::Vector3d state_surface = ComputeLoadVector(input);
    
    // 2. Pour chaque profondeur z demandée
    for (int iz = 0; iz < input.nz; iz++) {
        double z = input.z_coords[iz];
        
        // Trouver couche contenant z
        int layer_idx = FindLayerAtDepth(z, layers);
        
        // 3. Propager état depuis surface jusqu'à z
        Eigen::Vector3d state_at_z = state_surface;
        
        for (int i = 0; i <= layer_idx; i++) {
            double z_in_layer = (i == layer_idx) 
                ? (z - GetLayerTopDepth(i, layers))
                : layers[i].thickness;
            
            // Matrice T pour propagation intra-couche
            Eigen::Matrix3d T_partial = BuildPartialTMatrix(
                layers[i].m_parameter, 
                z_in_layer,
                layers[i].young_modulus,
                layers[i].poisson_ratio
            );
            
            state_at_z = T_partial * state_at_z;
            
            // Si interface liée, appliquer matrice R
            if (i < layer_idx && IsBondedInterface(i, input)) {
                Eigen::Matrix3d R = layers[i].R;
                state_at_z = R * state_at_z;
            }
        }
        
        // 4. Extraire réponses physiques du vecteur d'état
        ExtractPhysicalQuantities(state_at_z, layers[layer_idx], output, iz);
    }
}

// Nouvelles méthodes auxiliaires nécessaires
Eigen::Vector3d ComputeLoadVector(const PavementInputC& input) {
    // Vecteur [w, θ, M] représentant charge appliquée
    double q = input.pressure_kpa / 1000.0; // MPa
    double a = input.wheel_radius_m;
    
    return Eigen::Vector3d(
        q * a * a / 2.0,  // Déplacement vertical initial
        0.0,               // Rotation nulle (charge symétrique)
        q * a * a * a / 4.0  // Moment initial
    );
}

Eigen::Matrix3d BuildPartialTMatrix(double m, double z, double E, double nu) {
    // Matrice T pour propagation sur distance z dans une couche
    // UTILISE UNIQUEMENT exp(-mz) pour stabilité !
    
    double exp_neg_mz = std::exp(-m * z);
    double mz = m * z;
    
    Eigen::Matrix3d T;
    T(0, 0) = exp_neg_mz;
    T(0, 1) = (1.0 - exp_neg_mz) / m;
    T(0, 2) = 0.0;
    
    T(1, 0) = 0.0;
    T(1, 1) = exp_neg_mz;
    T(1, 2) = (1.0 - exp_neg_mz) / (E * m);
    
    T(2, 0) = -E * m * (1.0 - exp_neg_mz);
    T(2, 1) = 0.0;
    T(2, 2) = exp_neg_mz;
    
    return T;
}

void ExtractPhysicalQuantities(
    const Eigen::Vector3d& state,
    const LayerMatrices& layer,
    PavementOutputC& output,
    int iz
) {
    // State vector: [w, θ, M]
    double w = state(0);      // Déplacement vertical
    double theta = state(1);  // Rotation
    double M = state(2);      // Moment
    
    // Déflexion
    output.deflections[iz] = w * 1000.0; // m -> mm
    
    // Contrainte verticale (depuis moment)
    double sigma_z = -M / (layer.thickness * layer.thickness);
    output.vertical_stresses[iz] = sigma_z * 1000.0; // MPa -> kPa
    
    // Déformation radiale (relation constitutive)
    double epsilon_r = (1.0 + layer.poisson_ratio) * sigma_z / layer.young_modulus;
    output.radial_strains[iz] = epsilon_r * 1e6; // -> microstrain
    
    // Déformation horizontale (théorie élasticité)
    double epsilon_h = epsilon_r; // Symétrie axiale
    output.horizontal_strains[iz] = epsilon_h * 1e6;
}

int FindLayerAtDepth(double z, const std::vector<LayerMatrices>& layers) {
    double cumul_depth = 0.0;
    for (size_t i = 0; i < layers.size(); i++) {
        cumul_depth += layers[i].thickness;
        if (z <= cumul_depth || i == layers.size() - 1) {
            return i;
        }
    }
    return layers.size() - 1;
}

double GetLayerTopDepth(int layer_idx, const std::vector<LayerMatrices>& layers) {
    double depth = 0.0;
    for (int i = 0; i < layer_idx; i++) {
        depth += layers[i].thickness;
    }
    return depth;
}
```

---

##  VALIDATION PHASE 2

### 1. Solutions Analytiques Fermées

**Odemark-Boussinesq** (1 couche homogène):

```cpp
TEST(TRMMPhase2, CompareOdemarkSingleLayer) {
    // Configuration simple: 1 couche homogène
    PavementInputC input = CreateSingleLayerInput(
        /*E=*/200.0,   // MPa
        /*nu=*/0.30,
        /*h=*/10.0,    // m (semi-infini pratique)
        /*q=*/0.662,   // MPa
        /*a=*/0.15     // m
    );
    
    PavementOutputC output_trmm;
    TRMMSolver solver;
    solver.CalculateStable(input, output_trmm);
    
    // Solution fermée Boussinesq
    double deflection_boussinesq = (input.pressure_kpa / 1000.0) 
                                  * input.wheel_radius_m 
                                  * (1.0 - 0.30 * 0.30) 
                                  / 200.0;
    
    // Tolérance 0.1%
    EXPECT_NEAR(output_trmm.deflections[0], 
                deflection_boussinesq * 1000.0, 
                deflection_boussinesq * 1.0);
}
```

**Westergaard** (2 couches):

```cpp
TEST(TRMMPhase2, CompareWestergaardTwoLayers) {
    // Configuration: E1/E2 = 10 (bien conditionné)
    // Solution fermée Westergaard disponible
    
    // ... implémentation ...
    
    EXPECT_LT(relative_error, 0.001); // < 0.1%
}
```

### 2. Validation vs MATLAB

**Script MATLAB existant** (`matlab_test_validation.m`):

```matlab
% Exécuter calcul MATLAB
[def_matlab, sigma_matlab, eps_matlab] = pavementCalc(...);

% Exporter résultats pour comparaison C++
save('matlab_reference.mat', 'def_matlab', 'sigma_matlab', 'eps_matlab');
```

**Test C++:**

```cpp
TEST(TRMMPhase2, CompareMatlabReference) {
    // Charger résultats MATLAB
    MatlabReferenceData ref = LoadMatlabReference("matlab_reference.mat");
    
    // Calculer avec TRMM Phase 2
    PavementOutputC output;
    solver.CalculateStable(ref.input, output);
    
    // Comparer point par point
    for (int i = 0; i < ref.nz; i++) {
        double error_deflection = std::abs(
            output.deflections[i] - ref.deflections[i]
        ) / ref.deflections[i];
        
        EXPECT_LT(error_deflection, 0.001); // < 0.1%
    }
}
```

### 3. Tests de Convergence

```cpp
TEST(TRMMPhase2, ConvergenceIntegrationPoints) {
    // Tester que résultats convergent avec + de points intégration
    
    std::vector<int> n_points = {8, 16, 32, 64, 128};
    std::vector<double> deflections;
    
    for (int n : n_points) {
        PavementOutputC output;
        TRMMConfig config;
        config.integration_points = n;
        
        TRMMSolver solver(config);
        solver.CalculateStable(input, output);
        deflections.push_back(output.deflections[0]);
    }
    
    // Vérifier convergence monotone
    for (size_t i = 1; i < deflections.size(); i++) {
        double convergence_rate = std::abs(
            deflections[i] - deflections[i-1]
        ) / deflections[i];
        
        EXPECT_LT(convergence_rate, 0.01 / (1 << i)); // Convergence exponentielle
    }
}
```

---

##  CRITÈRES D'ACCEPTATION PHASE 2

### Précision Quantitative

| Métrique | Cible Phase 2 | Phase 1 Actuel |
|----------|---------------|----------------|
| **Erreur vs Odemark** | < 0.1% | ~5-10% (estimé) |
| **Erreur vs MATLAB** | < 0.1% | ~5-10% (estimé) |
| **Erreur vs Westergaard** | < 0.5% | Non validé |
| **Convergence intégration** | Exponentielle | Non testé |

### Stabilité Numérique (Maintenue)

-  Condition number < 10^6 pour tous cas
-  Pas d'overflow pour mh jusqu'à 200
-  Résidus matriciels < 1e-6

### Performance

-  Temps calcul < 10 ms par structure (vs 270 ms TMM ancien)
-  Pas de régression vs Phase 1

---

##  PLANNING PHASE 2

### Semaine 1: Implémentation Core

**Jours 1-2**: Refactoring ComputeResponses()
- Implémenter propagation état-vecteur
- Méthodes auxiliaires (ComputeLoadVector, BuildPartialTMatrix, etc.)

**Jours 3-4**: Extraction quantités physiques
- ExtractPhysicalQuantities() avec relations constitutives correctes
- Gestion cas limites (z entre couches, etc.)

**Jour 5**: Intégration & tests smoke
- Compiler, résoudre erreurs
- Tests basiques (1 couche, 2 couches simples)

### Semaine 2: Validation Extensive

**Jours 1-2**: Solutions fermées
- Tests Odemark-Boussinesq
- Tests Westergaard
- Analyse écarts, debug si nécessaire

**Jours 3-4**: Validation MATLAB
- Exécuter matlab_test_validation.m
- Créer tests comparatifs C++
- Ajuster si erreurs > 0.1%

**Jour 5**: Tests convergence & performance
- Tests intégration points
- Profiling performance
- Optimisations si nécessaire

### Semaine 3: Finalisation

**Jours 1-2**: Documentation
- Mise à jour TRMM_README.md
- Créer TRMM_PHASE2_VALIDATION.md
- Diagrammes UML si utile

**Jours 3-4**: Tests de régression
- Vérifier tous tests Phase 1 passent encore
- Tests edge cases additionnels
- Stress tests

**Jour 5**: Revue & déploiement
- Code review
- Merge vers main
- Release notes v2.0

---

##  RESSOURCES TECHNIQUES

### Livres de Référence

1. **Huang, Y.H. (2004)**  
   *"Pavement Analysis and Design"*  
   Chapter 3: Stresses and Deflections  
    Formules extraction contraintes depuis état-vecteur

2. **Burmister, D.M. (1943)**  
   *"The Theory of Stresses and Displacements in Layered Systems"*  
    Validation solutions 2-3 couches

### Papers Académiques

1. **Qiu et al. (2025)** - Référence TRMM principale  
2. **Dong et al. (2021)** - Implémentation numérique détaillée  
3. **Fan et al. (2022)** - Applications dynamiques

### Code Existant

- `matlab_test_validation.m` - Référence MATLAB
- `PavementCalculator.cpp` (ancien TMM) - Structure générale à conserver
- `TRMMSolver.cpp` Phase 1 - Base stable à étendre

---

##  CONFIGURATION DÉVELOPPEMENT

### Build Environment

```bash
# Compiler avec tests Phase 2
cd PavementCalculationEngine/build
cmake .. -DBUILD_TESTS=ON -DPHASE2_VALIDATION=ON
ninja

# Exécuter suite tests Phase 2
./tests/phase2_validation_tests
```

### Debug Options

```cpp
// TRMMSolver.h
struct TRMMConfig {
    bool verbose_logging = false;
    bool phase2_mode = true;        // NEW
    bool export_state_vectors = false;  // Pour debug
    double tolerance = 1e-8;
    int integration_points = 32;    // Augmenter pour Phase 2
};
```

---

##  LIVRABLES PHASE 2

### Code

-  `TRMMSolver.cpp` refactorisé avec propagation complète
-  Tests Google Test (>= 20 tests nouveaux)
-  Scripts validation MATLAB
-  Documentation inline complète

### Documentation

-  TRMM_PHASE2_VALIDATION.md (rapport validation)
-  TRMM_README.md mis à jour
-  Diagrammes propagation état-vecteur
-  Guide utilisateur (précision Phase 2 vs Phase 1)

### Validation

-  Tous tests Odemark < 0.1% erreur
-  Tous tests MATLAB < 0.1% erreur
-  Tests convergence passent
-  Performance maintenue/améliorée

---

##  RISQUES & MITIGATION

### Risque 1: Complexité Mathématique

**Probabilité**: Moyenne  
**Impact**: Élevé

**Mitigation**:
- Commencer par cas simple (1 couche) pour validation
- Progression incrémentale (2 couches, puis 3+)
- Consultation références académiques si blocage

### Risque 2: Performance Dégradée

**Probabilité**: Faible  
**Impact**: Moyen

**Mitigation**:
- Profiling dès début
- Eigen optimisé (SIMD) déjà actif
- Caching résultats matrices si nécessaire

### Risque 3: Précision Insuffisante

**Probabilité**: Faible  
**Impact**: Critique

**Mitigation**:
- Validation incrémentale vs solutions fermées
- Ajustement méthode numérique (+ points intégration)
- Consultation experts si erreur persistante

---

##  CHECKLIST AVANT DÉMARRAGE PHASE 2

- [ ] Phase 1 validée et mergée dans main
- [ ] Documentation Phase 1 complète
- [ ] Branch `phase2-propagation` créée
- [ ] Environnement build configuré
- [ ] Références académiques téléchargées
- [ ] MATLAB installé et testé
- [ ] Google Test framework intégré
- [ ] Planning communiqué à l''équipe

---

**FIN DU GUIDE PHASE 2**

**Next Action**: Créer branch `phase2-propagation` et débuter implémentation selon planning ci-dessus.
