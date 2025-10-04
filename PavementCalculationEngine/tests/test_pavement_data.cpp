#include <gtest/gtest.h>
#include "PavementData.h"
#include <stdexcept>

using namespace Pavement;

// Test fixture for PavementData tests
class PavementDataTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Setup code runs before each test
    }

    void TearDown() override {
        // Cleanup code runs after each test
    }
};

// ============================================================================
// CalculationInput Tests
// ============================================================================

TEST_F(PavementDataTest, DefaultConstructor) {
    CalculationInput input;
    EXPECT_EQ(input.layerCount, 0);
    EXPECT_TRUE(input.poissonRatios.empty());
    EXPECT_TRUE(input.youngModuli.empty());
    EXPECT_TRUE(input.thicknesses.empty());
}

TEST_F(PavementDataTest, SetDefaults) {
    CalculationInput input;
    input.SetDefaults();
    
    EXPECT_EQ(input.layerCount, 3);
    EXPECT_EQ(input.poissonRatios.size(), 3);
    EXPECT_EQ(input.youngModuli.size(), 3);
    EXPECT_EQ(input.thicknesses.size(), 3);
    
    // Check realistic default values
    EXPECT_GT(input.youngModuli[0], input.youngModuli[1]);  // Surface stiffer than base
    EXPECT_GT(input.youngModuli[1], input.youngModuli[2]);  // Base stiffer than platform
}

TEST_F(PavementDataTest, ValidationSuccess) {
    CalculationInput input;
    input.SetDefaults();
    
    EXPECT_NO_THROW(input.Validate());
}

TEST_F(PavementDataTest, ValidationFailsInvalidLayerCount) {
    CalculationInput input;
    input.layerCount = 0;  // Invalid
    input.poissonRatios.resize(0);
    input.youngModuli.resize(0);
    input.thicknesses.resize(0);
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsInvalidPoisson) {
    CalculationInput input;
    input.SetDefaults();
    input.poissonRatios[0] = 0.6;  // > 0.5, invalid
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsNegativePoisson) {
    CalculationInput input;
    input.SetDefaults();
    input.poissonRatios[1] = -0.1;  // Negative, invalid
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsInvalidYoungModulus) {
    CalculationInput input;
    input.SetDefaults();
    input.youngModuli[0] = 0.0;  // Must be > 0
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsNegativeThickness) {
    CalculationInput input;
    input.SetDefaults();
    input.thicknesses[0] = -0.1;  // Negative, invalid
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationAllowsLargePlatformThickness) {
    CalculationInput input;
    input.SetDefaults();
    input.thicknesses[2] = 200.0;  // Large platform thickness is OK
    
    EXPECT_NO_THROW(input.Validate());
}

TEST_F(PavementDataTest, ValidationFailsTooThinLayer) {
    CalculationInput input;
    input.SetDefaults();
    input.thicknesses[0] = 0.005;  // 5mm, too thin
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsExtremeModulusContrast) {
    CalculationInput input;
    input.SetDefaults();
    input.youngModuli[0] = 100000.0;  // Very stiff
    input.youngModuli[1] = 5.0;       // Very soft
    // Ratio = 20000:1 > 10000:1 limit
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsInvalidWheelType) {
    CalculationInput input;
    input.SetDefaults();
    input.wheelType = 3;  // Only 1 or 2 are valid
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsInvalidPressure) {
    CalculationInput input;
    input.SetDefaults();
    input.pressure = 6.0;  // > 5.0 MPa limit
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsZeroPressure) {
    CalculationInput input;
    input.SetDefaults();
    input.pressure = 0.0;  // Must be > 0
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ValidationFailsInvalidContactRadius) {
    CalculationInput input;
    input.SetDefaults();
    input.contactRadius = 1.5;  // > 1.0m limit
    
    EXPECT_THROW(input.Validate(), std::invalid_argument);
}

TEST_F(PavementDataTest, ToStringOutput) {
    CalculationInput input;
    input.SetDefaults();
    
    std::string output = input.ToString();
    
    EXPECT_NE(output.find("layerCount"), std::string::npos);
    EXPECT_NE(output.find("poissonRatios"), std::string::npos);
    EXPECT_NE(output.find("youngModuli"), std::string::npos);
}

// ============================================================================
// CalculationOutput Tests
// ============================================================================

TEST_F(PavementDataTest, OutputDefaultConstructor) {
    CalculationOutput output;
    EXPECT_TRUE(output.sigmaT.empty());
    EXPECT_TRUE(output.epsilonT.empty());
}

TEST_F(PavementDataTest, OutputResize) {
    CalculationOutput output;
    output.Resize(5);  // 3 layers = 5 positions
    
    EXPECT_EQ(output.sigmaT.size(), 5);
    EXPECT_EQ(output.epsilonT.size(), 5);
    EXPECT_EQ(output.sigmaZ.size(), 5);
    EXPECT_EQ(output.epsilonZ.size(), 5);
    EXPECT_EQ(output.deflection.size(), 5);
}

TEST_F(PavementDataTest, OutputClear) {
    CalculationOutput output;
    output.Resize(5);
    
    // Add some values
    output.sigmaT[0] = 100.0;
    output.epsilonT[1] = 50.0;
    
    output.Clear();
    
    // All values should be zero
    for (size_t i = 0; i < output.sigmaT.size(); ++i) {
        EXPECT_DOUBLE_EQ(output.sigmaT[i], 0.0);
        EXPECT_DOUBLE_EQ(output.epsilonT[i], 0.0);
        EXPECT_DOUBLE_EQ(output.sigmaZ[i], 0.0);
        EXPECT_DOUBLE_EQ(output.epsilonZ[i], 0.0);
        EXPECT_DOUBLE_EQ(output.deflection[i], 0.0);
    }
}

TEST_F(PavementDataTest, OutputToString) {
    CalculationOutput output;
    output.Resize(2);
    output.sigmaT[0] = 123.45;
    output.epsilonT[1] = 678.90;
    
    std::string str = output.ToString();
    
    EXPECT_NE(str.find("sigmaT"), std::string::npos);
    EXPECT_NE(str.find("123.45"), std::string::npos);
}

// ============================================================================
// WorkingData Tests
// ============================================================================

TEST_F(PavementDataTest, WorkingDataResize) {
    WorkingData work;
    work.Resize(3);  // 3 layers
    
    EXPECT_EQ(work.systemMatrix.rows(), 10);  // 3*4 - 2
    EXPECT_EQ(work.systemMatrix.cols(), 10);
    EXPECT_EQ(work.coefficients.size(), 10);
}

TEST_F(PavementDataTest, WorkingDataClear) {
    WorkingData work;
    work.Resize(2);
    
    // Set some values
    work.systemMatrix(0, 0) = 5.0;
    work.coefficients(1) = 3.0;
    
    work.Clear();
    
    // Check all zeros
    EXPECT_DOUBLE_EQ(work.systemMatrix(0, 0), 0.0);
    EXPECT_DOUBLE_EQ(work.coefficients(1), 0.0);
}

// ============================================================================
// Integration Tests
// ============================================================================

TEST_F(PavementDataTest, CompleteWorkflow) {
    // Create input
    CalculationInput input;
    input.SetDefaults();
    
    // Validate
    EXPECT_NO_THROW(input.Validate());
    
    // Create output
    CalculationOutput output;
    int resultSize = 2 * input.layerCount - 1;
    output.Resize(resultSize);
    output.Clear();
    
    EXPECT_EQ(output.sigmaT.size(), static_cast<size_t>(resultSize));
    
    // Create working data
    WorkingData work;
    work.Resize(input.layerCount);
    
    EXPECT_GT(work.systemMatrix.rows(), 0);
}
