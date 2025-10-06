/**
 * @file test_trmm_solver.cpp
 * @brief Google Test suite for TRMM Solver
 * 
 * Tests the Transmission and Reflection Matrix Method implementation,
 * focusing on numerical stability for extreme cases that fail with TMM.
 * 
 * @author Pavement Calculation Team
 * @date 2025-10-05
 */

#include <gtest/gtest.h>
#include "TRMMSolver.h"
#include "PavementData.h"
#include <cmath>
#include <vector>

using namespace PavementCalculation;

// ============================================================================
// Test Fixture
// ============================================================================

class TRMMSolverTest : public ::testing::Test {
protected:
    void SetUp() override {
        solver = std::make_unique<TRMMSolver>();
        
        // Configure for verbose testing
        TRMMSolver::TRMMConfig config;
        config.verbose_logging = true;
        config.stability_threshold = 700.0;
        config.tolerance = 1e-6;
        solver->SetConfig(config);
    }
    
    void TearDown() override {
        solver.reset();
    }
    
    std::unique_ptr<TRMMSolver> solver;
};

// ============================================================================
// Stability Tests - Core TRMM Advantage
// ============================================================================

TEST_F(TRMMSolverTest, ExponentialStability_Moderate) {
    // Test: Moderate m*h values should produce stable matrices
    
    double m = 184.805;  // First critical value from test failures
    double h = 0.20;
    double mh = m * h;  // = 36.96
    
    EXPECT_LT(mh, 50.0) << "This is a moderate case";
    
    // Build layer matrices
    auto matrices = solver->BuildLayerMatrices(5000.0, 0.35, h, m);
    
    // All matrix elements should be ≤ 1.5 (allowing small tolerance)
    double max_T = matrices.T.cwiseAbs().maxCoeff();
    double max_R = matrices.R.cwiseAbs().maxCoeff();
    
    EXPECT_LE(max_T, 1.5) << "Transmission matrix elements should be bounded";
    EXPECT_LE(max_R, 1.5) << "Reflection matrix elements should be bounded";
    
    // Check stability flag
    EXPECT_TRUE(matrices.IsStable()) << "Matrices should be flagged as stable";
    
    // Condition number should be reasonable
    double cond = matrices.GetConditionNumber();
    EXPECT_LT(cond, 1e6) << "Condition number should be reasonable: " << cond;
}

TEST_F(TRMMSolverTest, ExponentialStability_Critical) {
    // Test: Critical m*h value that caused TMM failure
    
    double m = 375.195;  // Second critical value
    double h = 0.20;
    double mh = m * h;  // = 75.04
    
    // With TMM: exp(+75.04) = 3.78e32 → OVERFLOW
    // With TRMM: exp(-75.04) = 2.64e-33 → STABLE
    
    double exp_neg = std::exp(-mh);
    EXPECT_GT(exp_neg, 0.0) << "Negative exponential should be positive";
    EXPECT_LE(exp_neg, 1.0) << "Negative exponential should be ≤ 1.0";
    
    // Build matrices - should not throw
    EXPECT_NO_THROW({
        auto matrices = solver->BuildLayerMatrices(5000.0, 0.35, h, m);
        EXPECT_TRUE(matrices.IsStable());
    });
}

TEST_F(TRMMSolverTest, ExponentialStability_Extreme) {
    // Test: Extreme m*h value that would catastrophically fail with TMM
    
    double m = 521.118;  // Third critical value
    double h = 0.20;
    double mh = m * h;  // = 104.22
    
    // With TMM: exp(+104.22) = 2.43e45 → CATASTROPHIC OVERFLOW
    // With TRMM: exp(-104.22) = 4.12e-46 → STABLE (small but valid)
    
    EXPECT_NO_THROW({
        auto matrices = solver->BuildLayerMatrices(5000.0, 0.35, h, m);
        
        // Even for extreme case, all elements bounded
        EXPECT_TRUE(matrices.IsStable());
        
        // Condition number should still be reasonable
        double cond = matrices.GetConditionNumber();
        EXPECT_LT(cond, 1e8) << "Even extreme case should have reasonable condition: " << cond;
    });
}

TEST_F(TRMMSolverTest, ExponentialStability_VeryExtreme) {
    // Test: Push to the limits - m*h = 150
    
    double m = 750.0;
    double h = 0.20;
    double mh = m * h;  // = 150
    
    // With TMM: impossible (exp(150) = 1.4e65)
    // With TRMM: exp(-150) = 7.0e-66 → underflows to near zero, but stable
    
    EXPECT_NO_THROW({
        auto matrices = solver->BuildLayerMatrices(5000.0, 0.35, h, m);
        EXPECT_TRUE(matrices.IsStable());
    });
}

// ============================================================================
// Validation Against TMM Failure Cases
// ============================================================================

TEST_F(TRMMSolverTest, CompareWithTMMFailure_Test5_TwoLayers) {
    // Test: Exact configuration from C API Test 5 that fails with TMM
    
    PavementInput input = {};
    input.layerCount = 2;
    
    // Layer properties
    double poisson[] = {0.35, 0.35};
    double moduli[] = {5000, 50};  // MPa
    double thickness[] = {0.20, 100.0};  // meters
    int bonded[] = {1};  // Bonded interface
    double z_coords[] = {0.0, 0.10, 0.20};
    
    input.poissonRatios = poisson;
    input.youngModuli = moduli;
    input.thicknesses = thickness;
    input.bondedInterfaces = bonded;
    input.wheelType = 0;  // Simple wheel
    input.pressure_kpa = 662.0;
    input.wheelRadius = 0.125;
    input.nz = 3;
    input.z_coords = z_coords;
    
    PavementOutput output = {};
    output.nz = input.nz;
    output.deflection_mm = new double[output.nz];
    output.vertical_stress_kpa = new double[output.nz];
    output.horizontal_strain = new double[output.nz];
    
    // TRMM should succeed where TMM failed
    bool success = solver->CalculateStable(input, output);
    
    EXPECT_TRUE(success) << "TRMM should succeed on TMM failure case";
    
    if (success) {
        // Surface deflection must be positive (TMM gave 0.0)
        EXPECT_GT(output.deflection_mm[0], 0.0) 
            << "Surface deflection must be positive under load";
        
        // Deflection must be physically reasonable (not zero, not huge)
        EXPECT_LT(output.deflection_mm[0], 10.0) 
            << "Deflection should be reasonable (< 10mm for this load)";
        
        // Deflection should decrease with depth
        EXPECT_GE(output.deflection_mm[0], output.deflection_mm[1])
            << "Deflection should decrease with depth";
        EXPECT_GE(output.deflection_mm[1], output.deflection_mm[2])
            << "Deflection should decrease monotonically";
        
        // Surface stress should be positive
        EXPECT_GT(output.vertical_stress_kpa[0], 0.0)
            << "Surface stress must be positive";
        
        std::cout << "\n=== TRMM Results for Test 5 Configuration ===" << std::endl;
        std::cout << "Surface deflection: " << output.deflection_mm[0] << " mm" << std::endl;
        std::cout << "Mid-layer deflection: " << output.deflection_mm[1] << " mm" << std::endl;
        std::cout << "Bottom deflection: " << output.deflection_mm[2] << " mm" << std::endl;
        std::cout << "Calculation time: " << output.calculation_time_ms << " ms" << std::endl;
    }
    
    // Cleanup
    delete[] output.deflection_mm;
    delete[] output.vertical_stress_kpa;
    delete[] output.horizontal_strain;
}

TEST_F(TRMMSolverTest, CompareWithTMMFailure_Test7_TwinWheels) {
    // Test: Twin wheel configuration that fails with TMM
    
    PavementInput input = {};
    input.layerCount = 2;
    
    double poisson[] = {0.35, 0.35};
    double moduli[] = {5000, 50};
    double thickness[] = {0.20, 100.0};
    int bonded[] = {1};
    double z_coords[] = {0.0};
    
    input.poissonRatios = poisson;
    input.youngModuli = moduli;
    input.thicknesses = thickness;
    input.bondedInterfaces = bonded;
    input.wheelType = 1;  // Twin wheels
    input.pressure_kpa = 662.0;
    input.wheelRadius = 0.125;
    input.wheelSpacing = 0.375;  // 375 mm spacing
    input.nz = 1;
    input.z_coords = z_coords;
    
    PavementOutput output = {};
    output.nz = input.nz;
    output.deflection_mm = new double[output.nz];
    output.vertical_stress_kpa = new double[output.nz];
    output.horizontal_strain = new double[output.nz];
    
    bool success = solver->CalculateStable(input, output);
    
    EXPECT_TRUE(success) << "TRMM should handle twin wheels";
    
    if (success) {
        EXPECT_GT(output.deflection_mm[0], 0.0) 
            << "Twin wheel deflection must be positive";
        
        std::cout << "\n=== TRMM Results for Twin Wheels ===" << std::endl;
        std::cout << "Surface deflection: " << output.deflection_mm[0] << " mm" << std::endl;
        std::cout << "Calculation time: " << output.calculation_time_ms << " ms" << std::endl;
    }
    
    delete[] output.deflection_mm;
    delete[] output.vertical_stress_kpa;
    delete[] output.horizontal_strain;
}

// ============================================================================
// Multi-Layer Tests
// ============================================================================

TEST_F(TRMMSolverTest, ThreeLayerStructure) {
    // Test: 3-layer pavement structure
    
    PavementInput input = {};
    input.layerCount = 3;
    
    double poisson[] = {0.35, 0.35, 0.35};
    double moduli[] = {5000, 200, 50};
    double thickness[] = {0.15, 0.30, 100.0};
    int bonded[] = {1, 1};
    double z_coords[] = {0.0, 0.15, 0.45, 1.0};
    
    input.poissonRatios = poisson;
    input.youngModuli = moduli;
    input.thicknesses = thickness;
    input.bondedInterfaces = bonded;
    input.wheelType = 0;
    input.pressure_kpa = 662.0;
    input.wheelRadius = 0.125;
    input.nz = 4;
    input.z_coords = z_coords;
    
    PavementOutput output = {};
    output.nz = input.nz;
    output.deflection_mm = new double[output.nz];
    output.vertical_stress_kpa = new double[output.nz];
    output.horizontal_strain = new double[output.nz];
    
    bool success = solver->CalculateStable(input, output);
    
    EXPECT_TRUE(success) << "3-layer calculation should succeed";
    
    if (success) {
        // Check monotonic decrease
        for (int i = 1; i < output.nz; ++i) {
            EXPECT_GE(output.deflection_mm[i-1], output.deflection_mm[i])
                << "Deflection should decrease with depth at index " << i;
        }
    }
    
    delete[] output.deflection_mm;
    delete[] output.vertical_stress_kpa;
    delete[] output.horizontal_strain;
}

TEST_F(TRMMSolverTest, FiveLayerComplex) {
    // Test: Complex 5-layer structure (from Test 12)
    
    PavementInput input = {};
    input.layerCount = 5;
    
    double poisson[] = {0.35, 0.35, 0.35, 0.35, 0.35};
    double moduli[] = {5000, 400, 200, 100, 50};
    double thickness[] = {0.10, 0.15, 0.20, 0.30, 100.0};
    int bonded[] = {1, 1, 1, 1};
    double z_coords[10];
    for (int i = 0; i < 10; ++i) {
        z_coords[i] = i * 0.1;
    }
    
    input.poissonRatios = poisson;
    input.youngModuli = moduli;
    input.thicknesses = thickness;
    input.bondedInterfaces = bonded;
    input.wheelType = 0;
    input.pressure_kpa = 662.0;
    input.wheelRadius = 0.125;
    input.nz = 10;
    input.z_coords = z_coords;
    
    PavementOutput output = {};
    output.nz = input.nz;
    output.deflection_mm = new double[output.nz];
    output.vertical_stress_kpa = new double[output.nz];
    output.horizontal_strain = new double[output.nz];
    
    bool success = solver->CalculateStable(input, output);
    
    EXPECT_TRUE(success) << "5-layer calculation should succeed";
    
    if (success) {
        EXPECT_GT(output.deflection_mm[0], 0.0) << "Surface deflection positive";
        
        // Should complete in reasonable time
        EXPECT_LT(output.calculation_time_ms, 2000.0) 
            << "Should complete in < 2 seconds";
    }
    
    delete[] output.deflection_mm;
    delete[] output.vertical_stress_kpa;
    delete[] output.horizontal_strain;
}

// ============================================================================
// Performance Tests
// ============================================================================

TEST_F(TRMMSolverTest, PerformanceComparison) {
    // Test: Measure performance for typical case
    
    PavementInput input = {};
    input.layerCount = 3;
    
    double poisson[] = {0.35, 0.35, 0.35};
    double moduli[] = {5000, 200, 50};
    double thickness[] = {0.15, 0.30, 100.0};
    int bonded[] = {1, 1};
    double z_coords[] = {0.0, 0.15, 0.45};
    
    input.poissonRatios = poisson;
    input.youngModuli = moduli;
    input.thicknesses = thickness;
    input.bondedInterfaces = bonded;
    input.wheelType = 0;
    input.pressure_kpa = 662.0;
    input.wheelRadius = 0.125;
    input.nz = 3;
    input.z_coords = z_coords;
    
    PavementOutput output = {};
    output.nz = input.nz;
    output.deflection_mm = new double[output.nz];
    output.vertical_stress_kpa = new double[output.nz];
    output.horizontal_strain = new double[output.nz];
    
    // Run multiple times for average
    constexpr int ITERATIONS = 10;
    double total_time = 0.0;
    
    for (int i = 0; i < ITERATIONS; ++i) {
        bool success = solver->CalculateStable(input, output);
        ASSERT_TRUE(success);
        total_time += output.calculation_time_ms;
    }
    
    double avg_time = total_time / ITERATIONS;
    
    std::cout << "\n=== TRMM Performance ===" << std::endl;
    std::cout << "Average calculation time: " << avg_time << " ms" << std::endl;
    std::cout << "Iterations: " << ITERATIONS << std::endl;
    
    // TRMM may be ~50% slower than TMM, but 100% reliable
    // Target: < 100 ms for 3-layer structure
    EXPECT_LT(avg_time, 100.0) << "Should be reasonably fast";
    
    delete[] output.deflection_mm;
    delete[] output.vertical_stress_kpa;
    delete[] output.horizontal_strain;
}

// ============================================================================
// Main
// ============================================================================

int main(int argc, char** argv) {
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
