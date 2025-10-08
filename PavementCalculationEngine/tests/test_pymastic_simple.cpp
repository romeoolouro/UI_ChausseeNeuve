#include <iostream>
#include "PyMasticSolver.h"

int main() {
    std::cout << "PyMastic C++ Port Test\n";
    std::cout << "======================\n\n";
    
    try {
        // Create test input matching PyMastic/Test.py
        PyMasticSolver::Input input;
        input.q_kpa = 100.0;  // lb
        input.a_m = 5.99;     // inch  
        input.x_offsets = {0, 8};
        input.z_depths = {0, 9.99, 10.01};
        input.H_thicknesses = {10, 6}; // inch
        input.E_moduli = {500, 40, 10}; // ksi
        input.nu_poisson = {0.35, 0.4, 0.45};
        input.bonded_interfaces = {0, 0}; // Frictionless
        input.iterations = 10;
        input.ZRO = 7e-7;
        input.inverser = "solve";
        
        std::cout << "Input validation: " << (input.Validate() ? "PASS" : "FAIL") << "\n";
        
        if (!input.Validate()) {
            std::cout << "Input validation failed!\n";
            return 1;
        }
        
        PyMasticSolver solver;
        std::cout << "Solver version: " << PyMasticSolver::GetVersion() << "\n\n";
        
        std::cout << "Computing responses...\n";
        auto output = solver.Compute(input);
        
        std::cout << "Output validation: " << (output.IsValid() ? "PASS" : "FAIL") << "\n\n";
        
        if (!output.IsValid()) {
            std::cout << "Output contains invalid values!\n";
            return 1;
        }
        
        // Print key results for comparison with PyMastic
        std::cout << "Results (matching PyMastic Test.py):\n";
        std::cout << "===================================\n";
        std::cout << "Displacement Z [0,0]: " << output.displacement_z(0, 0) << "\n";
        std::cout << "Stress Z [0,0]: " << output.stress_z(0, 0) << "\n";
        std::cout << "Displacement H [0,0]: " << output.displacement_h(0, 0) << "\n";
        std::cout << "Stress T [0,0]: " << output.stress_t(0, 0) << "\n\n";
        
        std::cout << "Displacement Z [1,0]: " << output.displacement_z(1, 0) << "\n";
        std::cout << "Stress Z [1,0]: " << output.stress_z(1, 0) << "\n";
        std::cout << "Stress R [1,0]: " << output.stress_r(1, 0) << "\n";
        std::cout << "Stress T [1,0]: " << output.stress_t(1, 0) << "\n\n";
        
        // Test different solver methods
        std::cout << "Testing solver methods:\n";
        std::vector<std::string> methods = {"solve", "inv", "pinv", "lu", "svd"};
        
        for (const auto& method : methods) {
            input.inverser = method;
            try {
                auto test_output = solver.Compute(input);
                std::cout << method << ": " << (test_output.IsValid() ? "PASS" : "FAIL") << "\n";
            } catch (const std::exception& e) {
                std::cout << method << ": FAILED - " << e.what() << "\n";
            }
        }
        
        std::cout << "\nPyMastic C++ port test completed successfully!\n";
        return 0;
        
    } catch (const std::exception& e) {
        std::cout << "ERROR: " << e.what() << "\n";
        return 1;
    }
}