/**
 * @brief C API test for PyMastic integration
 * Task 2A.3.3: Test PyMastic integration through PavementAPI
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "PavementAPI.h"

int main() {
    printf("PyMastic C API Integration Test\n");
    printf("===============================\n\n");
    
    // Setup input structure matching Python test case
    PavementInputC input;
    memset(&input, 0, sizeof(input));
    
    // Layer configuration (3 layers: AC/Base/Subgrade)
    input.nlayer = 3;
    input.poisson_ratio = (double*)malloc(3 * sizeof(double));
    input.young_modulus = (double*)malloc(3 * sizeof(double));
    input.thickness = (double*)malloc(3 * sizeof(double));
    input.bonded_interface = (int*)malloc(2 * sizeof(int));
    
    // Convert from PyMastic test units: ksi -> MPa, inch -> m
    input.poisson_ratio[0] = 0.35;
    input.poisson_ratio[1] = 0.40;  
    input.poisson_ratio[2] = 0.45;
    
    // Convert ksi to MPa: 1 ksi = 6.895 MPa
    input.young_modulus[0] = 500.0 * 6.895;  // 3447.5 MPa
    input.young_modulus[1] = 40.0 * 6.895;   // 275.8 MPa
    input.young_modulus[2] = 10.0 * 6.895;   // 68.95 MPa
    
    // Convert inch to m: 1 inch = 0.0254 m
    input.thickness[0] = 10.0 * 0.0254;  // 0.254 m
    input.thickness[1] = 6.0 * 0.0254;   // 0.1524 m
    input.thickness[2] = 0.0;            // Infinite (ignored)
    
    input.bonded_interface[0] = 0;  // Frictionless
    input.bonded_interface[1] = 0;  // Frictionless
    
    // Load configuration
    input.wheel_type = WHEEL_TYPE_SIMPLE;
    input.pressure_kpa = 100.0 * 6.895;  // Convert lb/in² to kPa
    input.wheel_radius_m = 5.99 * 0.0254;  // Convert inch to m
    input.wheel_spacing_m = 0.0;  // Not used for simple wheel
    
    // Calculation points (convert inch to m)
    input.nz = 3;
    input.z_coords = (double*)malloc(3 * sizeof(double));
    input.z_coords[0] = 0.0;
    input.z_coords[1] = 9.99 * 0.0254;   // 0.253746 m
    input.z_coords[2] = 10.01 * 0.0254;  // 0.254254 m
    
    printf("Input Configuration:\n");
    printf("- Layers: %d\n", input.nlayer);
    printf("- Pressure: %.2f kPa\n", input.pressure_kpa);
    printf("- Radius: %.4f m\n", input.wheel_radius_m);
    printf("- Z-coords: [%.3f, %.3f, %.3f] m\n", 
           input.z_coords[0], input.z_coords[1], input.z_coords[2]);
    printf("- E-moduli: [%.1f, %.1f, %.1f] MPa\n",
           input.young_modulus[0], input.young_modulus[1], input.young_modulus[2]);
    printf("\n");
    
    // Skip standard validation for PyMastic (uses different validation logic)
    printf("Skipping standard validation (PyMastic has its own validation)\n\n");
    
    // Call PyMastic calculation
    PavementOutputC output;
    printf("Calling PavementCalculatePyMastic...\n");
    int result = PavementCalculatePyMastic(&input, &output);
    
    printf("Calculation result: %d\n", result);
    printf("Success flag: %d\n", output.success);
    printf("Error code: %d\n", output.error_code);
    printf("Error message: %s\n", output.error_message);
    printf("Calculation time: %.3f ms\n", output.calculation_time_ms);
    printf("\n");
    
    if (result == PAVEMENT_SUCCESS && output.success) {
        printf("PyMastic Results:\n");
        printf("================\n");
        for (int i = 0; i < output.nz; i++) {
            printf("Point %d (z=%.3fm):\n", i, input.z_coords[i]);
            printf("  Deflection: %.6f mm\n", output.deflection_mm[i]);
            printf("  Vertical stress: %.2f kPa\n", output.vertical_stress_kpa[i]);
            printf("  Horizontal strain: %.1f µε\n", output.horizontal_strain[i]);
            printf("  Radial strain: %.1f µε\n", output.radial_strain[i]);
            printf("  Shear stress: %.2f kPa\n", output.shear_stress_kpa[i]);
            printf("\n");
        }
        
        // Free output memory
        PavementFreeOutput(&output);
        
        printf("*** PYMASTIC INTEGRATION TEST PASSED ***\n");
    } else {
        printf("*** PYMASTIC INTEGRATION TEST FAILED ***\n");
    }

cleanup:
    // Cleanup input memory
    free(input.poisson_ratio);
    free(input.young_modulus);
    free(input.thickness);
    free(input.bonded_interface);
    free(input.z_coords);
    
    return (result == PAVEMENT_SUCCESS) ? 0 : 1;
}