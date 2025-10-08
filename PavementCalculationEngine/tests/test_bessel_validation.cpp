/**
 * @brief Bessel function validation test against Python scipy
 */
#include <iostream>
#include <iomanip>
#include "PyMasticSolver.h"

int main() {
    std::cout << std::fixed << std::setprecision(10);
    std::cout << "Bessel Function Validation\n";
    std::cout << "=========================\n\n";
    
    PyMasticSolver solver;
    
    // Test values matching what Python would use
    std::vector<double> test_values = {0.1, 0.5, 1.0, 2.0, 5.0, 10.0};
    
    std::cout << "Testing Bessel J0 function:\n";
    std::cout << "x\t\tC++ J0(x)\tExpected (scipy)\n";
    std::cout << "-------------------------------------------\n";
    for (double x : test_values) {
        double j0_result = solver.BesselJ0(x);
        std::cout << x << "\t\t" << j0_result << "\t\t";
        
        // Expected values from scipy.special.jv(0, x)
        if (std::abs(x - 0.1) < 1e-6) std::cout << "0.9975031225" << std::endl;
        else if (std::abs(x - 0.5) < 1e-6) std::cout << "0.9384698073" << std::endl;
        else if (std::abs(x - 1.0) < 1e-6) std::cout << "0.7651976866" << std::endl;
        else if (std::abs(x - 2.0) < 1e-6) std::cout << "0.2238907791" << std::endl;
        else if (std::abs(x - 5.0) < 1e-6) std::cout << "-0.1775968073" << std::endl;
        else if (std::abs(x - 10.0) < 1e-6) std::cout << "-0.2459357645" << std::endl;
        else std::cout << "unknown" << std::endl;
    }
    
    std::cout << "\nTesting Bessel J1 function:\n";
    std::cout << "x\t\tC++ J1(x)\tExpected (scipy)\n";
    std::cout << "-------------------------------------------\n";
    for (double x : test_values) {
        double j1_result = solver.BesselJ1(x);
        std::cout << x << "\t\t" << j1_result << "\t\t";
        
        // Expected values from scipy.special.jv(1, x)
        if (std::abs(x - 0.1) < 1e-6) std::cout << "0.0499375260" << std::endl;
        else if (std::abs(x - 0.5) < 1e-6) std::cout << "0.2422684576" << std::endl;
        else if (std::abs(x - 1.0) < 1e-6) std::cout << "0.4400505857" << std::endl;
        else if (std::abs(x - 2.0) < 1e-6) std::cout << "0.5767248078" << std::endl;
        else if (std::abs(x - 5.0) < 1e-6) std::cout << "-0.3275791376" << std::endl;
        else if (std::abs(x - 10.0) < 1e-6) std::cout << "0.0434727462" << std::endl;
        else std::cout << "unknown" << std::endl;
    }
    
    // Test specific values from PyMastic test case
    std::cout << "\n\nPyMastic test case values:\n";
    std::cout << "=========================\n";
    double sumH = 16.0; // H=[10,6]
    double alpha = 5.99 / sumH;
    double m = 0.1; // Sample m value
    
    std::cout << "alpha = " << alpha << std::endl;
    std::cout << "m = " << m << std::endl;
    std::cout << "m * alpha = " << m * alpha << std::endl;
    
    double j0_test = solver.BesselJ0(m * alpha);
    double j1_test = solver.BesselJ1(m * alpha);
    
    std::cout << "J0(m * alpha) = " << j0_test << std::endl;
    std::cout << "J1(m * alpha) = " << j1_test << std::endl;
    
    return 0;
}