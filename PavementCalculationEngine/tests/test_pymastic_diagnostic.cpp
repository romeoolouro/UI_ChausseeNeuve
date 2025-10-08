/**
 * @brief Diagnostic PyMastic C++ test with intermediate outputs
 * Task 2A.3.2: Detailed comparison vs Python
 */
#include <iostream>
#include <iomanip>
#include "PyMasticSolver.h"

int main() {
    std::cout << "PyMastic C++ Diagnostic Test\n";
    std::cout << "============================\n\n";
    
    try {
        // Exact same parameters as Python Test.py
        PyMasticSolver::Input input;
        input.q_kpa = 100.0;  // lb (note: Python uses this directly)
        input.a_m = 5.99;     // inch  
        input.x_offsets = {0, 8};
        input.z_depths = {0, 9.99, 10.01};
        input.H_thicknesses = {10, 6}; // inch
        input.E_moduli = {500, 40, 10}; // ksi
        input.nu_poisson = {0.35, 0.4, 0.45};
        input.bonded_interfaces = {0, 0}; // Frictionless
        input.iterations = 40; // Increase for better convergence  
        input.ZRO = 7e-7;     // Same as Python
        input.inverser = "solve"; // Same as Python
        
        std::cout << std::fixed << std::setprecision(10);
        
        std::cout << "Input Parameters (matching Python exactly):\n";
        std::cout << "==========================================\n";
        std::cout << "q = " << input.q_kpa << " lb\n";
        std::cout << "a = " << input.a_m << " inch\n";
        std::cout << "x = [" << input.x_offsets[0] << ", " << input.x_offsets[1] << "]\n";
        std::cout << "z = [" << input.z_depths[0] << ", " << input.z_depths[1] << ", " << input.z_depths[2] << "]\n";
        std::cout << "H = [" << input.H_thicknesses[0] << ", " << input.H_thicknesses[1] << "]\n";
        std::cout << "E = [" << input.E_moduli[0] << ", " << input.E_moduli[1] << ", " << input.E_moduli[2] << "]\n";
        std::cout << "nu = [" << input.nu_poisson[0] << ", " << input.nu_poisson[1] << ", " << input.nu_poisson[2] << "]\n";
        std::cout << "ZRO = " << input.ZRO << "\n";
        std::cout << "isBounded = [" << input.bonded_interfaces[0] << ", " << input.bonded_interfaces[1] << "]\n";
        std::cout << "iterations = " << input.iterations << "\n";
        std::cout << "inverser = " << input.inverser << "\n\n";
        
        PyMasticSolver solver;
        std::cout << "Computing responses...\n";
        auto output = solver.Compute(input);
        
        std::cout << "\nC++ Results:\n";
        std::cout << "============\n";
        std::cout << "Displacement Z [0,0]: " << output.displacement_z(0, 0) << "\n";
        std::cout << "Stress Z [0,0]:       " << output.stress_z(0, 0) << "\n";
        std::cout << "Displacement H [0,0]: " << output.displacement_h(0, 0) << "\n";
        std::cout << "Stress T [0,0]:       " << output.stress_t(0, 0) << "\n\n";
        
        std::cout << "Displacement Z [1,0]: " << output.displacement_z(1, 0) << "\n";
        std::cout << "Stress Z [1,0]:       " << output.stress_z(1, 0) << "\n";
        std::cout << "Stress R [1,0]:       " << output.stress_r(1, 0) << "\n";
        std::cout << "Stress T [1,0]:       " << output.stress_t(1, 0) << "\n\n";
        
        std::cout << "\nExpected Python Results (from Test.py output):\n";
        std::cout << "===========================================\n";
        std::cout << "Displacement Z [0,0]: 3003.3446530601486\n";
        std::cout << "Stress Z [0,0]:       12991015.02196602\n";
        std::cout << "Displacement H [0,0]: 0.0002949773663423231\n";
        std::cout << "Stress T [0,0]:       -219910504.4822657\n\n";
        
        std::cout << "Displacement Z [1,0]: 2.9498312245830136\n";
        std::cout << "Stress Z [1,0]:       -6.132041758174443\n";
        std::cout << "Stress R [1,0]:       -210677168.35167006\n";
        std::cout << "Stress T [1,0]:       -210677168.35167137\n\n";
        
        std::cout << "\nError Analysis:\n";
        std::cout << "==============\n";
        double rel_error_disp_z = std::abs(output.displacement_z(0, 0) - 3003.3446530601486) / 3003.3446530601486 * 100.0;
        double rel_error_stress_z = std::abs(output.stress_z(0, 0) - 12991015.02196602) / 12991015.02196602 * 100.0;
        
        std::cout << "Displacement Z [0,0] relative error: " << rel_error_disp_z << "%\n";
        std::cout << "Stress Z [0,0] relative error: " << rel_error_stress_z << "%\n";
        
        if (rel_error_disp_z > 1.0 || rel_error_stress_z > 1.0) {
            std::cout << "\n*** SIGNIFICANT DIFFERENCE DETECTED ***\n";
            std::cout << "This suggests a fundamental implementation difference.\n";
            std::cout << "Possible causes:\n";
            std::cout << "1. Units conversion (lb vs kPa, inch vs m)\n";
            std::cout << "2. Algorithm interpretation difference\n";
            std::cout << "3. Bessel function implementation\n";
            std::cout << "4. Matrix operation differences\n";
            return 1;
        } else {
            std::cout << "\n*** VALIDATION SUCCESSFUL ***\n";
            return 0;
        }
        
    } catch (const std::exception& e) {
        std::cout << "ERROR: " << e.what() << "\n";
        return 1;
    } catch (...) {
        std::cout << "UNKNOWN ERROR occurred\n";
        return 1;
    }
}