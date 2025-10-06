#include "PavementAPI.h"
#include "TRMMSolver.h"
#include <stdio.h>
#include <stdlib.h>

int main() {
    printf("=== TRMM Test 5 Validation ===\n\n");
    
    PavementInputC input;
    input.nlayer = 2;
    
    double E[] = {5000.0, 50.0};
    double nu[] = {0.35, 0.35};
    double h[] = {0.20, 10.0};
    int bonded[] = {1};
    
    input.young_modulus = E;
    input.poisson_ratio = nu;
    input.thickness = h;
    input.bonded_interface = bonded;
    
    input.wheel_type = 0;
    input.pressure_kpa = 662.0;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    
    input.nz = 1;
    double z[] = {0.0};
    input.z_coords = z;
    
    printf("Test Configuration:\n");
    printf("  Layer 1: E=%.0f MPa, nu=%.2f, h=%.2f m\n", E[0], nu[0], h[0]);
    printf("  Layer 2: E=%.0f MPa, nu=%.2f, h=%.2f m (semi-infinite)\n", E[1], nu[1], h[1]);
    printf("  Load: P=%.0f kPa, radius=%.3f m\n\n", input.pressure_kpa, input.wheel_radius_m);
    
    PavementCalculation::TRMMSolver solver;
    
    PavementOutputC output = {0};
    
    printf("Calling TRMM solver...\n");
    bool result = solver.CalculateStable(input, output);
    
    printf("\n=== RESULTS ===\n");
    printf("Success: %s\n", result ? "YES" : "NO");
    printf("Error code: %d\n", output.error_code);
    
    if (result && output.deflection_mm) {
        printf("\nSurface deflection: %.6f mm\n", output.deflection_mm[0]);
        printf("Expected: > 0.0 mm (TMM gave 0.0 due to overflow)\n");
        
        if (output.deflection_mm[0] > 0.0) {
            printf("\n*** SUCCESS: TRMM avoided exponential overflow! ***\n");
        }
    } else {
        printf("Error message: %s\n", output.error_message);
    }
    
    PavementFreeOutput(&output);
    
    return result ? 0 : 1;
}
