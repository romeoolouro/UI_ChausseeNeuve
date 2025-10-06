#include "PavementCalculator.h"
#include "MatrixOperations.h"
#include "Logger.h"
#include "Constants.h"
#include <iostream>
#include <cmath>
#include <stdexcept>
#include <fstream>

namespace Pavement {

CalculationOutput PavementCalculator::Calculate(const CalculationInput& input) {
    // Validate input (throws if invalid)
    LOG_INFO("Starting pavement calculation");
    LOG_DEBUG("Input: " + std::to_string(input.layerCount) + " layers");
    
    input.Validate();
    LOG_INFO("Input validation passed");
    
    // Initialize output
    CalculationOutput output;
    int resultSize = 2 * input.layerCount - 1;
    output.Resize(resultSize);
    output.Clear();
    
    LOG_INFO("Initialized output structure with " + std::to_string(resultSize) + " result positions");
    LOG_DEBUG("Using Gauss-Legendre " + std::to_string(Constants::GAUSS_QUADRATURE_POINTS) + 
             "-point quadrature for Hankel integration");
    
    std::cout << "Starting pavement calculation with " << input.layerCount 
              << " layers using Eigen-based matrix operations..." << std::endl;
    
    // File-based logging for debugging
    std::ofstream debugFile("C:\\Temp\\PavementDebug.txt", std::ios::trunc); // Clear file at start
    if (debugFile.is_open()) {
        debugFile << "=== PAVEMENT CALCULATION START ===" << std::endl;
        debugFile << "Layer count: " << input.layerCount << std::endl;
        debugFile << "Pressure: " << input.pressure << " MPa" << std::endl;
        debugFile << "Contact radius: " << input.contactRadius << " m" << std::endl;
        debugFile << "Result size: " << resultSize << " positions" << std::endl;
        debugFile.close();
    }
    
    // Gauss-Legendre quadrature for Hankel transform integration
    // Integration over [0, infinity] - use practical upper bound
    const double upperBound = Constants::HANKEL_INTEGRATION_BOUND / input.contactRadius;
    
    // Perform Gauss-Legendre integration
    for (int i = 0; i < Constants::GAUSS_QUADRATURE_POINTS; ++i) {
        // Transform from [-1,1] to [0, upperBound]
        double m = (Constants::GAUSS_POINTS_4[i] + 1.0) * 0.5 * upperBound;
        double weight = Constants::GAUSS_WEIGHTS_4[i] * 0.5 * upperBound;
        
        std::cout << "Hankel Integration: Point " << i << ", m = " << m << ", weight = " << weight << std::endl;
        
        // Log to file
        std::ofstream debugFile("C:\\Temp\\PavementDebug.txt", std::ios::app);
        if (debugFile.is_open()) {
            debugFile << "\n=== Hankel Integration Point " << i << " ===" << std::endl;
            debugFile << "m = " << m << ", weight = " << weight << std::endl;
            debugFile.close();
        }
        
        if (m > Constants::MIN_HANKEL_PARAMETER) {  // Avoid singularity at m=0
            try {
                std::cout << "Processing integration point m=" << m << std::endl;
                CalculateForHankelParameter(m, input, output);
                std::cout << "Successfully calculated for m=" << m << std::endl;
                
            } catch (const std::exception& e) {
                LOG_WARNING("Integration point m=" + std::to_string(m) + 
                           " failed: " + std::string(e.what()));
                std::cerr << "Warning: Integration point m=" << m 
                         << " failed: " << e.what() << std::endl;
                // Continue with other points
            }
        }
    }
    
    LOG_INFO("Calculation completed successfully for " + std::to_string(resultSize) + 
             " result positions");
    std::cout << "Calculation completed successfully for " << resultSize 
              << " result positions" << std::endl;
    
    return output;
}

void PavementCalculator::CalculateForHankelParameter(double m, 
                                                     const CalculationInput& input,
                                                     CalculationOutput& output) {
    try {
        // Solve the linear system for this Hankel parameter
        std::cout << "Solving coefficients for m=" << m << std::endl;
        Eigen::VectorXd coefficients = MatrixOperations::SolveCoefficients(m, input);
        
        std::cout << "Coefficients for m=" << m << ": [";
        for (int i = 0; i < std::min(8, (int)coefficients.size()); ++i) {
            std::cout << coefficients(i);
            if (i < std::min(8, (int)coefficients.size()) - 1) std::cout << ", ";
        }
        std::cout << (coefficients.size() > 8 ? ", ...]" : "]") << std::endl;
        
        // Log coefficients to file
        std::ofstream debugFile("C:\\Temp\\PavementDebug.txt", std::ios::app);
        if (debugFile.is_open()) {
            debugFile << "Solved coefficients (size " << coefficients.size() << "): [";
            for (int i = 0; i < coefficients.size(); ++i) {
                debugFile << coefficients(i);
                if (i < coefficients.size() - 1) debugFile << ", ";
            }
            debugFile << "]" << std::endl;
            debugFile.close();
        }
        
        // Calculate solicitations from these coefficients
        std::cout << "Calculating solicitations for m=" << m << std::endl;
        CalculateSolicitationsFromCoefficients(coefficients, m, input, output);
        std::cout << "Completed solicitations for m=" << m << std::endl;
        
    } catch (const std::exception& e) {
        std::cerr << "Exception in CalculateForHankelParameter: " << e.what() << std::endl;
        throw std::runtime_error(
            "Failed to calculate for m=" + std::to_string(m) + ": " + e.what());
    }
}

void PavementCalculator::CalculateSolicitationsFromCoefficients(
    const Eigen::VectorXd& coefficients,
    double m,
    const CalculationInput& input,
    CalculationOutput& output) {
    
    // Calculate interface depths
    std::vector<double> depths;
    depths.push_back(0.0);  // Surface
    
    double cumulativeDepth = 0.0;
    for (int i = 0; i < input.layerCount - 1; ++i) {
        cumulativeDepth += input.thicknesses[i];
        depths.push_back(cumulativeDepth);
    }
    
    int outputIndex = 0;
    
    // For each layer, calculate at top and bottom
    for (int layerIndex = 0; layerIndex < input.layerCount; ++layerIndex) {
        // Setup layer properties
        LayerProperties props;
        props.youngModulus = input.youngModuli[layerIndex];
        props.poissonRatio = input.poissonRatios[layerIndex];
        
        // Extract this layer's coefficients [A, B, C, D]
        Eigen::Vector4d layerCoeffs;
        int coeffBase = layerIndex * 4;
        
        for (int j = 0; j < 4; ++j) {
            if (coeffBase + j < coefficients.size()) {
                layerCoeffs(j) = coefficients(coeffBase + j);
            } else {
                layerCoeffs(j) = 0.0;
            }
        }
        
        // Top of layer
        if (outputIndex < output.sigmaT.size()) {
            double depth = depths[layerIndex];
            auto sol = ComputeSolicitations(layerCoeffs, depth, m, props);
            
            // Accumulate contributions (Hankel transform integration)
            output.sigmaT[outputIndex] += sol.sigmaR;
            output.epsilonT[outputIndex] += sol.epsilonR;
            output.sigmaZ[outputIndex] += sol.sigmaZ;
            output.epsilonZ[outputIndex] += sol.epsilonZ;
            output.deflection[outputIndex] += sol.deflection;
            outputIndex++;
        }
        
        // Bottom of layer (not for last layer if it's semi-infinite platform)
        if (layerIndex < input.layerCount - 1 && outputIndex < output.sigmaT.size()) {
            double depth = depths[layerIndex + 1];
            auto sol = ComputeSolicitations(layerCoeffs, depth, m, props);
            
            output.sigmaT[outputIndex] += sol.sigmaR;
            output.epsilonT[outputIndex] += sol.epsilonR;
            output.sigmaZ[outputIndex] += sol.sigmaZ;
            output.epsilonZ[outputIndex] += sol.epsilonZ;
            output.deflection[outputIndex] += sol.deflection;
            outputIndex++;
        }
    }
}

SolicitationComponents PavementCalculator::ComputeSolicitations(
    const Eigen::Vector4d& coeffs,
    double depth,
    double m,
    const LayerProperties& props) {
    
    SolicitationComponents result;
    
    // Extract coefficients and material properties
    double A = coeffs(0);
    double B = coeffs(1);
    double C = coeffs(2);
    double D = coeffs(3);
    double E = props.youngModulus;
    double nu = props.poissonRatio;
    
    // Compute exponential terms
    double exp_neg = std::exp(-m * depth);
    double exp_pos = std::exp(m * depth);
    
    // Prevent overflow for large arguments
    if (m * depth > Constants::EXPONENTIAL_OVERFLOW_LIMIT) {
        exp_pos = 0.0;
    }
    if (m * depth < -Constants::EXPONENTIAL_OVERFLOW_LIMIT) {
        exp_neg = 0.0;
    }
    
    // Layered elastic theory formulas
    // Displacements
    double u_r = A * exp_neg + B * depth * exp_neg + C * exp_pos + D * depth * exp_pos;
    double u_z = -A * exp_neg + B * (1.0 - m * depth) * exp_neg 
                 + C * exp_pos - D * (1.0 + m * depth) * exp_pos;
    
    // Strains (simplified formulation)
    double epsilon_r = m * (A * exp_neg - C * exp_pos);
    double epsilon_z = -m * (A * exp_neg + C * exp_pos) 
                       + B * m * exp_neg - D * m * exp_pos;
    
    // Stresses from constitutive law
    double factor = E / ((1.0 + nu) * (1.0 - 2.0 * nu));
    result.sigmaR = factor * ((1.0 - nu) * epsilon_r + nu * epsilon_z);
    result.sigmaZ = factor * (nu * epsilon_r + (1.0 - nu) * epsilon_z);
    result.tauRZ = E / (2.0 * (1.0 + nu)) * m * (B * exp_neg + D * exp_pos);
    
    // Strains (convert to microstrain)
    result.epsilonR = epsilon_r * Constants::STRAIN_TO_MICROSTRAIN;
    result.epsilonZ = epsilon_z * Constants::STRAIN_TO_MICROSTRAIN;
    
    // Deflection (convert to mm)
    result.deflection = u_z * Constants::M_TO_MM;
    
    return result;
}

} // namespace Pavement
