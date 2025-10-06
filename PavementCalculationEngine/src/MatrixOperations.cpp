#include "MatrixOperations.h"
#include "Logger.h"
#include "Constants.h"
#include <cmath>
#include <stdexcept>
#include <iostream>
#include <fstream>

namespace Pavement {

Eigen::MatrixXd MatrixOperations::AssembleSystemMatrix(
    double m, 
    const CalculationInput& input) 
{
    int k = 4 * input.layerCount - 2;  // System size
    Eigen::MatrixXd M = Eigen::MatrixXd::Zero(k, k);
    
    auto depths = ComputeLayerDepths(input.thicknesses);
    
    // Assemble surface boundary conditions
    AssembleSurfaceBoundary(M, m, input);
    
    LOG_INFO("Assembling " + std::to_string(input.layerCount - 1) + " interfaces");
    
    // Assemble interface blocks for each layer
    int currentRow = 2; // Start after surface conditions
    for (int i = 0; i < input.layerCount - 1; ++i) {
        AssembleInterfaceBlock(M, i, m, input, depths);
        currentRow += 4; // Each interface adds 4 equations
    }
    
    LOG_INFO("Matrix assembly complete");
    
    return M;
}

Eigen::VectorXd MatrixOperations::SolveCoefficients(
    double m, 
    const CalculationInput& input) 
{
    Eigen::MatrixXd M = AssembleSystemMatrix(m, input);
    
    int k = 4 * input.layerCount - 2;
    Eigen::VectorXd b = Eigen::VectorXd::Zero(k);
    
    // Proper surface boundary conditions
    b(0) = 0.0;  // Zero shear stress at surface
    b(1) = -input.pressure;  // Applied normal stress (negative for compression)
    
    // Debug to file and console
    std::ofstream debugFile("C:\\Temp\\PavementDebug.txt", std::ios::app);
    
    auto logToAll = [&](const std::string& msg) {
        std::cout << msg << std::endl;
        if (debugFile.is_open()) {
            debugFile << msg << std::endl;
        }
    };
    
    logToAll("=== MATRIX SOLVE DEBUG (m=" + std::to_string(m) + ") ===");
    logToAll("Matrix size: " + std::to_string(M.rows()) + "x" + std::to_string(M.cols()));
    logToAll("Pressure value: " + std::to_string(input.pressure) + " MPa");
    
    std::string rhsStr = "Right-hand side vector (size " + std::to_string(k) + "): [";
    for (int i = 0; i < std::min(k, 10); ++i) {
        rhsStr += std::to_string(b(i));
        if (i < std::min(k, 10) - 1) rhsStr += ", ";
    }
    if (k > 10) rhsStr += ", ...";
    rhsStr += "]";
    logToAll(rhsStr);
    
    // Log ALL rows of matrix to see what's being assembled
    for (int row = 0; row < M.rows(); ++row) {
        std::string rowStr = "M[" + std::to_string(row) + "]: [";
        for (int col = 0; col < M.cols(); ++col) {  // Show ALL columns
            rowStr += std::to_string(M(row, col));
            if (col < M.cols() - 1) rowStr += ", ";
        }
        rowStr += "]";
        logToAll(rowStr);
    }
    
    if (debugFile.is_open()) {
        debugFile.close();
    }
    
    // SOLUTION 1: Row and column scaling for numerical stability
    // This is critical for ill-conditioned matrices with exponential terms
    Eigen::VectorXd rowScales = Eigen::VectorXd::Ones(k);
    Eigen::VectorXd colScales = Eigen::VectorXd::Ones(k);
    
    // Compute row scaling factors (largest absolute value in each row)
    for (int i = 0; i < k; ++i) {
        double maxRowVal = M.row(i).cwiseAbs().maxCoeff();
        if (maxRowVal > 1e-15) {  // Avoid division by near-zero
            rowScales(i) = 1.0 / maxRowVal;
        }
    }
    
    // Compute column scaling factors (largest absolute value in each column)
    for (int j = 0; j < k; ++j) {
        double maxColVal = M.col(j).cwiseAbs().maxCoeff();
        if (maxColVal > 1e-15) {
            colScales(j) = 1.0 / maxColVal;
        }
    }
    
    // Apply scaling to matrix: M_scaled = diag(rowScales) * M * diag(colScales)
    Eigen::MatrixXd M_scaled = M;
    for (int i = 0; i < k; ++i) {
        M_scaled.row(i) *= rowScales(i);
    }
    for (int j = 0; j < k; ++j) {
        M_scaled.col(j) *= colScales(j);
    }
    
    // Apply row scaling to right-hand side
    Eigen::VectorXd b_scaled = b.cwiseProduct(rowScales);
    
    // Log scaling info
    LOG_INFO("Matrix scaling applied - max row scale: " + std::to_string(rowScales.maxCoeff()) +
             ", min row scale: " + std::to_string(rowScales.minCoeff()));
    
    // Check matrix condition for numerical stability
    double conditionNumber = CheckConditionNumber(M_scaled);
    if (conditionNumber > Constants::CONDITION_NUMBER_WARNING_THRESHOLD) {
        LOG_WARNING("High condition number " + std::to_string(conditionNumber) + 
                   " - results may be inaccurate");
        std::cerr << "Warning: High condition number " << conditionNumber 
                  << " - results may be inaccurate" << std::endl;
    }
    
    // Use partial pivoting LU decomposition (stable and fast) on SCALED matrix
    Eigen::PartialPivLU<Eigen::MatrixXd> lu = M_scaled.partialPivLu();
    Eigen::VectorXd x_scaled = lu.solve(b_scaled);
    
    // Unscale the solution: x = diag(colScales) * x_scaled
    Eigen::VectorXd x = x_scaled.cwiseProduct(colScales);
    
    // Check solution validity using ORIGINAL matrix and RHS
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
    
    // CRITICAL: Platform layer (last layer) interface MUST be unbonded
    // because the platform only has 2 coefficients (A, B), not 4
    // Bonded/semi-bonded interfaces require 4×4 coefficient continuity
    bool isPlatformInterface = (layerIndex == input.layerCount - 2);
    if (isPlatformInterface) {
        interfaceType = 2; // Force unbonded for platform interface
    }
    
    LOG_INFO(
        "Assembling interface " + std::to_string(layerIndex) +
        ", type=" + std::to_string(interfaceType) +
        ", row=" + std::to_string(row) +
        ", isPlatform=" + std::to_string(isPlatformInterface));
    
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
    // Complete bonded interface implementation based on layered elastic theory
    // 4 continuity equations: vertical displacement, radial displacement, vertical stress, shear stress
    
    double h = depths[layerIndex + 1]; // Interface depth
    double E1 = input.youngModuli[layerIndex];     // Upper layer modulus
    double E2 = input.youngModuli[layerIndex + 1]; // Lower layer modulus
    double nu1 = input.poissonRatios[layerIndex];
    double nu2 = input.poissonRatios[layerIndex + 1];
    
    // Shear moduli
    double G1 = E1 / (2.0 * (1.0 + nu1));
    double G2 = E2 / (2.0 * (1.0 + nu2));
    
    // SOLUTION 3: Stabilisation selon littérature académique
    // "formulation contain only non-positive exponents, which are critical for numerical stability"
    double mh = m * h;
    
    // Technique de stabilisation CRITIQUE: Pour m*h > 30, exp(m*h) déborde
    // On reformule en utilisant UNIQUEMENT les termes décroissants
    // Principe: Si exp(mh) est énorme, les termes en exp(-mh) dominent de toute façon
    double exp_neg_mh, exp_pos_mh;
    
    if (mh > 30.0) {
        // Pour grand mh: exp(mh) >> exp(-mh), donc termes positifs négligeables
        // On met exp_pos_mh = 0 et on garde seulement exp_neg_mh
        exp_neg_mh = std::exp(-mh);
        exp_pos_mh = 0.0;  // Négligeable comparé à exp(mh) qui déborderait
    } else {
        // Pour petit/moyen mh: calcul normal
        exp_neg_mh = std::exp(-mh);
        exp_pos_mh = std::exp(mh);
    }

    
    int col1 = layerIndex * 4;       // Upper layer coefficients
    int col2 = (layerIndex + 1) * 4; // Lower layer coefficients
    
    // Check if we have room for the next layer's coefficients
    // For the last non-platform layer interfacing with platform, platform has no full coefficient set
    const int maxCol = M.cols() - 1;
    const bool hasNextLayerCoeffs = (col2 + 3) <= maxCol;
    
    if (!hasNextLayerCoeffs) {
        // Platform layer interface - should not happen for bonded interface
        // but add safety check
        throw std::runtime_error(
            "Bonded interface not supported for platform layer at layerIndex=" + 
            std::to_string(layerIndex));
    }
    
    // Equation 1: Continuity of vertical displacement w
    // w_upper(h) = w_lower(h)
    M(row, col1) = exp_neg_mh;
    M(row, col1 + 1) = h * exp_neg_mh;
    M(row, col1 + 2) = exp_pos_mh;
    M(row, col1 + 3) = h * exp_pos_mh;
    
    M(row, col2) = -exp_neg_mh;
    M(row, col2 + 1) = -h * exp_neg_mh;
    M(row, col2 + 2) = -exp_pos_mh;
    M(row, col2 + 3) = -h * exp_pos_mh;
    
    // Equation 2: Continuity of radial displacement u
    // u_upper(h) = u_lower(h)
    double term1_1 = ((1.0 - nu1) / m) * exp_neg_mh;
    double term1_2 = ((1.0 - nu1) * h / m - 1.0 / (m * m)) * exp_neg_mh;
    double term1_3 = -((1.0 - nu1) / m) * exp_pos_mh;
    double term1_4 = -((1.0 - nu1) * h / m + 1.0 / (m * m)) * exp_pos_mh;
    
    double term2_1 = -((1.0 - nu2) / m) * exp_neg_mh;
    double term2_2 = -((1.0 - nu2) * h / m - 1.0 / (m * m)) * exp_neg_mh;
    double term2_3 = ((1.0 - nu2) / m) * exp_pos_mh;
    double term2_4 = ((1.0 - nu2) / h / m + 1.0 / (m * m)) * exp_pos_mh;
    
    M(row + 1, col1) = term1_1;
    M(row + 1, col1 + 1) = term1_2;
    M(row + 1, col1 + 2) = term1_3;
    M(row + 1, col1 + 3) = term1_4;
    
    M(row + 1, col2) = term2_1;
    M(row + 1, col2 + 1) = term2_2;
    M(row + 1, col2 + 2) = term2_3;
    M(row + 1, col2 + 3) = term2_4;
    
    // Equation 3: Continuity of vertical stress σ_z
    // σ_z_upper(h) = σ_z_lower(h)
    double sigma_z1_1 = E1 * ((1.0 - nu1) + nu1 * m * h) * exp_neg_mh;
    double sigma_z1_2 = E1 * ((1.0 - nu1) * h + nu1 * (m * h * h - 1.0 / m)) * exp_neg_mh;
    double sigma_z1_3 = E1 * ((1.0 - nu1) - nu1 * m * h) * exp_pos_mh;
    double sigma_z1_4 = E1 * ((1.0 - nu1) * h - nu1 * (m * h * h + 1.0 / m)) * exp_pos_mh;
    
    double sigma_z2_1 = -E2 * ((1.0 - nu2) + nu2 * m * h) * exp_neg_mh;
    double sigma_z2_2 = -E2 * ((1.0 - nu2) * h + nu2 * (m * h * h - 1.0 / m)) * exp_neg_mh;
    double sigma_z2_3 = -E2 * ((1.0 - nu2) - nu2 * m * h) * exp_pos_mh;
    double sigma_z2_4 = -E2 * ((1.0 - nu2) * h - nu2 * (m * h * h + 1.0 / m)) * exp_pos_mh;
    
    M(row + 2, col1) = sigma_z1_1;
    M(row + 2, col1 + 1) = sigma_z1_2;
    M(row + 2, col1 + 2) = sigma_z1_3;
    M(row + 2, col1 + 3) = sigma_z1_4;
    
    M(row + 2, col2) = sigma_z2_1;
    M(row + 2, col2 + 1) = sigma_z2_2;
    M(row + 2, col2 + 2) = sigma_z2_3;
    M(row + 2, col2 + 3) = sigma_z2_4;
    
    // Equation 4: Continuity of shear stress τ_rz
    // τ_rz_upper(h) = τ_rz_lower(h)
    double tau_rz1_1 = G1 * m * (1.0 - m * h) * exp_neg_mh;
    double tau_rz1_2 = G1 * m * (-h + m * h * h - 2.0 / m) * exp_neg_mh;
    double tau_rz1_3 = -G1 * m * (1.0 + m * h) * exp_pos_mh;
    double tau_rz1_4 = -G1 * m * (h + m * h * h + 2.0 / m) * exp_pos_mh;
    
    double tau_rz2_1 = -G2 * m * (1.0 - m * h) * exp_neg_mh;
    double tau_rz2_2 = -G2 * m * (-h + m * h * h - 2.0 / m) * exp_neg_mh;
    double tau_rz2_3 = G2 * m * (1.0 + m * h) * exp_pos_mh;
    double tau_rz2_4 = G2 * m * (h + m * h * h + 2.0 / m) * exp_pos_mh;
    
    M(row + 3, col1) = tau_rz1_1;
    M(row + 3, col1 + 1) = tau_rz1_2;
    M(row + 3, col1 + 2) = tau_rz1_3;
    M(row + 3, col1 + 3) = tau_rz1_4;
    
    M(row + 3, col2) = tau_rz2_1;
    M(row + 3, col2 + 1) = tau_rz2_2;
    M(row + 3, col2 + 2) = tau_rz2_3;
    M(row + 3, col2 + 3) = tau_rz2_4;
}

void MatrixOperations::AssembleUnbondedInterface(
    Eigen::MatrixXd& M,
    int row,
    int layerIndex,
    double m,
    const CalculationInput& input,
    const std::vector<double>& depths) 
{
    // Unbonded interface boundary conditions (slip interface):
    // 1. Continuity of vertical displacement: w_upper(h) = w_lower(h)
    // 2. Continuity of vertical stress: σ_z_upper(h) = σ_z_lower(h)
    // 3. Zero shear stress in upper layer: τ_rz_upper(h) = 0
    // 4. Zero shear stress in lower layer: τ_rz_lower(h) = 0
    // NOTE: Radial displacement u is discontinuous at unbonded interface
    
    const double h = depths[layerIndex + 1];
    const double E1 = input.youngModuli[layerIndex];
    const double nu1 = input.poissonRatios[layerIndex];
    const double E2 = input.youngModuli[layerIndex + 1];
    const double nu2 = input.poissonRatios[layerIndex + 1];
    const int col = layerIndex * 4;
    
    // Check if lower layer is platform (semi-infinite foundation)
    // Platform has only 2 coefficients (A, B) starting at column 4*(layerCount-1)
    // For 4 layers: layers 0,1,2 have 4 coeffs each (0-11), platform has 2 (12-13)
    const bool isPlatformInterface = (layerIndex == input.layerCount - 2);
    const int platformCol = isPlatformInterface ? (4 * (input.layerCount - 1)) : 0;
    
    LOG_INFO("Unbonded interface: layerIndex=" + std::to_string(layerIndex) +
             ", row=" + std::to_string(row) +
             ", col=" + std::to_string(col) +
             ", isPlatform=" + std::to_string(isPlatformInterface) +
             ", platformCol=" + std::to_string(platformCol));
    
    // Check if we have room for the next layer's FULL coefficient set (4 coefficients)
    const int maxCol = M.cols() - 1;
    const bool hasNextLayerCoeffs = !isPlatformInterface && ((col + 7) <= maxCol);
    
    // SOLUTION 3: Stabilisation académique des exponentielles (UNIFORMISÉE avec AssembleBondedInterface)
    // "formulation contain only non-positive exponents, which are critical for numerical stability"
    const double mh = m * h;
    
    // Technique de stabilisation CRITIQUE: Pour m*h > 30, exp(m*h) déborde
    // On reformule en utilisant UNIQUEMENT les termes décroissants
    // Principe: Si exp(mh) est énorme, les termes en exp(-mh) dominent de toute façon
    double exp_neg_mh, exp_pos_mh;
    
    if (mh > 30.0) {
        // Pour grand mh: exp(mh) >> exp(-mh), donc termes positifs négligeables
        // On met exp_pos_mh = 0 et on garde seulement exp_neg_mh
        exp_neg_mh = std::exp(-mh);
        exp_pos_mh = 0.0;  // Négligeable comparé à exp(mh) qui déborderait
    } else {
        // Pour petit/moyen mh: calcul normal
        exp_neg_mh = std::exp(-mh);
        exp_pos_mh = std::exp(mh);
    }

    
    if (isPlatformInterface) {
        LOG_INFO("Platform interface: m=" + std::to_string(m) + ", h=" + std::to_string(h) +
                 ", exp_neg_mh=" + std::to_string(exp_neg_mh) +
                 ", exp_pos_mh=" + std::to_string(exp_pos_mh));
    }
    
    // Equation 1: Continuity of vertical displacement w_upper(h) = w_lower(h)
    // w = (A + Bz)e^(-mz) + (C + Dz)e^(mz)
    // Upper layer (layer i): w_i(h) = (A_i + B_i*h)exp(-m*h) + (C_i + D_i*h)exp(m*h)
    M(row, col) = exp_neg_mh;                    // A_i coefficient
    M(row, col + 1) = h * exp_neg_mh;            // B_i coefficient
    M(row, col + 2) = exp_pos_mh;                // C_i coefficient
    M(row, col + 3) = h * exp_pos_mh;            // D_i coefficient
    
    if (isPlatformInterface) {
        // Platform layer: Only A and B coefficients (decreasing exponential only)
        // w_platform(h) = (A_platform + B_platform*h)exp(-m*h)
        LOG_INFO("Writing platform equation 1: M(" + std::to_string(row) + "," + std::to_string(platformCol) + ") = " + std::to_string(-exp_neg_mh));
        M(row, platformCol) = -exp_neg_mh;       // A_platform coefficient
        M(row, platformCol + 1) = -h * exp_neg_mh; // B_platform coefficient
        LOG_INFO("After write: M(" + std::to_string(row) + "," + std::to_string(platformCol) + ") = " + std::to_string(M(row, platformCol)));
    } else if (hasNextLayerCoeffs) {
        // Normal layer: All 4 coefficients
        M(row, col + 4) = -exp_neg_mh;               // A_{i+1} coefficient
        M(row, col + 5) = -h * exp_neg_mh;           // B_{i+1} coefficient
        M(row, col + 6) = -exp_pos_mh;               // C_{i+1} coefficient
        M(row, col + 7) = -h * exp_pos_mh;           // D_{i+1} coefficient
    }
    
    // Equation 2: Continuity of vertical stress σ_z_upper(h) = σ_z_lower(h)
    // σ_z = -E * ∂w/∂z (simplified vertical stress)
    // ∂w/∂z = [-m*A + B - m*B*z]e^(-mz) + [m*C + D + m*D*z]e^(mz)
    
    // Upper layer vertical stress coefficients
    M(row + 1, col) = -E1 * (-m * exp_neg_mh);                    // A_i: -E1*(-m)e^(-mh)
    M(row + 1, col + 1) = -E1 * (exp_neg_mh - m * h * exp_neg_mh); // B_i: -E1*(1 - m*h)e^(-mh)
    M(row + 1, col + 2) = -E1 * (m * exp_pos_mh);                  // C_i: -E1*m*e^(mh)
    M(row + 1, col + 3) = -E1 * (exp_pos_mh + m * h * exp_pos_mh); // D_i: -E1*(1 + m*h)e^(mh)
    
    if (isPlatformInterface) {
        // Platform stress: Only decreasing exponential terms
        // ∂w_platform/∂z = [-m*A_platform + B_platform - m*B_platform*h]e^(-mh)
        M(row + 1, platformCol) = E2 * (-m * exp_neg_mh);                    // A_platform
        M(row + 1, platformCol + 1) = E2 * (exp_neg_mh - m * h * exp_neg_mh); // B_platform
    } else if (hasNextLayerCoeffs) {
        // Normal layer: All stress terms
        M(row + 1, col + 4) = E2 * (-m * exp_neg_mh);                    // A_{i+1}
        M(row + 1, col + 5) = E2 * (exp_neg_mh - m * h * exp_neg_mh);   // B_{i+1}
        M(row + 1, col + 6) = E2 * (m * exp_pos_mh);                     // C_{i+1}
        M(row + 1, col + 7) = E2 * (exp_pos_mh + m * h * exp_pos_mh);   // D_{i+1}
    }
    
    // Equation 3: Zero shear stress in upper layer τ_rz_upper(h) = 0
    // τ_rz = G * (∂u/∂z - ∂w/∂r) ≈ G * [∂u/∂z - m*w] (simplified)
    // At unbonded interface, shear must vanish in upper layer
    
    const double G1 = E1 / (2.0 * (1.0 + nu1));
    
    // Shear stress upper layer: G1 * [derivative terms]
    M(row + 2, col) = G1 * exp_neg_mh;           // A_i coefficient
    M(row + 2, col + 1) = G1 * h * exp_neg_mh;   // B_i coefficient
    M(row + 2, col + 2) = -G1 * exp_pos_mh;      // C_i coefficient (sign change for positive exponential)
    M(row + 2, col + 3) = -G1 * h * exp_pos_mh;  // D_i coefficient
    
    // Equation 4: Zero shear stress in lower layer τ_rz_lower(h) = 0
    const double G2 = E2 / (2.0 * (1.0 + nu2));
    
    if (isPlatformInterface) {
        // Platform shear stress: Only A and B coefficients
        M(row + 3, platformCol) = G2 * exp_neg_mh;       // A_platform coefficient
        M(row + 3, platformCol + 1) = G2 * h * exp_neg_mh; // B_platform coefficient
    } else if (hasNextLayerCoeffs) {
        // Normal layer: All shear terms
        M(row + 3, col + 4) = G2 * exp_neg_mh;       // A_{i+1} coefficient
        M(row + 3, col + 5) = G2 * h * exp_neg_mh;   // B_{i+1} coefficient
        M(row + 3, col + 6) = -G2 * exp_pos_mh;      // C_{i+1} coefficient
        M(row + 3, col + 7) = -G2 * h * exp_pos_mh;  // D_{i+1} coefficient
    }
}

void MatrixOperations::AssembleSurfaceBoundary(
    Eigen::MatrixXd& M,
    double m,
    const CalculationInput& input) 
{
    // Surface boundary conditions at z = 0 (pavement surface):
    // Row 0: Zero shear stress at free surface: τ_rz(z=0) = 0
    // Row 1: Applied normal stress at surface: σ_z(z=0) = -P (pressure)
    
    // For the first layer (layer 0), at surface z = 0:
    // Displacement field: w = (A_1 + B_1*z)e^(-m*z) + (C_1 + D_1*z)e^(m*z)
    //                     u = (radial displacement expression)
    
    // At z = 0: e^(-m*0) = 1, e^(m*0) = 1, z = 0
    
    // Equation 1: Zero shear stress at surface τ_rz(0) = 0
    // τ_rz = G * (∂u/∂z - ∂w/∂r)
    // For axisymmetric case with Hankel transform approach:
    // The shear stress boundary condition simplifies to a relationship
    // between the coefficients A_1 and C_1
    // Typical form: A_1 - C_1 = 0  OR  τ_rz ~ (A_1 - C_1) terms
    
    M(0, 0) = 1.0;   // A_1 coefficient (positive contribution)
    M(0, 1) = 0.0;   // B_1 coefficient (zero at z=0)
    M(0, 2) = -1.0;  // C_1 coefficient (negative for zero shear)
    M(0, 3) = 0.0;   // D_1 coefficient (zero at z=0)
    
    // Equation 2: Applied normal stress at surface σ_z(0) = -P
    // σ_z = -E/(1-ν²) * [(1-ν)∂w/∂z + ν*m*u]
    // Simplified for surface: σ_z ≈ -E * ∂w/∂z
    // ∂w/∂z|_{z=0} = -m*A_1 + B_1 + m*C_1 + D_1
    // Setting σ_z(0) = -P gives: -E_1*(-m*A_1 + B_1 + m*C_1 + D_1) = -P
    // Simplifying: E_1*(m*A_1 - B_1 - m*C_1 - D_1) = P
    
    const double E1 = input.youngModuli[0];
    // Parameter 'm' now properly passed from Hankel integration loop
    // Surface boundary equations now use correct varying 'm' value instead of hardcoded 1.0
    
    // Surface stress boundary condition form:
    // E1*m*A_1 - E1*B_1 - E1*m*C_1 - E1*D_1 = P
    
    // Now using correct varying 'm' parameter instead of simplified form:
    M(1, 0) = E1 * m;    // A_1 coefficient: E1*m*A_1 term
    M(1, 1) = -E1;       // B_1 coefficient: -E1*B_1 term (derivative)
    M(1, 2) = -E1 * m;   // C_1 coefficient: -E1*m*C_1 term
    M(1, 3) = -E1;       // D_1 coefficient: -E1*D_1 term (derivative)
    
    // Note: The right-hand side vector b will contain:
    // b(0) = 0.0 (zero shear stress)
    // b(1) = -input.pressure (applied normal stress)
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
