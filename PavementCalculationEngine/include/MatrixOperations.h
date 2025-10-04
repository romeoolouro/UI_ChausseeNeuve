#pragma once

#include <Eigen/Dense>
#include <vector>
#include "PavementData.h"

namespace Pavement {

/**
 * Matrix operations using Eigen library for pavement calculation.
 * Replaces manual Gauss-Jordan inversion with optimized LU decomposition.
 */
class MatrixOperations {
public:
    /**
     * Assemble system matrix for given Hankel parameter m.
     * Implements layered elastic theory boundary conditions.
     * 
     * @param m Hankel transform parameter
     * @param input Calculation input with layer properties
     * @return System matrix M for equation M*x = b
     */
    static Eigen::MatrixXd AssembleSystemMatrix(
        double m, 
        const CalculationInput& input);
    
    /**
     * Solve linear system M*x = b for layer coefficients.
     * Uses Eigen's partial pivoting LU decomposition for numerical stability.
     * 
     * @param m Hankel transform parameter
     * @param input Calculation input with layer properties
     * @return Coefficient vector x
     * @throws std::runtime_error if matrix is singular or solution fails
     */
    static Eigen::VectorXd SolveCoefficients(
        double m, 
        const CalculationInput& input);

private:
    /**
     * Compute cumulative layer depths from thicknesses.
     * 
     * @param thicknesses Layer thicknesses in meters
     * @return Vector of depths from surface [0, h1, h1+h2, ...]
     */
    static std::vector<double> ComputeLayerDepths(
        const std::vector<double>& thicknesses);
    
    /**
     * Assemble matrix block for layer interface boundary conditions.
     * Handles bonded, unbonded, and semi-bonded interface types.
     * 
     * @param M System matrix to populate (modified in place)
     * @param layerIndex Layer index (0-based)
     * @param m Hankel transform parameter
     * @param input Calculation input
     * @param depths Cumulative layer depths
     */
    static void AssembleInterfaceBlock(
        Eigen::MatrixXd& M,
        int layerIndex,
        double m,
        const CalculationInput& input,
        const std::vector<double>& depths);
    
    /**
     * Assemble bonded interface conditions (continuous displacement and stress).
     * 
     * @param M System matrix
     * @param row Starting row index
     * @param layerIndex Layer index
     * @param m Hankel parameter
     * @param input Calculation input
     * @param depths Layer depths
     */
    static void AssembleBondedInterface(
        Eigen::MatrixXd& M,
        int row,
        int layerIndex,
        double m,
        const CalculationInput& input,
        const std::vector<double>& depths);
    
    /**
     * Assemble unbonded interface conditions (continuous normal stress, zero shear).
     * 
     * @param M System matrix
     * @param row Starting row index
     * @param layerIndex Layer index
     * @param m Hankel parameter
     * @param input Calculation input
     * @param depths Layer depths
     */
    static void AssembleUnbondedInterface(
        Eigen::MatrixXd& M,
        int row,
        int layerIndex,
        double m,
        const CalculationInput& input,
        const std::vector<double>& depths);

    /**
     * Assemble surface boundary conditions (zero shear stress, applied normal stress).
     * 
     * @param M System matrix
     * @param input Calculation input
     */
    static void AssembleSurfaceBoundary(
        Eigen::MatrixXd& M,
        const CalculationInput& input);
    
    /**
     * Check matrix condition number for numerical stability warning.
     * 
     * @param M Matrix to check
     * @return Condition number estimate
     */
    static double CheckConditionNumber(const Eigen::MatrixXd& M);
};

} // namespace Pavement