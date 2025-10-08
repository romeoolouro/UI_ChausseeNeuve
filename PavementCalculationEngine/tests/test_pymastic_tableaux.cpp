/**
 * @brief PyMastic Tableaux Validation Test 
 * Task 2A.3.2: Validate PyMastic against academic Tableaux I.1 and I.5
 * 
 * Target: <0.5% error vs expected values
 * Priority: Academic validation over Python reference matching
 */
#include <iostream>
#include <iomanip>
#include <cmath>
#include "PyMasticSolver.h"

// Test results structure
struct ValidationResult {
    std::string test_name;
    double measured;
    double expected;
    double error_percent;
    bool passed;
    std::string units;
};

void print_result(const ValidationResult& result) {
    std::cout << std::fixed << std::setprecision(4);
    std::cout << "=== " << result.test_name << " ===\n";
    std::cout << "Measured:  " << result.measured << " " << result.units << "\n";
    std::cout << "Expected:  " << result.expected << " " << result.units << "\n";
    std::cout << "Error:     " << result.error_percent << "%\n";
    std::cout << "Status:    " << (result.passed ? "✅ PASS" : "❌ FAIL") << "\n\n";
}

ValidationResult test_tableau_i1_structure_souple() {
    std::cout << "🔬 Testing Tableau I.1: Structure Souple (Flexible Pavement)\n";
    std::cout << "Configuration: BBM(5500 MPa, 0.04m) / GNT(600 MPa, 0.15m) / PF2(50 MPa)\n";
    std::cout << "Expected: εz = 711.5 ± 4 μdef at z = 0.19m (base GNT)\n\n";
    
    PyMasticSolver::Input input;
    
    // Load configuration (convert to PyMastic units)
    input.q_kpa = 662.0;        // Pressure in kPa (PyMastic expects same units)
    input.a_m = 0.1125;        // Radius in m
    
    // Coordinates (single point at base of GNT layer)
    input.x_offsets = {0.0};   // Center of load (axle)
    input.z_depths = {0.19};   // Depth = 0.04 + 0.15 = 0.19m (base GNT)
    
    // Layer properties (3 layers: BBM/GNT/PF2)
    input.H_thicknesses = {0.04, 0.15};  // First 2 layers (semi-infinite last)
    input.E_moduli = {5500.0, 600.0, 50.0};  // Young's moduli in MPa
    input.nu_poisson = {0.35, 0.35, 0.35};   // Poisson ratios
    input.bonded_interfaces = {1, 1};    // All interfaces bonded
    
    // PyMastic parameters for high accuracy
    input.iterations = 50;
    input.ZRO = 1e-8;
    input.inverser = "solve";
    
    try {
        PyMasticSolver solver;
        auto output = solver.Compute(input);
        
        // Extract vertical strain at measurement point (convert to microstrain)
        double epsilon_z_measured = output.strain_z(0, 0) * 1e6;  // Convert to μdef
        double epsilon_z_expected = 711.5;  // μdef
        
        ValidationResult result;
        result.test_name = "Tableau I.1 - Vertical Strain (εz)";
        result.measured = epsilon_z_measured;
        result.expected = epsilon_z_expected;
        result.error_percent = std::abs(epsilon_z_measured - epsilon_z_expected) / epsilon_z_expected * 100.0;
        result.passed = (result.error_percent < 0.5);  // Target <0.5% error
        result.units = "μdef";
        
        return result;
        
    } catch (const std::exception& e) {
        std::cerr << "Error in Tableau I.1 test: " << e.what() << std::endl;
        
        ValidationResult result;
        result.test_name = "Tableau I.1 - COMPUTATION ERROR";
        result.measured = 0.0;
        result.expected = 711.5;
        result.error_percent = 100.0;
        result.passed = false;
        result.units = "μdef";
        return result;
    }
}

ValidationResult test_tableau_i5_semi_bonded() {
    std::cout << "🔬 Testing Tableau I.5: Semi-Rigide (Semi-Bonded)\n";
    std::cout << "Configuration: BBSG(7000 MPa, 0.06m) / GC-T3(23000 MPa, 0.15m) / PF3(120 MPa)\n";
    std::cout << "Expected: σt = 0.612 ± 0.003 MPa at z = 0.21m (base GC-T3, semi-bonded)\n\n";
    
    PyMasticSolver::Input input;
    
    // Load configuration
    input.q_kpa = 662.0;
    input.a_m = 0.1125;
    
    // Coordinates 
    input.x_offsets = {0.0};   // Center (twin wheel center)
    input.z_depths = {0.21};   // Base of GC-T3 layer = 0.06 + 0.15 = 0.21m
    
    // Layer properties (3 layers: BBSG/GC-T3/PF3)
    input.H_thicknesses = {0.06, 0.15};
    input.E_moduli = {7000.0, 23000.0, 120.0};
    input.nu_poisson = {0.35, 0.35, 0.35};
    input.bonded_interfaces = {1, 0};  // BBSG-GC bonded, GC-PF3 semi-bonded (frictionless)
    
    // High accuracy parameters
    input.iterations = 50;
    input.ZRO = 1e-8;
    input.inverser = "solve";
    
    try {
        PyMasticSolver solver;
        auto output = solver.Compute(input);
        
        // Extract tangential stress (convert MPa units)
        double sigma_t_measured = std::abs(output.stress_t(0, 0)) / 1000.0;  // kPa to MPa
        double sigma_t_expected = 0.612;  // MPa
        
        ValidationResult result;
        result.test_name = "Tableau I.5 Semi-Bonded - Tangential Stress (σt)";
        result.measured = sigma_t_measured;
        result.expected = sigma_t_expected;
        result.error_percent = std::abs(sigma_t_measured - sigma_t_expected) / sigma_t_expected * 100.0;
        result.passed = (result.error_percent < 0.5);
        result.units = "MPa";
        
        return result;
        
    } catch (const std::exception& e) {
        std::cerr << "Error in Tableau I.5 semi-bonded test: " << e.what() << std::endl;
        
        ValidationResult result;
        result.test_name = "Tableau I.5 Semi-Bonded - COMPUTATION ERROR";
        result.measured = 0.0;
        result.expected = 0.612;
        result.error_percent = 100.0;
        result.passed = false;
        result.units = "MPa";
        return result;
    }
}

ValidationResult test_tableau_i5_fully_bonded() {
    std::cout << "🔬 Testing Tableau I.5: Semi-Rigide (Fully Bonded)\n";
    std::cout << "Configuration: BBSG(7000 MPa, 0.06m) / GC-T3(23000 MPa, 0.15m) / PF3(120 MPa)\n";
    std::cout << "Expected: σt = 0.815 ± 0.003 MPa at z = 0.21m (base GC-T3, fully bonded)\n\n";
    
    PyMasticSolver::Input input;
    
    // Same as semi-bonded but with fully bonded interfaces
    input.q_kpa = 662.0;
    input.a_m = 0.1125;
    input.x_offsets = {0.0};
    input.z_depths = {0.21};
    input.H_thicknesses = {0.06, 0.15};
    input.E_moduli = {7000.0, 23000.0, 120.0};
    input.nu_poisson = {0.35, 0.35, 0.35};
    input.bonded_interfaces = {1, 1};  // All interfaces fully bonded
    
    input.iterations = 50;
    input.ZRO = 1e-8;
    input.inverser = "solve";
    
    try {
        PyMasticSolver solver;
        auto output = solver.Compute(input);
        
        double sigma_t_measured = std::abs(output.stress_t(0, 0)) / 1000.0;
        double sigma_t_expected = 0.815;
        
        ValidationResult result;
        result.test_name = "Tableau I.5 Fully Bonded - Tangential Stress (σt)";
        result.measured = sigma_t_measured;
        result.expected = sigma_t_expected;
        result.error_percent = std::abs(sigma_t_measured - sigma_t_expected) / sigma_t_expected * 100.0;
        result.passed = (result.error_percent < 0.5);
        result.units = "MPa";
        
        return result;
        
    } catch (const std::exception& e) {
        std::cerr << "Error in Tableau I.5 fully bonded test: " << e.what() << std::endl;
        
        ValidationResult result;
        result.test_name = "Tableau I.5 Fully Bonded - COMPUTATION ERROR";
        result.measured = 0.0;
        result.expected = 0.815;
        result.error_percent = 100.0;
        result.passed = false;
        result.units = "MPa";
        return result;
    }
}

int main() {
    std::cout << "PyMastic Tableaux Academic Validation\n";
    std::cout << "=====================================\n\n";
    std::cout << "Target: <0.5% error for academic validation\n";
    std::cout << "Priority: Tableaux accuracy over Python reference matching\n\n";
    
    std::vector<ValidationResult> results;
    
    // Run all validation tests
    results.push_back(test_tableau_i1_structure_souple());
    results.push_back(test_tableau_i5_semi_bonded());
    results.push_back(test_tableau_i5_fully_bonded());
    
    // Print all results
    std::cout << "\n" << std::string(60, '=') << "\n";
    std::cout << "VALIDATION SUMMARY\n";
    std::cout << std::string(60, '=') << "\n\n";
    
    int passed = 0;
    for (const auto& result : results) {
        print_result(result);
        if (result.passed) passed++;
    }
    
    std::cout << "Overall Results: " << passed << "/" << results.size() << " tests passed\n";
    
    if (passed == results.size()) {
        std::cout << "🎉 ALL TABLEAUX VALIDATION TESTS PASSED!\n";
        std::cout << "PyMastic implementation meets academic accuracy requirements (<0.5%)\n";
        return 0;
    } else {
        std::cout << "⚠️  TABLEAUX VALIDATION INCOMPLETE\n";
        std::cout << "PyMastic needs calibration to meet academic requirements\n";
        return 1;
    }
}