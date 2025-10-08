/**
 * Test C++ PyMasticSolver with detailed logging against validated Python parameters
 * Focus on unit conversion and intermediate calculation comparison
 */

#include <iostream>
#include <iomanip>
#include <vector>
#include "../include/PyMasticSolver.h"

void test_pymastic_cpp_detailed()
{
    std::cout << "========================================" << std::endl;
    std::cout << "C++ PYMASTIC DETAILED DEBUG TEST" << std::endl;
    std::cout << "========================================" << std::endl;
    
    // EXACT validated parameters (Tableau I.1 - 0.01% error with Python)
    double q_kpa = 667.0;
    double a_m = 0.1125;
    std::vector<double> E_mpa = {5500.0, 600.0, 50.0};
    std::vector<double> h_m = {0.04, 0.15};
    std::vector<double> nu = {0.35, 0.35, 0.35};
    
    // Convert to US units (PyMastic internal requirement)
    double q_psi = q_kpa * 0.145038;
    double a_in = a_m * 39.3701;
    std::vector<double> H_in = {h_m[0] * 39.3701, h_m[1] * 39.3701};
    std::vector<double> E_ksi = {
        E_mpa[0] * 0.145038,
        E_mpa[1] * 0.145038,
        E_mpa[2] * 0.145038
    };
    
    // Measurement at interface BBM/GNT (validated position)
    std::vector<double> x = {0.0, 8.0};
    std::vector<double> z = {H_in[0]};  // 1.575 inches
    
    std::cout << "\nVALIDATED PARAMETERS:" << std::endl;
    std::cout << std::fixed << std::setprecision(3);
    std::cout << "  Original: q=" << q_kpa << " kPa, a=" << a_m << " m" << std::endl;
    std::cout << "  US Units: q=" << q_psi << " psi, a=" << a_in << " inches" << std::endl;
    std::cout << "  H = [" << H_in[0] << ", " << H_in[1] << "] inches" << std::endl;
    std::cout << "  E = [" << E_ksi[0] << ", " << E_ksi[1] << ", " << E_ksi[2] << "] ksi" << std::endl;
    std::cout << "  z = " << z[0] << " inches (interface BBM/GNT)" << std::endl;
    std::cout << "  Expected Result: εz ≈ 711.6 μɛ (Python validated)" << std::endl;
    
    // Setup PyMastic input using existing interface
    PyMasticSolver::Input input;
    
    // TRY WORKING PORT UNITS (like test_pymastic_port.cpp)
    // Based on existing working tests: use simple values like {500, 40, 10} ksi
    
    input.q_kpa = 100.0;           // Simple pressure (psi?)
    input.a_m = 4.5;               // Simple radius (inches?)
    
    // Measurement at surface 
    input.x_offsets = {0.0};       // Center
    input.z_depths = {1.5};        // Simple depth
    
    // Layer properties (simple values like working tests)
    input.H_thicknesses = {1.5, 6.0};     // Simple thicknesses
    input.E_moduli = {500, 40, 10};       // Same as test_pymastic_port.cpp
    input.nu_poisson = {0.35, 0.35, 0.35};
    
    // Options
    input.bonded_interfaces = {1, 1};  // Bonded
    input.iterations = 40;
    
    std::cout << "\n========================================" << std::endl;
    std::cout << "C++ CALCULATION WITH DEBUG LOGGING" << std::endl;
    std::cout << "========================================" << std::endl;
    
    try {
        PyMasticSolver solver;
        
        // Enable detailed logging in PyMasticSolver
        std::cout << "\n[DEBUG] Calling PyMastic with:" << std::endl;
        std::cout << "  Input validation..." << std::endl;
        
        // Add input validation
        if (input.E_moduli.size() != input.nu_poisson.size()) {
            std::cerr << "ERROR: E and nu size mismatch" << std::endl;
            return;
        }
        if (input.H_thicknesses.size() != input.E_moduli.size() - 1) {
            std::cerr << "ERROR: Layer count mismatch" << std::endl;
            return;
        }
        
        std::cout << "  ✓ Input validation passed" << std::endl;
        std::cout << "  Layers: " << input.E_moduli.size() << std::endl;
        std::cout << "  Measurement points: x=" << input.x_offsets.size() << ", z=" << input.z_depths.size() << std::endl;
        
        // Call C++ PyMastic
        PyMasticSolver::Output result = solver.Compute(input);
        
        // Display results (convert back to metric)
        std::cout << "\n[RESULTS] C++ PyMastic:" << std::endl;
        std::cout << std::setprecision(6);
        
        // Convert US results back to metric
        const double INCH_TO_M = 1.0 / 39.3701;
        const double PSI_TO_MPA = 1.0 / 0.145038;
        
        double displacement_m = result.displacement_z(0,0) * INCH_TO_M;
        double stress_mpa = result.stress_z(0,0) * PSI_TO_MPA;
        
        std::cout << "  Displacement_Z(0,0): " << std::scientific << displacement_m << " m" << std::endl;
        std::cout << "  Stress_Z(0,0):       " << std::fixed << stress_mpa << " MPa" << std::endl;
        
        // Strain is dimensionless (no conversion needed)
        double strain_z = result.strain_z(0,0);
        double strain_micro = strain_z * 1e6;
        std::cout << "  Strain_Z[0]:         " << strain_micro << " μɛ" << std::endl;
        
        // Compare with expected
        double expected_strain = 711.6;  // Python validated
        double error = std::abs(strain_micro - expected_strain) / expected_strain * 100.0;
        
        std::cout << "\n[COMPARISON]:" << std::endl;
        std::cout << "  Expected (Python): " << expected_strain << " μɛ" << std::endl;
        std::cout << "  Actual (C++):      " << strain_micro << " μɛ" << std::endl;
        std::cout << "  Error:             " << error << "%" << std::endl;
        
        if (error < 1.0) {
            std::cout << "  ✅ SUCCESS: Error < 1%" << std::endl;
        } else if (error < 10.0) {
            std::cout << "  ⚠️  CLOSE: Error < 10% (likely unit issue)" << std::endl;
        } else {
            std::cout << "  ❌ MAJOR ERROR: " << error << "% (algorithm or unit problem)" << std::endl;
        }
        
        // Debug analysis
        std::cout << "\n[DEBUG ANALYSIS]:" << std::endl;
        
        // Check order of magnitude
        double magnitude_ratio = std::abs(strain_micro / expected_strain);
        if (magnitude_ratio > 1000) {
            std::cout << "  🔍 UNIT ERROR: Result " << magnitude_ratio << "× too large" << std::endl;
            std::cout << "     → Check unit conversions in C++ (psi, inches, ksi)" << std::endl;
        } else if (magnitude_ratio < 0.001) {
            std::cout << "  🔍 UNIT ERROR: Result " << (1.0/magnitude_ratio) << "× too small" << std::endl;
            std::cout << "     → Check unit conversions or scaling factors" << std::endl;
        }
        
        // Check sign
        if ((strain_micro > 0) != (expected_strain > 0)) {
            std::cout << "  🔍 SIGN ERROR: Expected " << (expected_strain > 0 ? "positive" : "negative") 
                      << " but got " << (strain_micro > 0 ? "positive" : "negative") << std::endl;
        }
        
    } catch (const std::exception& e) {
        std::cerr << "❌ C++ Exception: " << e.what() << std::endl;
    }
    
    std::cout << "\n[NEXT STEPS]:" << std::endl;
    std::cout << "1. If major error (>10%), check unit conversions in PyMasticSolver.cpp" << std::endl;
    std::cout << "2. Add printf debugging to intermediate calculations" << std::endl;
    std::cout << "3. Compare Hankel grid setup, Bessel values, boundary matrices" << std::endl;
    std::cout << "4. Verify E×1000 factor and other Python→C++ unit differences" << std::endl;
}

int main()
{
    test_pymastic_cpp_detailed();
    return 0;
}