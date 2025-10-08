/**
 * @file debug_pymastic_simple.cpp
 * @brief Debug PyMastic with simple test case from Python Test.py
 */

#include <iostream>
#include <vector>
#include "../include/PyMasticSolver.h"

int main() {
    std::cout << "=== PyMastic Simple Debug Test ===" << std::endl;
    std::cout << "Using exact parameters from Python Test.py" << std::endl << std::endl;
    
    // Exact parameters from Python Test.py
    PyMasticSolver::Input input;
    input.q_kpa = 100.0;           // psi (despite variable name)
    input.a_m = 5.99;              // inches (despite variable name)
    input.x_offsets = {0.0, 8.0};
    input.z_depths = {0.0, 9.99, 10.01};
    input.H_thicknesses = {10.0, 6.0};  // inches
    input.E_moduli = {500.0, 40.0, 10.0};  // ksi
    input.nu_poisson = {0.35, 0.4, 0.45};
    input.bonded_interfaces = {0, 0};  // Unbonded
    input.iterations = 10;
    input.ZRO = 7e-7;
    input.inverser = "solve";
    
    std::cout << "Input validation: " << (input.Validate() ? "PASS" : "FAIL") << std::endl;
    
    if (!input.Validate()) {
        std::cerr << "Input validation failed!" << std::endl;
        return 1;
    }
    
    try {
        PyMasticSolver solver;
        std::cout << "Computing..." << std::endl;
        auto output = solver.Compute(input);
        
        std::cout << "\nResults:" << std::endl;
        std::cout << "Displacement_Z[0,0]: " << output.displacement_z(0, 0) << " inches" << std::endl;
        std::cout << "Stress_Z[0,0]: " << output.stress_z(0, 0) << " psi" << std::endl;
        std::cout << "Displacement_H[0,0]: " << output.displacement_h(0, 0) << " inches" << std::endl;
        std::cout << "Stress_T[0,0]: " << output.stress_t(0, 0) << " psi" << std::endl;
        
        std::cout << "\nStrain_Z[0,0]: " << output.strain_z(0, 0) << " (dimensionless)" << std::endl;
        std::cout << "Strain_R[0,0]: " << output.strain_r(0, 0) << " (dimensionless)" << std::endl;
        std::cout << "Strain_T[0,0]: " << output.strain_t(0, 0) << " (dimensionless)" << std::endl;
        
        // Check for NaN/Inf
        if (std::isnan(output.displacement_z(0, 0)) || std::isinf(output.displacement_z(0, 0))) {
            std::cerr << "\n⚠ WARNING: NaN or Inf detected in displacement!" << std::endl;
        }
        if (std::isnan(output.strain_z(0, 0)) || std::isinf(output.strain_z(0, 0))) {
            std::cerr << "\n⚠ WARNING: NaN or Inf detected in strain!" << std::endl;
        }
        
    } catch (const std::exception& e) {
        std::cerr << "Exception: " << e.what() << std::endl;
        return 1;
    }
    
    return 0;
}
