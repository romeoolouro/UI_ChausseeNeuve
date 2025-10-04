#include <gtest/gtest.h>
#include "MatrixOperations.h"
#include "PavementData.h"
#include "Constants.h"
#include <Eigen/Dense>
#include <cmath>

using namespace Pavement;

class MatrixOperationsTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Create default input for testing
        input.SetDefaults();
    }

    CalculationInput input;
};

// ============================================================================
// Basic Matrix Operations Tests
// ============================================================================

TEST_F(MatrixOperationsTest, AssembleSystemMatrixSize) {
    double m = 1.0;
    Eigen::MatrixXd M = MatrixOperations::AssembleSystemMatrix(m, input);
    
    int expectedSize = input.layerCount * 4 - 2;  // 3*4-2 = 10
    EXPECT_EQ(M.rows(), expectedSize);
    EXPECT_EQ(M.cols(), expectedSize);
}

TEST_F(MatrixOperationsTest, AssembleSystemMatrixNotEmpty) {
    double m = 1.0;
    Eigen::MatrixXd M = MatrixOperations::AssembleSystemMatrix(m, input);
    
    // Matrix should have non-zero elements
    double sum = M.sum();
    EXPECT_NE(sum, 0.0);
}

TEST_F(MatrixOperationsTest, SolveCoefficientsReturnsCorrectSize) {
    double m = 1.0;
    Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
    
    int expectedSize = input.layerCount * 4 - 2;
    EXPECT_EQ(coeffs.size(), expectedSize);
}

TEST_F(MatrixOperationsTest, SolveCoefficientsRepeatable) {
    double m = 1.0;
    
    Eigen::VectorXd coeffs1 = MatrixOperations::SolveCoefficients(m, input);
    Eigen::VectorXd coeffs2 = MatrixOperations::SolveCoefficients(m, input);
    
    // Same input should give same output
    for (int i = 0; i < coeffs1.size(); ++i) {
        EXPECT_NEAR(coeffs1(i), coeffs2(i), 1e-10);
    }
}

TEST_F(MatrixOperationsTest, SolveCoefficientsVariesWithParameter) {
    Eigen::VectorXd coeffs1 = MatrixOperations::SolveCoefficients(1.0, input);
    Eigen::VectorXd coeffs2 = MatrixOperations::SolveCoefficients(2.0, input);
    
    // Different m should give different coefficients
    bool different = false;
    for (int i = 0; i < coeffs1.size(); ++i) {
        if (std::abs(coeffs1(i) - coeffs2(i)) > 1e-6) {
            different = true;
            break;
        }
    }
    EXPECT_TRUE(different);
}

// ============================================================================
// Numerical Stability Tests
// ============================================================================

TEST_F(MatrixOperationsTest, SolveCoefficientsLargeParameter) {
    // Test with large Hankel parameter
    double m = 50.0;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_EQ(coeffs.size(), input.layerCount * 4 - 2);
    });
}

TEST_F(MatrixOperationsTest, SolveCoefficientsSmallParameter) {
    // Test with small Hankel parameter (near singularity)
    double m = 0.001;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_EQ(coeffs.size(), input.layerCount * 4 - 2);
    });
}

TEST_F(MatrixOperationsTest, SolveCoefficientsStiffStructure) {
    // Create very stiff structure
    input.youngModuli[0] = 50000.0;  // Very stiff
    input.youngModuli[1] = 5000.0;
    input.youngModuli[2] = 500.0;
    
    double m = 1.0;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_GT(coeffs.size(), 0);
    });
}

TEST_F(MatrixOperationsTest, SolveCoefficientsSoftStructure) {
    // Create soft structure
    input.youngModuli[0] = 1000.0;
    input.youngModuli[1] = 100.0;
    input.youngModuli[2] = 20.0;
    
    double m = 1.0;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_GT(coeffs.size(), 0);
    });
}

// ============================================================================
// Boundary Condition Tests
// ============================================================================

TEST_F(MatrixOperationsTest, BondedInterfaceConditions) {
    // Test bonded interface assembly
    double m = 1.0;
    Eigen::MatrixXd M = MatrixOperations::AssembleSystemMatrix(m, input);
    
    // Matrix should be square and non-singular
    EXPECT_EQ(M.rows(), M.cols());
    
    // Determinant should be non-zero (non-singular)
    double det = M.determinant();
    EXPECT_NE(det, 0.0);
}

TEST_F(MatrixOperationsTest, UnbondedInterfaceHandling) {
    // Create structure with unbonded interface
    input.interfaceTypes[0] = 1;  // Unbonded
    
    double m = 1.0;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_GT(coeffs.size(), 0);
    });
}

// ============================================================================
// Physical Validity Tests
// ============================================================================

TEST_F(MatrixOperationsTest, CoefficientsFinite) {
    double m = 1.0;
    Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
    
    // All coefficients should be finite
    for (int i = 0; i < coeffs.size(); ++i) {
        EXPECT_TRUE(std::isfinite(coeffs(i))) 
            << "Coefficient " << i << " is not finite";
    }
}

TEST_F(MatrixOperationsTest, CoefficientsNotAllZero) {
    double m = 1.0;
    Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
    
    // At least one coefficient should be non-zero
    double maxAbs = coeffs.cwiseAbs().maxCoeff();
    EXPECT_GT(maxAbs, 1e-10);
}

// ============================================================================
// Layer Configuration Tests
// ============================================================================

TEST_F(MatrixOperationsTest, TwoLayerStructure) {
    // Create 2-layer structure
    input.layerCount = 2;
    input.poissonRatios.resize(2, 0.35);
    input.youngModuli = {5000.0, 50.0};
    input.thicknesses = {0.2, 100.0};
    input.interfaceTypes.resize(1, 0);  // 1 interface
    
    double m = 1.0;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_EQ(coeffs.size(), 2 * 4 - 2);  // 6 coefficients
    });
}

TEST_F(MatrixOperationsTest, FiveLayerStructure) {
    // Create 5-layer structure
    input.layerCount = 5;
    input.poissonRatios.resize(5, 0.35);
    input.youngModuli = {10000.0, 5000.0, 1000.0, 200.0, 50.0};
    input.thicknesses = {0.08, 0.12, 0.20, 0.30, 100.0};
    input.interfaceTypes.resize(4, 0);  // 4 interfaces
    
    double m = 1.0;
    
    EXPECT_NO_THROW({
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
        EXPECT_EQ(coeffs.size(), 5 * 4 - 2);  // 18 coefficients
    });
}

// ============================================================================
// Performance Tests
// ============================================================================

TEST_F(MatrixOperationsTest, SolveCoefficientsPerformance) {
    // Test should complete quickly
    auto start = std::chrono::high_resolution_clock::now();
    
    for (int i = 0; i < 100; ++i) {
        double m = 0.1 + i * 0.1;
        Eigen::VectorXd coeffs = MatrixOperations::SolveCoefficients(m, input);
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);
    
    // 100 solves should complete in < 1 second
    EXPECT_LT(duration.count(), 1000);
}
