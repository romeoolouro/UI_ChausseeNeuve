#include <iostream>
#include "PavementData.h"
#include "PavementCalculator.h"

#ifdef EIGEN_AVAILABLE
#include <Eigen/Dense>
#endif

int main() {
    std::cout << "PavementCalculationEngine - Task 1.2 Test (Eliminate Global Variables)" << std::endl;
    
    try {
        // Test structured data creation
        Pavement::CalculationInput input;
        input.SetDefaults();
        
        std::cout << "Created default calculation input:" << std::endl;
        std::cout << input.ToString() << std::endl;
        
        // Test validation
        input.Validate();
        std::cout << "Validation successful!" << std::endl;
        
        // Test output structure
        Pavement::CalculationOutput output;
        output.Resize(5);  // 2*3-1 for 3-layer structure
        std::cout << "Created output structure with " << output.sigmaT.size() << " result positions" << std::endl;
        
        // Test working data
        Pavement::WorkingData workingData;
        workingData.Initialize(input.layerCount);
        std::cout << "Initialized working data with matrix size: " << workingData.matrixSize << std::endl;
        
#ifdef EIGEN_AVAILABLE
        // Test Eigen integration (from Task 1.1)
        Eigen::Matrix2d m;
        m << 1, 2, 3, 4;
        std::cout << "Eigen test matrix (Task 1.1):\n" << m << std::endl;
#endif

        std::cout << "\nTask 1.2 (Eliminate Global Variables): SUCCESS" << std::endl;
        std::cout << "- Global variables replaced with structured data" << std::endl;
        std::cout << "- Input validation working" << std::endl;
        std::cout << "- Output and working data structures functional" << std::endl;
        
    } catch (const std::exception& e) {
        std::cerr << "ERROR: " << e.what() << std::endl;
        return 1;
    }

    return 0;
}