/**
 * @file test_trmm_test5.c
 * @brief Test TRMM solver with Test 5 configuration (high m*h value)
 * 
 * This test validates TRMM numerical stability for:
 * - 2 layers (E=5000 MPa / 50 MPa)
 * - h=0.20m thick first layer
 * - Bonded interface
 * - Expected m*h = 36.96 (causes overflow with TMM, stable with TRMM)
 */

#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "../include/PavementAPI.h"

int main(void) {
    printf("=== TRMM Test 5: High m*h value (overflow prevention) ===\n\n");
    
    // Test 5 configuration from TEST_RESULTS_ANALYSIS.md
    PavementInputC input = {0};
    
    // 2 layers: stiff over soft (high stiffness contrast)
    input.nlayer = 2;
    
    // Allocate arrays
    input.poisson_ratio = (double*)malloc(2 * sizeof(double));
    input.young_modulus = (double*)malloc(2 * sizeof(double));
    input.thickness = (double*)malloc(2 * sizeof(double));
    input.bonded_interface = (int*)malloc(1 * sizeof(int));
    input.z_coords = (double*)malloc(3 * sizeof(double));
    
    // Layer 1: Stiff (5000 MPa, 0.20m thick)
    input.poisson_ratio[0] = 0.35;
    input.young_modulus[0] = 5000.0;  // MPa
    input.thickness[0] = 0.20;        // meters
    
    // Layer 2: Soft subgrade (50 MPa, infinite)
    input.poisson_ratio[1] = 0.40;
    input.young_modulus[1] = 50.0;    // MPa
    input.thickness[1] = 10.0;        // meters (semi-infinite approximation)
    
    // Bonded interface
    input.bonded_interface[0] = 1;
    
    // Load configuration
    input.wheel_type = WHEEL_TYPE_SIMPLE;
    input.pressure_kpa = 700.0;       // kPa
    input.wheel_radius_m = 0.15;      // meters
    input.wheel_spacing_m = 0.0;      // Not used for simple wheel
    
    // Calculation points (top, mid-layer 1, interface)
    input.nz = 3;
    input.z_coords[0] = 0.0;          // Surface
    input.z_coords[1] = 0.10;         // Mid-layer 1
    input.z_coords[2] = 0.20;         // Interface (bottom of layer 1)
    
    printf("Test Configuration:\n");
    printf("  Layer 1: E = %.0f MPa, nu = %.2f, h = %.2f m\n", 
           input.young_modulus[0], input.poisson_ratio[0], input.thickness[0]);
    printf("  Layer 2: E = %.0f MPa, nu = %.2f\n", 
           input.young_modulus[1], input.poisson_ratio[1]);
    printf("  Load: P = %.0f kPa, radius = %.2f m\n\n", 
           input.pressure_kpa, input.wheel_radius_m);
    
    // Calculate expected m parameter (approximate)
    double E1 = input.young_modulus[0];
    double nu1 = input.poisson_ratio[0];
    double lambda = E1 * nu1 / ((1.0 + nu1) * (1.0 - 2.0 * nu1));
    double mu = E1 / (2.0 * (1.0 + nu1));
    double m = sqrt((lambda + 2.0 * mu) / mu) / input.wheel_radius_m;
    double mh = m * input.thickness[0];
    
    printf("Numerical Stability Analysis:\n");
    printf("  m parameter: %.3f (1/m)\n", m);
    printf("  m * h: %.2f\n", mh);
    printf("  exp(+m*h): %.2e %s\n", exp(mh), mh > 30 ? "(OVERFLOW RISK)" : "(stable)");
    printf("  exp(-m*h): %.2e (stable, bounded)\n\n", exp(-mh));
    
    // Call TRMM solver
    PavementOutputC output = {0};
    printf("Calling PavementCalculateStable()...\n");
    int result = PavementCalculateStable(&input, &output);
    
    printf("\nResults:\n");
    printf("  Return code: %d (%s)\n", result, result == PAVEMENT_SUCCESS ? "SUCCESS" : "FAILED");
    printf("  Calculation time: %.2f ms\n", output.calculation_time_ms);
    printf("  Error message: %s\n\n", output.error_message);
    
    if (result == PAVEMENT_SUCCESS && output.success == 1) {
        printf("Deflections at calculation points:\n");
        for (int i = 0; i < output.nz; i++) {
            printf("  z = %.2f m: deflection = %.4f mm\n", 
                   input.z_coords[i], output.deflection_mm[i]);
        }
        printf("\n");
        
        printf("Stresses at calculation points:\n");
        for (int i = 0; i < output.nz; i++) {
            printf("  z = %.2f m: sigma_z = %.2f kPa\n", 
                   input.z_coords[i], output.vertical_stress_kpa[i]);
        }
        printf("\n");
        
        // Validation
        printf("Validation:\n");
        if (output.deflection_mm[0] > 0.0) {
            printf("  [PASS] Surface deflection > 0 (%.4f mm)\n", output.deflection_mm[0]);
        } else {
            printf("  [FAIL] Surface deflection = 0 (numerical overflow detected)\n");
        }
        
        if (output.deflection_mm[0] < 10.0) {
            printf("  [PASS] Deflection within realistic range (< 10 mm)\n");
        } else {
            printf("  [WARN] Deflection seems high (%.4f mm)\n", output.deflection_mm[0]);
        }
        
        // Free output arrays
        PavementFreeOutput(&output);
        
    } else {
        printf("[FAIL] Calculation failed\n");
    }
    
    // Free input arrays
    free(input.poisson_ratio);
    free(input.young_modulus);
    free(input.thickness);
    free(input.bonded_interface);
    free(input.z_coords);
    
    printf("\n=== Test Complete ===\n");
    return (result == PAVEMENT_SUCCESS && output.success == 1) ? 0 : 1;
}
