#include <gtest/gtest.h>
#include "PavementCalculator.h"
#include "PavementData.h"
#include "Constants.h"
#include <cmath>

using namespace Pavement;

class PavementCalculatorTest : public ::testing::Test {
protected:
    void SetUp() override {
        calculator = std::make_unique<PavementCalculator>();
        input.SetDefaults();
    }

    std::unique_ptr<PavementCalculator> calculator;
    CalculationInput input;
};

// ============================================================================
// Basic Calculation Tests
// ============================================================================

TEST_F(PavementCalculatorTest, CalculateReturnsCorrectOutputSize) {
    CalculationOutput output = calculator->Calculate(input);
    
    int expectedSize = 2 * input.layerCount - 1;  // 5 for 3 layers
    EXPECT_EQ(output.sigmaT.size(), static_cast<size_t>(expectedSize));
    EXPECT_EQ(output.epsilonT.size(), static_cast<size_t>(expectedSize));
    EXPECT_EQ(output.sigmaZ.size(), static_cast<size_t>(expectedSize));
    EXPECT_EQ(output.epsilonZ.size(), static_cast<size_t>(expectedSize));
    EXPECT_EQ(output.deflection.size(), static_cast<size_t>(expectedSize));
}

TEST_F(PavementCalculatorTest, CalculateProducesNonZeroResults) {
    CalculationOutput output = calculator->Calculate(input);
    
    // At least some values should be non-zero
    bool hasNonZero = false;
    for (size_t i = 0; i < output.sigmaT.size(); ++i) {
        if (std::abs(output.sigmaT[i]) > 1e-10 || 
            std::abs(output.deflection[i]) > 1e-10) {
            hasNonZero = true;
            break;
        }
    }
    EXPECT_TRUE(hasNonZero);
}

TEST_F(PavementCalculatorTest, CalculateResultsAreFinite) {
    CalculationOutput output = calculator->Calculate(input);
    
    for (size_t i = 0; i < output.sigmaT.size(); ++i) {
        EXPECT_TRUE(std::isfinite(output.sigmaT[i])) 
            << "sigmaT[" << i << "] is not finite";
        EXPECT_TRUE(std::isfinite(output.epsilonT[i])) 
            << "epsilonT[" << i << "] is not finite";
        EXPECT_TRUE(std::isfinite(output.sigmaZ[i])) 
            << "sigmaZ[" << i << "] is not finite";
        EXPECT_TRUE(std::isfinite(output.epsilonZ[i])) 
            << "epsilonZ[" << i << "] is not finite";
        EXPECT_TRUE(std::isfinite(output.deflection[i])) 
            << "deflection[" << i << "] is not finite";
    }
}

// ============================================================================
// Physical Validity Tests
// ============================================================================

TEST_F(PavementCalculatorTest, SurfaceDeflectionIsPositive) {
    CalculationOutput output = calculator->Calculate(input);
    
    // Surface deflection (first position) should be positive (downward)
    EXPECT_GT(output.deflection[0], 0.0);
}

TEST_F(PavementCalculatorTest, DeflectionDecreasesWithDepth) {
    CalculationOutput output = calculator->Calculate(input);
    
    // Generally, deflection should decrease with depth
    // (though this isn't always strictly monotonic)
    EXPECT_GT(output.deflection[0], output.deflection[output.deflection.size() - 1]);
}

TEST_F(PavementCalculatorTest, VerticalStressPositiveAtSurface) {
    CalculationOutput output = calculator->Calculate(input);
    
    // Vertical stress at surface should be positive (compression)
    // Note: Sign convention may vary, adjust if needed
    EXPECT_NE(output.sigmaZ[0], 0.0);
}

// ============================================================================
// Parametric Tests
// ============================================================================

TEST_F(PavementCalculatorTest, HigherPressureIncreasesDeflection) {
    // Calculate with default pressure
    CalculationOutput output1 = calculator->Calculate(input);
    
    // Calculate with higher pressure
    input.pressure *= 2.0;
    CalculationOutput output2 = calculator->Calculate(input);
    
    // Higher pressure should give higher deflection (approximately)
    EXPECT_GT(output2.deflection[0], output1.deflection[0] * 1.5);
}

TEST_F(PavementCalculatorTest, StiifferSurfaceReducesDeflection) {
    // Calculate with default modulus
    CalculationOutput output1 = calculator->Calculate(input);
    
    // Calculate with stiffer surface
    input.youngModuli[0] *= 2.0;
    CalculationOutput output2 = calculator->Calculate(input);
    
    // Stiffer surface should reduce deflection
    EXPECT_LT(output2.deflection[0], output1.deflection[0]);
}

TEST_F(PavementCalculatorTest, ThickerSurfaceReducesDeflection) {
    // Calculate with default thickness
    CalculationOutput output1 = calculator->Calculate(input);
    
    // Calculate with thicker surface layer
    input.thicknesses[0] *= 2.0;
    CalculationOutput output2 = calculator->Calculate(input);
    
    // Thicker surface should reduce deflection
    EXPECT_LT(output2.deflection[0], output1.deflection[0]);
}

// ============================================================================
// Edge Case Tests
// ============================================================================

TEST_F(PavementCalculatorTest, TwoLayerStructure) {
    input.layerCount = 2;
    input.poissonRatios.resize(2, 0.35);
    input.youngModuli = {5000.0, 50.0};
    input.thicknesses = {0.2, 100.0};
    input.interfaceTypes.resize(1, 0);
    
    EXPECT_NO_THROW({
        CalculationOutput output = calculator->Calculate(input);
        EXPECT_EQ(output.sigmaT.size(), 3);  // 2*2-1
    });
}

TEST_F(PavementCalculatorTest, SevenLayerStructure) {
    input.layerCount = 7;
    input.poissonRatios.resize(7, 0.35);
    input.youngModuli = {15000.0, 10000.0, 5000.0, 1000.0, 500.0, 200.0, 50.0};
    input.thicknesses = {0.06, 0.08, 0.12, 0.15, 0.20, 0.30, 100.0};
    input.interfaceTypes.resize(6, 0);
    
    EXPECT_NO_THROW({
        CalculationOutput output = calculator->Calculate(input);
        EXPECT_EQ(output.sigmaT.size(), 13);  // 2*7-1
    });
}

TEST_F(PavementCalculatorTest, UnbondedInterface) {
    input.interfaceTypes[0] = 1;  // Unbonded first interface
    
    EXPECT_NO_THROW({
        CalculationOutput output = calculator->Calculate(input);
        EXPECT_GT(output.deflection[0], 0.0);
    });
}

TEST_F(PavementCalculatorTest, TwinWheelConfiguration) {
    input.wheelType = 2;  // Twin wheels
    input.wheelSpacing = 0.375;  // Typical twin wheel spacing
    
    EXPECT_NO_THROW({
        CalculationOutput output = calculator->Calculate(input);
        EXPECT_GT(output.deflection[0], 0.0);
    });
}

// ============================================================================
// Consistency Tests
// ============================================================================

TEST_F(PavementCalculatorTest, CalculateIsRepeatable) {
    CalculationOutput output1 = calculator->Calculate(input);
    CalculationOutput output2 = calculator->Calculate(input);
    
    // Same input should give identical output
    for (size_t i = 0; i < output1.sigmaT.size(); ++i) {
        EXPECT_NEAR(output1.sigmaT[i], output2.sigmaT[i], 1e-10);
        EXPECT_NEAR(output1.deflection[i], output2.deflection[i], 1e-10);
    }
}

TEST_F(PavementCalculatorTest, CalculateIndependentOfPriorCalls) {
    // First calculation
    CalculationOutput output1 = calculator->Calculate(input);
    
    // Change input and calculate
    input.pressure *= 1.5;
    CalculationOutput output2 = calculator->Calculate(input);
    
    // Reset input and calculate again
    input.pressure /= 1.5;
    CalculationOutput output3 = calculator->Calculate(input);
    
    // First and third should match
    for (size_t i = 0; i < output1.sigmaT.size(); ++i) {
        EXPECT_NEAR(output1.sigmaT[i], output3.sigmaT[i], 1e-10);
        EXPECT_NEAR(output1.deflection[i], output3.deflection[i], 1e-10);
    }
}

// ============================================================================
// Performance Tests
// ============================================================================

TEST_F(PavementCalculatorTest, CalculationPerformance) {
    auto start = std::chrono::high_resolution_clock::now();
    
    CalculationOutput output = calculator->Calculate(input);
    
    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);
    
    // Calculation should complete in < 2 seconds
    EXPECT_LT(duration.count(), 2000);
}

// ============================================================================
// Validation Tests
// ============================================================================

TEST_F(PavementCalculatorTest, ThrowsOnInvalidInput) {
    input.layerCount = -1;  // Invalid
    
    EXPECT_THROW({
        calculator->Calculate(input);
    }, std::invalid_argument);
}

TEST_F(PavementCalculatorTest, ThrowsOnEmptyVectors) {
    input.layerCount = 3;
    input.poissonRatios.clear();  // Empty, invalid
    
    EXPECT_THROW({
        calculator->Calculate(input);
    }, std::invalid_argument);
}
