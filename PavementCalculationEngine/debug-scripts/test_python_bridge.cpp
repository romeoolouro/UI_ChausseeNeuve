#include "PyMasticPythonBridge.h"
#include <iostream>
#include <iomanip>

int main() {
    std::cout << "Testing PyMastic Python Bridge Integration" << std::endl;
    std::cout << "=========================================" << std::endl;
    
    // Validated Tableau I.1 parameters
    PyMasticPythonBridge::Input input;
    input.q_kpa = 667.0;
    input.a_m = 0.1125;
    input.z_depths_m = {0.04};  // Interface BBM/GNT
    input.H_thicknesses_m = {0.04, 0.15};
    input.E_moduli_mpa = {5500, 600, 50};
    input.nu_poisson = {0.35, 0.35, 0.35};
    input.bonded_interfaces = {1, 1};  // Bonded
    
    std::cout << "\nInput parameters (Tableau I.1 validated):" << std::endl;
    std::cout << "  q = " << input.q_kpa << " kPa" << std::endl;
    std::cout << "  a = " << input.a_m << " m" << std::endl;
    std::cout << "  z = " << input.z_depths_m[0] << " m (interface BBM/GNT)" << std::endl;
    std::cout << "  Expected: εz ≈ 711.6 μɛ" << std::endl;
    
    // Call Python bridge
    std::cout << "\nCalling PyMastic Python Bridge..." << std::endl;
    auto result = PyMasticPythonBridge::Calculate(input);
    
    // Display results
    std::cout << "\nResults:" << std::endl;
    std::cout << "  Success: " << (result.success ? "YES" : "NO") << std::endl;
    
    if (result.success && !result.strain_z_microdef.empty()) {
        std::cout << std::fixed << std::setprecision(1);
        std::cout << "  Strain Z: " << result.strain_z_microdef[0] << " μɛ" << std::endl;
        std::cout << "  Displacement Z: " << std::scientific << result.displacement_z_m[0] << " m" << std::endl;
        std::cout << "  Stress Z: " << std::fixed << result.stress_z_mpa[0] << " MPa" << std::endl;
        
        double expected = 711.6;
        double error = std::abs(result.strain_z_microdef[0] - expected) / expected * 100.0;
        std::cout << "  Error vs expected: " << error << "%" << std::endl;
        
        if (error < 1.0) {
            std::cout << "  ✅ SUCCESS: Python bridge working correctly!" << std::endl;
            return 0;
        } else {
            std::cout << "  ❌ ERROR: Results don't match expected values" << std::endl;
            return 1;
        }
    } else {
        std::cout << "  Error: " << result.error_message << std::endl;
        return 1;
    }
}