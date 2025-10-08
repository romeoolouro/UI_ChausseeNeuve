/**
 * @brief Simple PyMastic Tableaux Validation (Standalone)
 * Task 2A.3.2: Validate PyMastic against academic Tableaux I.1 and I.5
 * Standalone version without DLL dependencies for quick testing
 */
#include <iostream>
#include <iomanip>
#include <cmath>
#include <vector>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

// Simple manual implementation of critical PyMastic functions for validation
struct SimpleResult {
    double displacement_z;
    double stress_z;
    double stress_r; 
    double stress_t;
    double strain_z;
    double strain_r;
    double strain_t;
};

// Manual Bessel J0 approximation (sufficient for this test)
double manual_bessel_j0(double x) {
    if (std::abs(x) < 1e-8) return 1.0;
    
    // Series expansion for small x
    if (std::abs(x) < 3.0) {
        double term = 1.0;
        double sum = 1.0;
        double x2 = x * x / 4.0;
        
        for (int n = 1; n <= 15; ++n) {
            term *= -x2 / (n * n);
            sum += term;
            if (std::abs(term) < 1e-15) break;
        }
        return sum;
    }
    
    // Asymptotic expansion for large x
    double sqrt_2_pi_x = std::sqrt(2.0 / (M_PI * std::abs(x)));
    double phase = std::abs(x) - M_PI / 4.0;
    return sqrt_2_pi_x * std::cos(phase);
}

// Manual Bessel J1 approximation
double manual_bessel_j1(double x) {
    if (std::abs(x) < 1e-8) return 0.0;
    
    if (std::abs(x) < 3.0) {
        double term = x / 2.0;
        double sum = term;
        double x2 = x * x / 4.0;
        
        for (int n = 1; n <= 15; ++n) {
            term *= -x2 / (n * (n + 1));
            sum += term;
            if (std::abs(term) < 1e-15) break;
        }
        return sum;
    }
    
    double sqrt_2_pi_x = std::sqrt(2.0 / (M_PI * std::abs(x)));
    double phase = std::abs(x) - 3.0 * M_PI / 4.0;
    return x > 0 ? sqrt_2_pi_x * std::cos(phase) : -sqrt_2_pi_x * std::cos(phase);
}

// Simplified PyMastic calculation for Tableaux validation
SimpleResult compute_simple_pymastic(double q_kpa, double a_m, double z_depth,
                                    const std::vector<double>& H_layers,
                                    const std::vector<double>& E_moduli,
                                    const std::vector<double>& nu_ratios,
                                    const std::vector<int>& bonded_interfaces) {
    
    SimpleResult result = {0};
    
    // Critical insight: PyMastic uses specific integration and scaling
    // For now, use simplified Boussinesq-based approximation with corrections
    
    double total_depth = z_depth;
    double effective_E = E_moduli[0];  // Surface layer modulus
    double nu = nu_ratios[0];
    
    // Multi-layer effect: reduce effective modulus based on layer contrast
    for (size_t i = 0; i < E_moduli.size(); ++i) {
        if (E_moduli[i] < effective_E) {
            effective_E = std::min(effective_E, E_moduli[i] * 2.0);  // Stiffening factor
        }
    }
    
    // Load parameters
    double P = q_kpa * M_PI * a_m * a_m;  // Total load
    double r = 0.0;  // Center of load
    
    // Boussinesq approximations with multi-layer corrections
    double R = std::sqrt(r*r + z_depth*z_depth);
    double z_factor = z_depth / (R * R * R);
    
    // Vertical displacement (Boussinesq)
    result.displacement_z = P * (1.0 + nu) / (2.0 * M_PI * effective_E * R);
    
    // Stresses
    result.stress_z = 3.0 * P * z_depth*z_depth*z_depth / (2.0 * M_PI * R*R*R*R*R);
    result.stress_r = P / (2.0 * M_PI * R*R) - result.stress_z;
    result.stress_t = result.stress_r * 0.5;  // Approximation
    
    // Strains (from stress-strain relations)
    result.strain_z = result.stress_z / effective_E - nu * (result.stress_r + result.stress_t) / effective_E;
    result.strain_r = result.stress_r / effective_E - nu * (result.stress_z + result.stress_t) / effective_E;
    result.strain_t = result.stress_t / effective_E - nu * (result.stress_z + result.stress_r) / effective_E;
    
    return result;
}

void test_tableau_i1_simple() {
    std::cout << "🔬 Tableau I.1 Simple Test: Structure Souple\n";
    std::cout << "Expected: εz = 711.5 ± 4 μdef at z = 0.19m\n\n";
    
    // Input parameters
    double q_kpa = 662.0;
    double a_m = 0.1125;
    double z_depth = 0.19;
    std::vector<double> H_layers = {0.04, 0.15};
    std::vector<double> E_moduli = {5500.0, 600.0, 50.0};
    std::vector<double> nu_ratios = {0.35, 0.35, 0.35};
    std::vector<int> bonded = {1, 1};
    
    SimpleResult result = compute_simple_pymastic(q_kpa, a_m, z_depth, H_layers, E_moduli, nu_ratios, bonded);
    
    // Convert strain to microstrain
    double strain_z_microdef = result.strain_z * 1e6;
    double expected = 711.5;
    double error_percent = std::abs(strain_z_microdef - expected) / expected * 100.0;
    
    std::cout << std::fixed << std::setprecision(2);
    std::cout << "Measured εz: " << strain_z_microdef << " μdef\n";
    std::cout << "Expected εz: " << expected << " μdef\n";
    std::cout << "Error: " << error_percent << "%\n";
    std::cout << "Status: " << (error_percent < 50.0 ? "Reasonable range" : "Needs calibration") << "\n\n";
}

void test_tableau_i5_simple() {
    std::cout << "🔬 Tableau I.5 Simple Test: Semi-Rigide\n";
    std::cout << "Expected: σt = 0.612 MPa (semi-bonded) at z = 0.21m\n\n";
    
    // Input parameters
    double q_kpa = 662.0;
    double a_m = 0.1125;
    double z_depth = 0.21;
    std::vector<double> H_layers = {0.06, 0.15};
    std::vector<double> E_moduli = {7000.0, 23000.0, 120.0};
    std::vector<double> nu_ratios = {0.35, 0.35, 0.35};
    std::vector<int> bonded = {1, 0};  // Semi-bonded
    
    SimpleResult result = compute_simple_pymastic(q_kpa, a_m, z_depth, H_layers, E_moduli, nu_ratios, bonded);
    
    // Convert stress to MPa
    double stress_t_mpa = std::abs(result.stress_t) / 1000.0;
    double expected = 0.612;
    double error_percent = std::abs(stress_t_mpa - expected) / expected * 100.0;
    
    std::cout << std::fixed << std::setprecision(3);
    std::cout << "Measured σt: " << stress_t_mpa << " MPa\n";
    std::cout << "Expected σt: " << expected << " MPa\n";
    std::cout << "Error: " << error_percent << "%\n";
    std::cout << "Status: " << (error_percent < 50.0 ? "Reasonable range" : "Needs calibration") << "\n\n";
}

int main() {
    std::cout << "PyMastic Tableaux Simple Validation\n";
    std::cout << "====================================\n\n";
    
    std::cout << "NOTE: This is a simplified test to identify scaling issues.\n";
    std::cout << "Full PyMastic implementation needed for <0.5% accuracy.\n\n";
    
    test_tableau_i1_simple();
    test_tableau_i5_simple();
    
    std::cout << "Analysis:\n";
    std::cout << "- If errors are ~1000%, scaling/units problem\n";
    std::cout << "- If errors are ~50-100%, algorithm approximation issue\n";
    std::cout << "- If errors are <10%, close to target - need fine-tuning\n\n";
    
    return 0;
}