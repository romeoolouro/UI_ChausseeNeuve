/**
 * @brief Final PyMastic Tableaux Validation Test
 * Task 2A.3.2-3: Test full PyMastic implementation with corrected Hankel integration
 * 
 * This test uses the complete PyMasticSolver with:
 * - Corrected SetupHankelGrid() matching Python exactly
 * - Full state vector propagation (A, B, C, D coefficients)
 * - Proper response calculations
 */

// Temporarily undefine DLL export to avoid linkage issues in standalone test
#ifdef PAVEMENT_CALCULATION_API
#undef PAVEMENT_CALCULATION_API
#endif
#define PAVEMENT_CALCULATION_API

#include "../include/PyMasticSolver.h"
#include <iostream>
#include <iomanip>
#include <cmath>

void test_tableau_i1_full() {
    std::cout << "🎯 TABLEAU I.1 - Full PyMastic Test\n";
    std::cout << "====================================\n\n";
    
    PyMasticSolver::Input input;
    
    // Tableau I.1 configuration
    input.q_kpa = 662.0;
    input.a_m = 0.1125;
    input.x_offsets = {0.0};  // Center
    input.z_depths = {0.19};  // Base of GNT layer
    input.H_thicknesses = {0.04, 0.15};
    input.E_moduli = {5500.0, 600.0, 50.0};
    input.nu_poisson = {0.35, 0.35, 0.35};
    input.bonded_interfaces = {1, 1};
    input.iterations = 50;
    input.ZRO = 1e-8;
    input.inverser = "solve";
    
    try {
        PyMasticSolver solver;
        auto output = solver.Compute(input);
        
        double strain_z_microdef = output.strain_z(0, 0) * 1e6;
        double expected = 711.5;
        double error_percent = std::abs(strain_z_microdef - expected) / expected * 100.0;
        
        std::cout << std::fixed << std::setprecision(2);
        std::cout << "Results:\n";
        std::cout << "  Displacement Z: " << output.displacement_z(0, 0) * 1000.0 << " mm\n";
        std::cout << "  Strain εz:      " << strain_z_microdef << " μdef\n";
        std::cout << "  Expected εz:    " << expected << " μdef\n";
        std::cout << "  Error:          " << error_percent << "%\n\n";
        
        if (error_percent < 0.5) {
            std::cout << "✅ EXCELLENT! Academic validation achieved (<0.5%)\n";
        } else if (error_percent < 5.0) {
            std::cout << "✅ VERY GOOD! Close to target\n";
        } else if (error_percent < 20.0) {
            std::cout << "⚠️  IMPROVED but needs fine-tuning\n";
        } else {
            std::cout << "❌ Still needs work\n";
        }
        
    } catch (const std::exception& e) {
        std::cout << "❌ Error: " << e.what() << "\n";
    }
}

void test_tableau_i5_semi() {
    std::cout << "\n🎯 TABLEAU I.5 Semi-Bonded - Full PyMastic Test\n";
    std::cout << "================================================\n\n";
    
    PyMasticSolver::Input input;
    
    input.q_kpa = 662.0;
    input.a_m = 0.1125;
    input.x_offsets = {0.0};
    input.z_depths = {0.21};
    input.H_thicknesses = {0.06, 0.15};
    input.E_moduli = {7000.0, 23000.0, 120.0};
    input.nu_poisson = {0.35, 0.35, 0.35};
    input.bonded_interfaces = {1, 0};  // Semi-bonded
    input.iterations = 50;
    input.ZRO = 1e-8;
    input.inverser = "solve";
    
    try {
        PyMasticSolver solver;
        auto output = solver.Compute(input);
        
        double stress_t_mpa = std::abs(output.stress_t(0, 0)) / 1000.0;
        double expected = 0.612;
        double error_percent = std::abs(stress_t_mpa - expected) / expected * 100.0;
        
        std::cout << std::fixed << std::setprecision(3);
        std::cout << "Results:\n";
        std::cout << "  Stress σt:    " << stress_t_mpa << " MPa\n";
        std::cout << "  Expected σt:  " << expected << " MPa\n";
        std::cout << "  Error:        " << error_percent << "%\n\n";
        
        if (error_percent < 0.5) {
            std::cout << "✅ EXCELLENT! Academic validation achieved (<0.5%)\n";
        } else if (error_percent < 5.0) {
            std::cout << "✅ VERY GOOD! Close to target\n";
        } else if (error_percent < 20.0) {
            std::cout << "⚠️  IMPROVED but needs fine-tuning\n";
        } else {
            std::cout << "❌ Still needs work\n";
        }
        
    } catch (const std::exception& e) {
        std::cout << "❌ Error: " << e.what() << "\n";
    }
}

int main() {
    std::cout << "PyMastic Tableaux Validation - Corrected Integration\n";
    std::cout << "====================================================\n\n";
    std::cout << "Testing full PyMasticSolver with corrected Hankel integration\n";
    std::cout << "Integration method now matches Python MLE.py exactly\n\n";
    
    test_tableau_i1_full();
    test_tableau_i5_semi();
    
    return 0;
}