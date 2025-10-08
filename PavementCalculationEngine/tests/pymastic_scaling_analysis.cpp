/**
 * @brief PyMastic Diagnostic Test for Scaling Analysis
 * Task 2A.3.2: Identify exact scaling factors to fix Tableaux validation
 * 
 * Analysis target: 
 * - Tableau I.1: ~6600x error in strain (measured 4.7M vs expected 711.5 μdef)
 * - Tableau I.5: ~6x error in stress (measured 0.095 vs expected 0.612 MPa)
 */
#include <iostream>
#include <iomanip>
#include <cmath>
#include <vector>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

void analyze_tableau_i1_scaling() {
    std::cout << "🔍 TABLEAU I.1 SCALING ANALYSIS\n";
    std::cout << "================================\n\n";
    
    // Expected configuration for Tableau I.1
    double q_kpa = 662.0;           // Pressure
    double a_m = 0.1125;            // Radius  
    double z_depth = 0.19;          // Measurement depth
    std::vector<double> H_layers = {0.04, 0.15};          // Layer thicknesses
    std::vector<double> E_moduli = {5500.0, 600.0, 50.0}; // Young's moduli (MPa)
    std::vector<double> nu_ratios = {0.35, 0.35, 0.35};   // Poisson ratios
    
    // Calculate key parameters as in PyMastic
    double sumH = H_layers[0] + H_layers[1]; // = 0.19 m
    double alpha = a_m / sumH;               // = 0.1125 / 0.19 = 0.5921
    double L = z_depth / sumH;               // = 0.19 / 0.19 = 1.0
    
    // Expected result
    double expected_strain_microdef = 711.5;
    double expected_strain = expected_strain_microdef * 1e-6; // Convert to absolute strain
    
    std::cout << std::fixed << std::setprecision(6);
    std::cout << "Input Configuration:\n";
    std::cout << "  q = " << q_kpa << " kPa\n";
    std::cout << "  a = " << a_m << " m\n"; 
    std::cout << "  z = " << z_depth << " m\n";
    std::cout << "  H = [" << H_layers[0] << ", " << H_layers[1] << "] m\n";
    std::cout << "  E = [" << E_moduli[0] << ", " << E_moduli[1] << ", " << E_moduli[2] << "] MPa\n\n";
    
    std::cout << "PyMastic Normalized Parameters:\n";
    std::cout << "  sumH = " << sumH << " m\n";
    std::cout << "  alpha = a/sumH = " << alpha << "\n";
    std::cout << "  L = z/sumH = " << L << "\n\n";
    
    std::cout << "Expected Result:\n";
    std::cout << "  εz = " << expected_strain_microdef << " μdef\n";
    std::cout << "  εz = " << expected_strain << " (absolute)\n\n";
    
    // Analyze scaling factors needed
    std::cout << "Scaling Analysis:\n";
    std::cout << "  Current C++ result: ~4,700,000 μdef (from simple test)\n";
    std::cout << "  Expected result:          711.5 μdef\n";
    std::cout << "  Error factor: ~6600x\n\n";
    
    std::cout << "Potential scaling issues to investigate:\n";
    std::cout << "  1. Units: kPa vs Pa (factor 1000)\n";
    std::cout << "  2. Hankel integration weights (summing vs integrating)\n";  
    std::cout << "  3. Bessel function normalization\n";
    std::cout << "  4. sumH normalization missing somewhere\n";
    std::cout << "  5. Response coefficient matrix scaling\n\n";
    
    // Check if it's a simple unit conversion issue
    double corrected_by_1000 = 4700000.0 / 1000.0; // = 4700 μdef
    double corrected_by_6600 = 4700000.0 / 6600.0;  // = 712 μdef ← Very close!
    
    std::cout << "Quick scaling tests:\n";
    std::cout << "  Divide by 1000 (kPa→Pa): " << corrected_by_1000 << " μdef (still ~6.6x error)\n";
    std::cout << "  Divide by 6600:          " << corrected_by_6600 << " μdef (≈ expected!)\n\n";
    
    std::cout << "🎯 Conclusion: Need to find where the ~6600x factor comes from!\n";
    std::cout << "Likely candidate: Hankel integration or sumH^n scaling.\n\n";
}

void analyze_tableau_i5_scaling() {
    std::cout << "🔍 TABLEAU I.5 SCALING ANALYSIS\n";
    std::cout << "================================\n\n";
    
    // Expected configuration for Tableau I.5
    double q_kpa = 662.0;
    double a_m = 0.1125;
    double z_depth = 0.21;
    std::vector<double> H_layers = {0.06, 0.15};
    std::vector<double> E_moduli = {7000.0, 23000.0, 120.0};
    
    double sumH = H_layers[0] + H_layers[1]; // = 0.21 m
    double alpha = a_m / sumH;
    double L = z_depth / sumH; // = 1.0
    
    double expected_stress_mpa = 0.612;      // Semi-bonded case
    double expected_stress_kpa = expected_stress_mpa * 1000.0; // = 612 kPa
    
    std::cout << std::fixed << std::setprecision(6);
    std::cout << "Input Configuration:\n";
    std::cout << "  q = " << q_kpa << " kPa\n";
    std::cout << "  a = " << a_m << " m\n";
    std::cout << "  z = " << z_depth << " m (base of GC-T3)\n";
    std::cout << "  H = [" << H_layers[0] << ", " << H_layers[1] << "] m\n";
    std::cout << "  E = [" << E_moduli[0] << ", " << E_moduli[1] << ", " << E_moduli[2] << "] MPa\n\n";
    
    std::cout << "PyMastic Normalized Parameters:\n";
    std::cout << "  sumH = " << sumH << " m\n";
    std::cout << "  alpha = " << alpha << "\n";
    std::cout << "  L = " << L << "\n\n";
    
    std::cout << "Expected Result:\n";
    std::cout << "  σt = " << expected_stress_mpa << " MPa\n";
    std::cout << "  σt = " << expected_stress_kpa << " kPa\n\n";
    
    std::cout << "Scaling Analysis:\n";
    std::cout << "  Current C++ result: ~0.095 MPa = 95 kPa (from simple test)\n";
    std::cout << "  Expected result:    0.612 MPa = 612 kPa\n";
    std::cout << "  Error factor: ~6.4x too small\n\n";
    
    std::cout << "Stress scaling is less severe than strain scaling.\n";
    std::cout << "This suggests different error sources:\n";
    std::cout << "  - Strain: Integration/summation error (~6600x)\n";
    std::cout << "  - Stress: Coefficient or sign error (~6x)\n\n";
    
    double corrected_stress = 0.095 * 6.44; // ≈ 0.612 MPa
    std::cout << "Quick test: 0.095 × 6.44 = " << corrected_stress << " MPa (≈ expected)\n\n";
    
    std::cout << "🎯 Stress error is more manageable - likely coefficient issue.\n\n";
}

void recommend_fix_strategy() {
    std::cout << "🛠️  RECOMMENDED FIX STRATEGY\n";
    std::cout << "============================\n\n";
    
    std::cout << "Priority 1: Fix strain calculation (~6600x error)\n";
    std::cout << "  Actions:\n";
    std::cout << "  1. Compare PyMastic C++ Hankel integration vs Python exactly\n";
    std::cout << "  2. Check if integration weights (ft_weights) are correct\n";
    std::cout << "  3. Verify sumH usage in displacement calculation\n";
    std::cout << "  4. Check Bessel J1 function scaling and arguments\n\n";
    
    std::cout << "Priority 2: Fix stress calculation (~6x error) \n";
    std::cout << "  Actions:\n";
    std::cout << "  1. Verify stress response matrix calculations\n";
    std::cout << "  2. Check sign conventions and coefficient matrices\n";
    std::cout << "  3. Compare stress integration method with Python\n\n";
    
    std::cout << "Debugging approach:\n";
    std::cout << "  1. Add detailed logging to PyMasticSolver.cpp\n";
    std::cout << "  2. Print intermediate values: m_values, ft_weights, Rs, Bessel values\n";
    std::cout << "  3. Compare step-by-step with Python PyMastic calculation\n";
    std::cout << "  4. Run single-point calculation with known inputs\n\n";
    
    std::cout << "Success criteria:\n";
    std::cout << "  - Tableau I.1: εz within ±4 μdef of 711.5 μdef (<0.6% error)\n";
    std::cout << "  - Tableau I.5: σt within ±0.003 MPa of 0.612 MPa (<0.5% error)\n\n";
}

int main() {
    std::cout << "PyMastic Scaling Diagnostic Analysis\n";
    std::cout << "====================================\n\n";
    std::cout << "Purpose: Identify exact scaling factors for Tableaux validation\n";
    std::cout << "Based on: Simple test results showing systematic scaling errors\n\n";
    
    analyze_tableau_i1_scaling();
    analyze_tableau_i5_scaling();
    recommend_fix_strategy();
    
    std::cout << "Next Step: Implement detailed debugging in PyMasticSolver.cpp\n";
    std::cout << "           to identify exact source of scaling factors.\n\n";
    
    return 0;
}