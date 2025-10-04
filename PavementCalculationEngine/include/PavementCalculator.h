#pragma once

#include "PavementData.h"
#include "MatrixOperations.h"

namespace Pavement {

// Forward declarations
struct LayerProperties {
    double youngModulus;
    double poissonRatio;
};

struct SolicitationComponents {
    double sigmaR;
    double sigmaZ;
    double tauRZ;
    double epsilonR;
    double epsilonZ;
    double deflection;
};

/**
 * Main pavement calculation engine using layered elastic theory.
 * Uses Eigen-based matrix operations for numerical stability.
 */
class PavementCalculator {
public:
    /**
     * Calculate stresses, strains, and deflections for pavement structure.
     * Uses Hankel transforms and layered elastic theory with Eigen matrix operations.
     * 
     * @param input Structured input data (validated)
     * @return Calculation results for all interfaces
     * @throws std::invalid_argument if input validation fails
     * @throws std::runtime_error if matrix solution fails
     */
    CalculationOutput Calculate(const CalculationInput& input);

private:
    /**
     * Perform Hankel transform integration for single parameter m.
     * 
     * @param m Hankel transform parameter
     * @param input Calculation input
     * @param output Results storage (accumulated)
     */
    void CalculateForHankelParameter(double m, const CalculationInput& input, 
                                   CalculationOutput& output);
    
    /**
     * Calculate stresses and strains at all interfaces for given coefficients.
     * 
     * @param coefficients Solution coefficients from matrix solve
     * @param m Hankel parameter
     * @param input Calculation input
     * @param output Results storage
     */
    void CalculateSolicitationsFromCoefficients(
        const Eigen::VectorXd& coefficients,
        double m,
        const CalculationInput& input,
        CalculationOutput& output);
    
    /**
     * Compute stress and strain at specific depth and radial position.
     * 
     * @param coeffs Layer coefficients [A, B, C, D]
     * @param depth Depth from surface
     * @param m Hankel parameter
     * @param layerProps Layer elastic properties
     * @return Stress/strain components
     */
    SolicitationComponents ComputeSolicitations(
        const Eigen::Vector4d& coeffs,
        double depth,
        double m,
        const LayerProperties& layerProps);
};

} // namespace Pavement