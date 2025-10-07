/*
 * VALIDATION PHASE 2 - TRMM avec propagation complete matrices T/R
 * Tests contre tableaux de reference (Tableau I.1, I.5)
 * 
 * Objectif: Erreur < 0.1% vs valeurs attendues
 */

#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "../include/PavementAPI.h"

#define TEST_PASSED "\033[1;32m[PASS]\033[0m"
#define TEST_FAILED "\033[1;31m[FAIL]\033[0m"
#define TEST_TITLE  "\033[1;36m%s\033[0m\n"

void test_tableau_i1_structure_souple() {
    printf(TEST_TITLE, "=== TEST TABLEAU I.1: STRUCTURE SOUPLE ===");
    printf("Configuration: BBM(E=5500, h=0.04m) / GNT(E=600, h=0.15m) / PF2(E=50 MPa)\n");
    printf("Valeur attendue: εz = 711.5 ± 4 μdef (axe de roue)\n\n");
    
    PavementInputC input;
    PavementOutputC output;
    
    // Structure souple (Tableau I.1)
    input.nlayer = 3;
    input.nz = 1;
    
    // Allouer tableaux dynamiquement
    input.young_modulus = (double*)malloc(3 * sizeof(double));
    input.poisson_ratio = (double*)malloc(3 * sizeof(double));
    input.thickness = (double*)malloc(3 * sizeof(double));
    input.bonded_interface = (int*)malloc(2 * sizeof(int));
    input.z_coords = (double*)malloc(1 * sizeof(double));
    
    output.deflection_mm = (double*)malloc(1 * sizeof(double));
    output.vertical_stress_kpa = (double*)malloc(1 * sizeof(double));
    output.horizontal_strain = (double*)malloc(1 * sizeof(double));
    output.radial_strain = (double*)malloc(1 * sizeof(double));
    output.shear_stress_kpa = (double*)malloc(1 * sizeof(double));
    
    // Couches
    input.young_modulus[0] = 5500.0;  // BBM
    input.young_modulus[1] = 600.0;   // GNT
    input.young_modulus[2] = 50.0;    // PF2
    
    input.poisson_ratio[0] = 0.35;
    input.poisson_ratio[1] = 0.35;
    input.poisson_ratio[2] = 0.35;
    
    input.thickness[0] = 0.04;   // 4 cm
    input.thickness[1] = 0.15;   // 15 cm
    input.thickness[2] = 100.0;  // Semi-infini
    
    input.bonded_interface[0] = 1; // BBM-GNT collée
    input.bonded_interface[1] = 1; // GNT-PF2 collée
    
    // Charge standard
    input.wheel_type = 0; // Simple
    input.pressure_kpa = 662.0;
    input.wheel_radius_m = 0.1125;
    input.wheel_spacing_m = 0.0;
    
    // Point mesure: z = 0.19 m (base GNT - axe roue)
    input.z_coords[0] = 0.19;
    
    int result = PavementCalculateStable(&input, &output);
    
    if (result == 0) {
        double epsilon_z_measured = output.radial_strain[0]; // microstrain
        double epsilon_z_expected = 711.5;
        double error_percent = fabs(epsilon_z_measured - epsilon_z_expected) / epsilon_z_expected * 100.0;
        
        printf("✓ Calcul réussi\n");
        printf("  εz mesuré  = %.2f μdef\n", epsilon_z_measured);
        printf("  εz attendu = %.2f μdef\n", epsilon_z_expected);
        printf("  Erreur     = %.4f%%\n", error_percent);
        
        if (error_percent < 0.5) { // Tolerance 0.5%
            printf("%s Validation Tableau I.1: εz dans tolérance (< 0.5%%)\n\n", TEST_PASSED);
        } else {
            printf("%s Validation Tableau I.1: Erreur trop grande (%.4f%% > 0.5%%)\n\n", TEST_FAILED, error_percent);
        }
    } else {
        printf("%s Calcul échoué (code %d)\n\n", TEST_FAILED, result);
    }
    
    // Libérer mémoire
    free(input.young_modulus);
    free(input.poisson_ratio);
    free(input.thickness);
    free(input.bonded_interface);
    free(input.z_coords);
    free(output.deflection_mm);
    free(output.vertical_stress_kpa);
    free(output.horizontal_strain);
    free(output.radial_strain);
    free(output.shear_stress_kpa);
}

void test_tableau_i5_semi_collee() {
    printf(TEST_TITLE, "=== TEST TABLEAU I.5: STRUCTURE SEMI-RIGIDE (SEMI-COLLEE) ===");
    printf("Configuration: BBSG(E=7000, h=0.06m) / GC-T3(E=23000, h=0.15m, semi) / GC-T3(E=23000, h=0.15m, collée) / PF3(E=120 MPa)\n");
    printf("Valeur attendue: σt = 0.612 ± 0.003 MPa (centre jumelage, base GC semi-collée)\n\n");
    
    PavementInputC input;
    PavementOutputC output;
    
    input.nlayer = 4;
    input.nz = 1;
    
    // Allouer tableaux
    input.young_modulus = (double*)malloc(4 * sizeof(double));
    input.poisson_ratio = (double*)malloc(4 * sizeof(double));
    input.thickness = (double*)malloc(4 * sizeof(double));
    input.bonded_interface = (int*)malloc(3 * sizeof(int));
    input.z_coords = (double*)malloc(1 * sizeof(double));
    
    output.deflection_mm = (double*)malloc(1 * sizeof(double));
    output.vertical_stress_kpa = (double*)malloc(1 * sizeof(double));
    output.horizontal_strain = (double*)malloc(1 * sizeof(double));
    output.radial_strain = (double*)malloc(1 * sizeof(double));
    output.shear_stress_kpa = (double*)malloc(1 * sizeof(double));
    
    // Couches
    input.young_modulus[0] = 7000.0;   // BBSG
    input.young_modulus[1] = 23000.0;  // GC-T3 semi-collée
    input.young_modulus[2] = 23000.0;  // GC-T3 collée
    input.young_modulus[3] = 120.0;    // PF3
    
    input.poisson_ratio[0] = 0.35;
    input.poisson_ratio[1] = 0.35;
    input.poisson_ratio[2] = 0.35;
    input.poisson_ratio[3] = 0.35;
    
    input.thickness[0] = 0.06;   // 6 cm
    input.thickness[1] = 0.15;   // 15 cm
    input.thickness[2] = 0.15;   // 15 cm
    input.thickness[3] = 100.0;  // Semi-infini
    
    input.bonded_interface[0] = 1; // BBSG-GC collée
    input.bonded_interface[1] = 0; // GC-GC semi-collée
    input.bonded_interface[2] = 1; // GC-PF3 collée
    
    // Charge standard
    input.wheel_type = 0;
    input.pressure_kpa = 662.0;
    input.wheel_radius_m = 0.1125;
    input.wheel_spacing_m = 0.0;
    
    // Point mesure: z = 0.21 m (base GC-T3 semi-collée)
    input.z_coords[0] = 0.21;
    
    int result = PavementCalculateStable(&input, &output);
    
    if (result == 0) {
        double sigma_t_measured = fabs(output.vertical_stress_kpa[0]) / 1000.0; // MPa
        double sigma_t_expected = 0.612;
        double error_percent = fabs(sigma_t_measured - sigma_t_expected) / sigma_t_expected * 100.0;
        
        printf("✓ Calcul réussi\n");
        printf("  σt mesuré  = %.4f MPa\n", sigma_t_measured);
        printf("  σt attendu = %.4f MPa\n", sigma_t_expected);
        printf("  Erreur     = %.4f%%\n", error_percent);
        
        if (error_percent < 0.5) {
            printf("%s Validation Tableau I.5 (semi-collée): σt dans tolérance (< 0.5%%)\n\n", TEST_PASSED);
        } else {
            printf("%s Validation Tableau I.5 (semi-collée): Erreur trop grande (%.4f%% > 0.5%%)\n\n", TEST_FAILED, error_percent);
        }
    } else {
        printf("%s Calcul échoué (code %d)\n\n", TEST_FAILED, result);
    }
    
    free(input.young_modulus);
    free(input.poisson_ratio);
    free(input.thickness);
    free(input.bonded_interface);
    free(input.z_coords);
    free(output.deflection_mm);
    free(output.vertical_stress_kpa);
    free(output.horizontal_strain);
    free(output.radial_strain);
    free(output.shear_stress_kpa);
}

void test_tableau_i5_collee() {
    printf(TEST_TITLE, "=== TEST TABLEAU I.5: STRUCTURE SEMI-RIGIDE (COLLEE) ===");
    printf("Configuration: BBSG(E=7000, h=0.06m) / GC-T3(E=23000, h=0.15m, collée) / PF3(E=120 MPa)\n");
    printf("Valeur attendue: σt = 0.815 ± 0.003 MPa (centre jumelage, base GC collée)\n\n");
    
    PavementInputC input;
    PavementOutputC output;
    
    input.nlayer = 3;
    input.nz = 1;
    
    // Allouer tableaux
    input.young_modulus = (double*)malloc(3 * sizeof(double));
    input.poisson_ratio = (double*)malloc(3 * sizeof(double));
    input.thickness = (double*)malloc(3 * sizeof(double));
    input.bonded_interface = (int*)malloc(2 * sizeof(int));
    input.z_coords = (double*)malloc(1 * sizeof(double));
    
    output.deflection_mm = (double*)malloc(1 * sizeof(double));
    output.vertical_stress_kpa = (double*)malloc(1 * sizeof(double));
    output.horizontal_strain = (double*)malloc(1 * sizeof(double));
    output.radial_strain = (double*)malloc(1 * sizeof(double));
    output.shear_stress_kpa = (double*)malloc(1 * sizeof(double));
    
    // Couches
    input.young_modulus[0] = 7000.0;   // BBSG
    input.young_modulus[1] = 23000.0;  // GC-T3 collée
    input.young_modulus[2] = 120.0;    // PF3
    
    input.poisson_ratio[0] = 0.35;
    input.poisson_ratio[1] = 0.35;
    input.poisson_ratio[2] = 0.35;
    
    input.thickness[0] = 0.06;   // 6 cm
    input.thickness[1] = 0.15;   // 15 cm
    input.thickness[2] = 100.0;  // Semi-infini
    
    input.bonded_interface[0] = 1; // BBSG-GC collée
    input.bonded_interface[1] = 1; // GC-PF3 collée
    
    // Charge standard
    input.wheel_type = 0;
    input.pressure_kpa = 662.0;
    input.wheel_radius_m = 0.1125;
    input.wheel_spacing_m = 0.0;
    
    // Point mesure: z = 0.21 m (base GC-T3 collée)
    input.z_coords[0] = 0.21;
    
    int result = PavementCalculateStable(&input, &output);
    
    if (result == 0) {
        double sigma_t_measured = fabs(output.vertical_stress_kpa[0]) / 1000.0; // MPa
        double sigma_t_expected = 0.815;
        double error_percent = fabs(sigma_t_measured - sigma_t_expected) / sigma_t_expected * 100.0;
        
        printf("✓ Calcul réussi\n");
        printf("  σt mesuré  = %.4f MPa\n", sigma_t_measured);
        printf("  σt attendu = %.4f MPa\n", sigma_t_expected);
        printf("  Erreur     = %.4f%%\n", error_percent);
        
        if (error_percent < 0.5) {
            printf("%s Validation Tableau I.5 (collée): σt dans tolérance (< 0.5%%)\n\n", TEST_PASSED);
        } else {
            printf("%s Validation Tableau I.5 (collée): Erreur trop grande (%.4f%% > 0.5%%)\n\n", TEST_FAILED, error_percent);
        }
    } else {
        printf("%s Calcul échoué (code %d)\n\n", TEST_FAILED, result);
    }
    
    free(input.young_modulus);
    free(input.poisson_ratio);
    free(input.thickness);
    free(input.bonded_interface);
    free(input.z_coords);
    free(output.deflection_mm);
    free(output.vertical_stress_kpa);
    free(output.horizontal_strain);
    free(output.radial_strain);
    free(output.shear_stress_kpa);
}

void test_numerical_stability_phase2() {
    printf(TEST_TITLE, "=== TEST STABILITE NUMERIQUE PHASE 2 ===");
    printf("Configuration: E=5000/50 MPa, h=0.20m (cas critique Phase 1)\n\n");
    
    PavementInputC input;
    PavementOutputC output;
    
    input.nlayer = 2;
    input.nz = 2;
    
    // Allouer tableaux
    input.young_modulus = (double*)malloc(2 * sizeof(double));
    input.poisson_ratio = (double*)malloc(2 * sizeof(double));
    input.thickness = (double*)malloc(2 * sizeof(double));
    input.bonded_interface = (int*)malloc(1 * sizeof(int));
    input.z_coords = (double*)malloc(2 * sizeof(double));
    
    output.deflection_mm = (double*)malloc(2 * sizeof(double));
    output.vertical_stress_kpa = (double*)malloc(2 * sizeof(double));
    output.horizontal_strain = (double*)malloc(2 * sizeof(double));
    output.radial_strain = (double*)malloc(2 * sizeof(double));
    output.shear_stress_kpa = (double*)malloc(2 * sizeof(double));
    
    input.young_modulus[0] = 5000.0;
    input.young_modulus[1] = 50.0;
    
    input.poisson_ratio[0] = 0.35;
    input.poisson_ratio[1] = 0.35;
    
    input.thickness[0] = 0.20;   // 20 cm (critique)
    input.thickness[1] = 100.0;
    
    input.bonded_interface[0] = 1;
    
    input.wheel_type = 0;
    input.pressure_kpa = 662.0;
    input.wheel_radius_m = 0.1125;
    input.wheel_spacing_m = 0.0;
    
    input.z_coords[0] = 0.10;
    input.z_coords[1] = 0.20;
    
    int result = PavementCalculateStable(&input, &output);
    
    if (result == 0) {
        int all_non_zero = 1;
        for (int i = 0; i < input.nz; i++) {
            if (fabs(output.deflection_mm[i]) < 1e-9) {
                all_non_zero = 0;
                break;
            }
        }
        
        if (all_non_zero) {
            printf("%s Toutes valeurs NON NULLES avec Phase 2\n", TEST_PASSED);
            printf("  z=0.10m: w=%.4f mm, εT=%.2f μdef\n", 
                   output.deflection_mm[0], output.horizontal_strain[0]);
            printf("  z=0.20m: w=%.4f mm, εT=%.2f μdef\n\n", 
                   output.deflection_mm[1], output.horizontal_strain[1]);
        } else {
            printf("%s Valeurs nulles détectées (Phase 2 échoué)\n\n", TEST_FAILED);
        }
    } else {
        printf("%s Calcul échoué (code %d)\n\n", TEST_FAILED, result);
    }
    
    free(input.young_modulus);
    free(input.poisson_ratio);
    free(input.thickness);
    free(input.bonded_interface);
    free(input.z_coords);
    free(output.deflection_mm);
    free(output.vertical_stress_kpa);
    free(output.horizontal_strain);
    free(output.radial_strain);
    free(output.shear_stress_kpa);
}

int main() {
    printf("\n");
    printf("╔══════════════════════════════════════════════════════════════╗\n");
    printf("║   VALIDATION PHASE 2 - PROPAGATION COMPLETE MATRICES T/R    ║\n");
    printf("║           Tests contre tableaux de reference                 ║\n");
    printf("╚══════════════════════════════════════════════════════════════╝\n");
    printf("\n");
    
    // Tests validation
    test_tableau_i1_structure_souple();
    test_tableau_i5_semi_collee();
    test_tableau_i5_collee();
    test_numerical_stability_phase2();
    
    printf("╔══════════════════════════════════════════════════════════════╗\n");
    printf("║                  TESTS PHASE 2 TERMINÉS                      ║\n");
    printf("╚══════════════════════════════════════════════════════════════╝\n");
    printf("\n");
    
    return 0;
}
