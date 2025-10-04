#include "MatrixOperations.h"
#include "Logger.h"
#include "Constants.h"
#include <cmath>
#include <stdexcept>
#include <iostream>

namespace Pavement {

Eigen::MatrixXd MatrixOperations::AssembleSystemMatrix(
    double m, 
    const CalculationInput& input) 
{
    int k = 4 * input.layerCount - 2;  // System size
    Eigen::MatrixXd M = Eigen::MatrixXd::Zero(k, k);
    
    auto depths = ComputeLayerDepths(input.thicknesses);
    
    // Assemble surface boundary conditions (row 0)
    AssembleSurfaceBoundary(M, input);
    
    // Assemble interface blocks for each layer
    int currentRow = 2; // Start after surface conditions
    for (int i = 0; i < input.layerCount - 1; ++i) {
        AssembleInterfaceBlock(M, i, m, input, depths);
        currentRow += 4; // Each interface adds 4 equations
    }
    
    return M;
}

Eigen::VectorXd MatrixOperations::SolveCoefficients(
    double m, 
    const CalculationInput& input) 
{
    Eigen::MatrixXd M = AssembleSystemMatrix(m, input);
    
    int k = 4 * input.layerCount - 2;
    Eigen::VectorXd b = Eigen::VectorXd::Zero(k);
    b(0) = 1.0;  // Surface boundary condition: applied load
    
    // Check matrix condition for numerical stability
    double conditionNumber = CheckConditionNumber(M);
    if (conditionNumber > Constants::CONDITION_NUMBER_WARNING_THRESHOLD) {
        LOG_WARNING("High condition number " + std::to_string(conditionNumber) + 
                   " - results may be inaccurate");
        std::cerr << "Warning: High condition number " << conditionNumber 
                  << " - results may be inaccurate" << std::endl;
    }
    
    // Use partial pivoting LU decomposition (stable and fast)
    Eigen::PartialPivLU<Eigen::MatrixXd> lu = M.partialPivLu();
    Eigen::VectorXd x = lu.solve(b);
    
    // Check solution validity
    double residual = (M * x - b).norm();
    if (residual > Constants::RESIDUAL_TOLERANCE) {
        std::string error = "Matrix solution failed: residual = " + std::to_string(residual) +
                          " (tolerance: " + std::to_string(Constants::RESIDUAL_TOLERANCE) + ")";
        LOG_ERROR(error);
        throw std::runtime_error(error);
    }
    
    return x;
}

std::vector<double> MatrixOperations::ComputeLayerDepths(
    const std::vector<double>& thicknesses) 
{
    std::vector<double> depths;
    depths.reserve(thicknesses.size() + 1);
    
    depths.push_back(0.0);  // Surface depth
    double cumulativeDepth = 0.0;
    
    for (size_t i = 0; i < thicknesses.size() - 1; ++i) {  // Exclude platform
        cumulativeDepth += thicknesses[i];
        depths.push_back(cumulativeDepth);
    }
    
    return depths;
}

void MatrixOperations::AssembleInterfaceBlock(
    Eigen::MatrixXd& M,
    int layerIndex,
    double m,
    const CalculationInput& input,
    const std::vector<double>& depths) 
{
    int row = 2 + layerIndex * 4;  // Starting row for this interface
    
    // Get interface type (0=bonded, 1=semi-bonded, 2=unbonded)
    int interfaceType = input.interfaceTypes[layerIndex];
    
    if (interfaceType == 0 || interfaceType == 1) {
        // Bonded or semi-bonded interface
        AssembleBondedInterface(M, row, layerIndex, m, input, depths);
    } else if (interfaceType == 2) {
        // Unbonded interface
        AssembleUnbondedInterface(M, row, layerIndex, m, input, depths);
    }
}

void MatrixOperations::AssembleBondedInterface(
    Eigen::MatrixXd& M,
    int row,
    int layerIndex,
    double m,
    const CalculationInput& input,
    const std::vector<double>& depths) 
{
    // This is a simplified version - in the full implementation,
    // this would contain the complex boundary condition equations
    // from the original det_MFINI function.
    
    double h = depths[layerIndex + 1]; // Interface depth
    double E1 = input.youngModuli[layerIndex];     // Upper layer modulus
    double E2 = input.youngModuli[layerIndex + 1]; // Lower layer modulus
    double nu1 = input.poissonRatios[layerIndex];
    double nu2 = input.poissonRatios[layerIndex + 1];
    
    // Simplified bonded interface equations
    // In reality, these would be the full elastic theory boundary conditions
    int col = layerIndex * 4;
    
    // Continuity of vertical displacement
    M(row, col) = std::exp(-m * h);
    M(row, col + 1) = h * std::exp(-m * h);
    M(row, col + 2) = std::exp(m * h);
    M(row, col + 3) = h * std::exp(m * h);
    
    // Upper layer contribution (negative for continuity)
    M(row, col + 4) = -std::exp(-m * h);
    M(row, col + 5) = -h * std::exp(-m * h);
    
    // Continuity of radial displacement
    M(row + 1, col) = (1.0 - nu1) * std::exp(-m * h);
    M(row + 1, col + 1) = (1.0 - nu1) * h * std::exp(-m * h);
    M(row + 1, col + 2) = -(1.0 - nu1) * std::exp(m * h);
    M(row + 1, col + 3) = -(1.0 - nu1) * h * std::exp(m * h);
    
    // Continuity of vertical stress
    M(row + 2, col) = E1 * std::exp(-m * h);
    M(row + 2, col + 1) = E1 * h * std::exp(-m * h);
    M(row + 2, col + 2) = E1 * std::exp(m * h);
    M(row + 2, col + 3) = E1 * h * std::exp(m * h);
    
    M(row + 2, col + 4) = -E2 * std::exp(-m * h);
    M(row + 2, col + 5) = -E2 * h * std::exp(-m * h);
    
    // Continuity of shear stress
    M(row + 3, col) = E1 / (1.0 + nu1) * std::exp(-m * h);
    M(row + 3, col + 1) = E1 / (1.0 + nu1) * h * std::exp(-m * h);
    M(row + 3, col + 2) = -E1 / (1.0 + nu1) * std::exp(m * h);
    M(row + 3, col + 3) = -E1 / (1.0 + nu1) * h * std::exp(m * h);
    
    M(row + 3, col + 4) = -E2 / (1.0 + nu2) * std::exp(-m * h);
    M(row + 3, col + 5) = -E2 / (1.0 + nu2) * h * std::exp(-m * h);
}

void MatrixOperations::AssembleUnbondedInterface(
    Eigen::MatrixXd& M,
    int row,
    int layerIndex,
    double m,
    const CalculationInput& input,
    const std::vector<double>& depths) 
{
    // Unbonded interface: continuous vertical displacement and normal stress,
    // but zero shear stress
    double h = depths[layerIndex + 1];
    double E1 = input.youngModuli[layerIndex];
    double E2 = input.youngModuli[layerIndex + 1];
    int col = layerIndex * 4;
    
    // Continuity of vertical displacement
    M(row, col) = std::exp(-m * h);
    M(row, col + 1) = h * std::exp(-m * h);
    M(row, col + 2) = std::exp(m * h);
    M(row, col + 3) = h * std::exp(m * h);
    M(row, col + 4) = -std::exp(-m * h);
    M(row, col + 5) = -h * std::exp(-m * h);
    
    // Continuity of normal stress
    M(row + 1, col) = E1 * std::exp(-m * h);
    M(row + 1, col + 1) = E1 * h * std::exp(-m * h);
    M(row + 1, col + 2) = E1 * std::exp(m * h);
    M(row + 1, col + 3) = E1 * h * std::exp(m * h);
    M(row + 1, col + 4) = -E2 * std::exp(-m * h);
    M(row + 1, col + 5) = -E2 * h * std::exp(-m * h);
    
    // Zero shear stress in upper layer
    M(row + 2, col) = E1 * std::exp(-m * h);
    M(row + 2, col + 1) = E1 * h * std::exp(-m * h);
    M(row + 2, col + 2) = -E1 * std::exp(m * h);
    M(row + 2, col + 3) = -E1 * h * std::exp(m * h);
    
    // Zero shear stress in lower layer
    M(row + 3, col + 4) = E2 * std::exp(-m * h);
    M(row + 3, col + 5) = E2 * h * std::exp(-m * h);
}

void MatrixOperations::AssembleSurfaceBoundary(
    Eigen::MatrixXd& M,
    const CalculationInput& input) 
{
    // Surface boundary conditions:
    // Row 0: Zero shear stress at surface (τrz = 0)
    M(0, 0) = 1.0;  // A1 coefficient
    M(0, 2) = -1.0; // C1 coefficient
    
    // Row 1: Applied normal stress at surface (σz = -P)
    M(1, 0) = input.youngModuli[0];  // E1 * A1
    M(1, 1) = 0.0;                   // B1 coefficient
    M(1, 2) = input.youngModuli[0];  // E1 * C1
    M(1, 3) = 0.0;                   // D1 coefficient
}

double MatrixOperations::CheckConditionNumber(const Eigen::MatrixXd& M) 
{
    // Estimate condition number using SVD (expensive but accurate)
    Eigen::JacobiSVD<Eigen::MatrixXd> svd(M, Eigen::ComputeThinU | Eigen::ComputeThinV);
    auto singularValues = svd.singularValues();
    
    double maxSV = singularValues.maxCoeff();
    double minSV = singularValues.minCoeff();
    
    if (minSV < 1e-15) {
        return std::numeric_limits<double>::infinity();
    }
    
    return maxSV / minSV;
}

} // namespace Pavement