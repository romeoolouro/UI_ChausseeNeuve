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
    
    // Assemble surface boundary conditions (rows 0-1)
    AssembleSurfaceBoundary(M, m, input);
    
    // Assemble interface blocks for each layer interface
    int currentRow = 2; // Start after surface conditions (rows 0-1)
    for (int i = 0; i < input.layerCount - 1; ++i) {
        AssembleInterfaceBlock(M, i, m, input, depths, currentRow);
        currentRow += 4; // Each interface adds 4 equations
    }
    
    // Add platform boundary condition (stress → 0 at infinity)
    // This is automatically satisfied by our formulation
    
    return M;
}

Eigen::VectorXd MatrixOperations::SolveCoefficients(
    double m, 
    const CalculationInput& input) 
{
    Eigen::MatrixXd M = AssembleSystemMatrix(m, input);
    
    int k = 4 * input.layerCount - 2;
    Eigen::VectorXd b = Eigen::VectorXd::Zero(k);
    
    // Surface boundary conditions:
    b(0) = 0.0;  // Zero shear stress at surface
    b(1) = -input.pressure;  // Applied normal stress (compression = negative)
    
    // All interface conditions are homogeneous (b = 0)
    
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
        std::cerr << "Matrix solution error: residual = " << residual << std::endl;
        // Don't throw - return x anyway for debugging
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
    const std::vector<double>& depths,
    int startRow) 
{
    // Get interface type (0=bonded, 1=semi-bonded, 2=unbonded)
    int interfaceType = input.interfaceTypes[layerIndex];
    
    if (interfaceType == 0 || interfaceType == 1) {
        // Bonded or semi-bonded interface
        AssembleBondedInterface(M, startRow, layerIndex, m, input, depths);
    } else if (interfaceType == 2) {
        // Unbonded interface
        AssembleUnbondedInterface(M, startRow, layerIndex, m, input, depths);
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
    double h = depths[layerIndex + 1]; // Interface depth
    double E1 = input.youngModuli[layerIndex];     // Upper layer modulus
    double E2 = input.youngModuli[layerIndex + 1]; // Lower layer modulus
    double nu1 = input.poissonRatios[layerIndex];
    double nu2 = input.poissonRatios[layerIndex + 1];
    
    // Column indices for upper layer coefficients (A1, B1, C1, D1)
    int col1 = layerIndex * 4;
    // Column indices for lower layer coefficients (A2, B2, C2, D2)
    int col2 = (layerIndex + 1) * 4;
    
    // Exponential terms at interface depth
    double exp_neg_h = std::exp(-m * h);
    double exp_pos_h = std::exp(m * h);
    
    // Prevent overflow for large arguments
    if (m * h > Constants::EXPONENTIAL_OVERFLOW_LIMIT) {
        exp_pos_h = 0.0;
    }
    
    // Equation 1: Continuity of vertical displacement
    // u_z1(h) = u_z2(h)
    // -A1*exp(-mh) + B1*(1-mh)*exp(-mh) + C1*exp(mh) - D1*(1+mh)*exp(mh) = 
    // -A2*exp(-mh) + B2*(1-mh)*exp(-mh) + C2*exp(mh) - D2*(1+mh)*exp(mh)
    M(row, col1 + 0) = -exp_neg_h;                    // A1
    M(row, col1 + 1) = (1.0 - m * h) * exp_neg_h;    // B1
    M(row, col1 + 2) = exp_pos_h;                     // C1
    M(row, col1 + 3) = -(1.0 + m * h) * exp_pos_h;   // D1
    
    M(row, col2 + 0) = exp_neg_h;                     // -A2
    M(row, col2 + 1) = -(1.0 - m * h) * exp_neg_h;   // -B2
    M(row, col2 + 2) = -exp_pos_h;                    // -C2
    M(row, col2 + 3) = (1.0 + m * h) * exp_pos_h;    // D2
    
    // Equation 2: Continuity of radial displacement
    // u_r1(h) = u_r2(h)
    M(row + 1, col1 + 0) = exp_neg_h;                 // A1
    M(row + 1, col1 + 1) = h * exp_neg_h;             // B1
    M(row + 1, col1 + 2) = exp_pos_h;                 // C1
    M(row + 1, col1 + 3) = h * exp_pos_h;             // D1
    
    M(row + 1, col2 + 0) = -exp_neg_h;                // -A2
    M(row + 1, col2 + 1) = -h * exp_neg_h;            // -B2
    M(row + 1, col2 + 2) = -exp_pos_h;                // -C2
    M(row + 1, col2 + 3) = -h * exp_pos_h;            // -D2
    
    // Equation 3: Continuity of vertical stress (σ_z)
    // E1 * ε_z1 = E2 * ε_z2
    double factor1 = E1 / ((1.0 + nu1) * (1.0 - 2.0 * nu1));
    double factor2 = E2 / ((1.0 + nu2) * (1.0 - 2.0 * nu2));
    
    M(row + 2, col1 + 0) = factor1 * (nu1 - (1.0 - nu1) * m) * exp_neg_h;    // A1
    M(row + 2, col1 + 1) = factor1 * ((1.0 - nu1) * m * h + nu1) * exp_neg_h; // B1
    M(row + 2, col1 + 2) = factor1 * (nu1 + (1.0 - nu1) * m) * exp_pos_h;    // C1
    M(row + 2, col1 + 3) = factor1 * (nu1 - (1.0 - nu1) * m * h) * exp_pos_h; // D1
    
    M(row + 2, col2 + 0) = -factor2 * (nu2 - (1.0 - nu2) * m) * exp_neg_h;    // -A2
    M(row + 2, col2 + 1) = -factor2 * ((1.0 - nu2) * m * h + nu2) * exp_neg_h; // -B2
    M(row + 2, col2 + 2) = -factor2 * (nu2 + (1.0 - nu2) * m) * exp_pos_h;    // -C2
    M(row + 2, col2 + 3) = -factor2 * (nu2 - (1.0 - nu2) * m * h) * exp_pos_h; // -D2
    
    // Equation 4: Continuity of shear stress (τ_rz)
    double shear1 = E1 / (2.0 * (1.0 + nu1));
    double shear2 = E2 / (2.0 * (1.0 + nu2));
    
    M(row + 3, col1 + 0) = shear1 * m * exp_neg_h;    // A1
    M(row + 3, col1 + 1) = shear1 * exp_neg_h;        // B1
    M(row + 3, col1 + 2) = -shear1 * m * exp_pos_h;   // C1
    M(row + 3, col1 + 3) = shear1 * exp_pos_h;        // D1
    
    M(row + 3, col2 + 0) = -shear2 * m * exp_neg_h;   // -A2
    M(row + 3, col2 + 1) = -shear2 * exp_neg_h;       // -B2
    M(row + 3, col2 + 2) = shear2 * m * exp_pos_h;    // C2
    M(row + 3, col2 + 3) = -shear2 * exp_pos_h;       // -D2
}

void MatrixOperations::AssembleUnbondedInterface(
    Eigen::MatrixXd& M,
    int row,
    int layerIndex,
    double m,
    const CalculationInput& input,
    const std::vector<double>& depths) 
{
    double h = depths[layerIndex + 1];
    double E1 = input.youngModuli[layerIndex];
    double E2 = input.youngModuli[layerIndex + 1];
    double nu1 = input.poissonRatios[layerIndex];
    double nu2 = input.poissonRatios[layerIndex + 1];
    
    int col1 = layerIndex * 4;
    int col2 = (layerIndex + 1) * 4;
    
    double exp_neg_h = std::exp(-m * h);
    double exp_pos_h = std::exp(m * h);
    
    if (m * h > Constants::EXPONENTIAL_OVERFLOW_LIMIT) {
        exp_pos_h = 0.0;
    }
    
    // Equation 1: Continuity of vertical displacement (same as bonded)
    M(row, col1 + 0) = -exp_neg_h;
    M(row, col1 + 1) = (1.0 - m * h) * exp_neg_h;
    M(row, col1 + 2) = exp_pos_h;
    M(row, col1 + 3) = -(1.0 + m * h) * exp_pos_h;
    
    M(row, col2 + 0) = exp_neg_h;
    M(row, col2 + 1) = -(1.0 - m * h) * exp_neg_h;
    M(row, col2 + 2) = -exp_pos_h;
    M(row, col2 + 3) = (1.0 + m * h) * exp_pos_h;
    
    // Equation 2: Continuity of normal stress (same as bonded)
    double factor1 = E1 / ((1.0 + nu1) * (1.0 - 2.0 * nu1));
    double factor2 = E2 / ((1.0 + nu2) * (1.0 - 2.0 * nu2));
    
    M(row + 1, col1 + 0) = factor1 * (nu1 - (1.0 - nu1) * m) * exp_neg_h;
    M(row + 1, col1 + 1) = factor1 * ((1.0 - nu1) * m * h + nu1) * exp_neg_h;
    M(row + 1, col1 + 2) = factor1 * (nu1 + (1.0 - nu1) * m) * exp_pos_h;
    M(row + 1, col1 + 3) = factor1 * (nu1 - (1.0 - nu1) * m * h) * exp_pos_h;
    
    M(row + 1, col2 + 0) = -factor2 * (nu2 - (1.0 - nu2) * m) * exp_neg_h;
    M(row + 1, col2 + 1) = -factor2 * ((1.0 - nu2) * m * h + nu2) * exp_neg_h;
    M(row + 1, col2 + 2) = -factor2 * (nu2 + (1.0 - nu2) * m) * exp_pos_h;
    M(row + 1, col2 + 3) = -factor2 * (nu2 - (1.0 - nu2) * m * h) * exp_pos_h;
    
    // Equation 3: Zero shear stress in upper layer
    double shear1 = E1 / (2.0 * (1.0 + nu1));
    
    M(row + 2, col1 + 0) = shear1 * m * exp_neg_h;
    M(row + 2, col1 + 1) = shear1 * exp_neg_h;
    M(row + 2, col1 + 2) = -shear1 * m * exp_pos_h;
    M(row + 2, col1 + 3) = shear1 * exp_pos_h;
    
    // Equation 4: Zero shear stress in lower layer
    double shear2 = E2 / (2.0 * (1.0 + nu2));
    
    M(row + 3, col2 + 0) = shear2 * m * exp_neg_h;
    M(row + 3, col2 + 1) = shear2 * exp_neg_h;
    M(row + 3, col2 + 2) = -shear2 * m * exp_pos_h;
    M(row + 3, col2 + 3) = shear2 * exp_pos_h;
}

void MatrixOperations::AssembleSurfaceBoundary(
    Eigen::MatrixXd& M,
    double m,
    const CalculationInput& input) 
{
    // Surface boundary conditions (z = 0):
    // Row 0: Zero shear stress at surface (τ_rz = 0)
    // τ_rz = G * (∂u_r/∂z + ∂u_z/∂r) = G * m * (B1 + D1) = 0
    double G = input.youngModuli[0] / (2.0 * (1.0 + input.poissonRatios[0]));
    
    M(0, 0) = 0.0;           // A1 doesn't contribute to shear at z=0
    M(0, 1) = G * m;         // B1 coefficient
    M(0, 2) = 0.0;           // C1 doesn't contribute to shear at z=0
    M(0, 3) = G * m;         // D1 coefficient
    
    // Row 1: Applied normal stress at surface (σ_z = -P)
    // σ_z = E/(1+ν)/(1-2ν) * [ν*ε_r + (1-ν)*ε_z]
    double E = input.youngModuli[0];
    double nu = input.poissonRatios[0];
    double factor = E / ((1.0 + nu) * (1.0 - 2.0 * nu));
    
    M(1, 0) = factor * (nu - (1.0 - nu) * m);    // A1 coefficient
    M(1, 1) = factor * nu;                       // B1 coefficient
    M(1, 2) = factor * (nu + (1.0 - nu) * m);    // C1 coefficient
    M(1, 3) = factor * nu;                       // D1 coefficient
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